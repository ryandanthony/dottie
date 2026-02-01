# Quickstart: Installation Sources

**Feature**: 006-install-sources  
**Date**: January 31, 2026

## Overview

This guide helps developers implement the `dottie install` command. The configuration models already exist; this feature adds the command and installation services.

## Prerequisites

- .NET 10.0 SDK
- Docker (for integration tests)
- Ubuntu environment (or WSL for Windows development)

## Development Setup

```bash
# Clone and setup
git checkout 006-install-sources
dotnet restore
dotnet build

# Run unit tests
dotnet test

# Run integration tests
cd tests
./run-integration-tests.ps1
```

## Project Structure Quick Reference

```
src/Dottie.Configuration/Installing/   # All installer services go here
src/Dottie.Cli/Commands/               # InstallCommand.cs
tests/Dottie.Configuration.Tests/Installing/  # Unit tests
tests/integration/scenarios/           # Integration test scenarios
```

## Implementation Order

Follow TDD for each component:

### 1. Core Infrastructure

```bash
# Create test file first
tests/Dottie.Configuration.Tests/Installing/InstallContextTests.cs
tests/Dottie.Configuration.Tests/Installing/Utilities/HttpDownloaderTests.cs
tests/Dottie.Configuration.Tests/Installing/Utilities/ArchiveExtractorTests.cs

# Then implement
src/Dottie.Configuration/Installing/InstallContext.cs
src/Dottie.Configuration/Installing/InstallResult.cs
src/Dottie.Configuration/Installing/IInstallSource.cs
src/Dottie.Configuration/Installing/Utilities/HttpDownloader.cs
src/Dottie.Configuration/Installing/Utilities/ArchiveExtractor.cs
```

### 2. GitHub Release Installer (P1)

```bash
# Test first
tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs

# Then implement
src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs
```

### 3. Integration Test

```bash
# Create integration scenario
tests/integration/scenarios/install-github-release/
├── dottie.yml
├── README.md
└── validate.sh
```

## Key Patterns

### Installer Service Pattern

Each installer implements `IInstallSource`:

```csharp
public class GithubReleaseInstaller : IInstallSource
{
    public string SourceName => "GitHub Releases";
    public int Priority => 1;
    
    public async Task<IReadOnlyList<InstallResult>> InstallAsync(
        InstallBlock installBlock,
        InstallContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<InstallResult>();
        
        foreach (var item in installBlock.Github)
        {
            if (context.DryRun)
            {
                results.Add(InstallResult.Skipped(item.Repo, InstallSourceType.GithubRelease, 
                    "Dry run - would download from GitHub"));
                continue;
            }
            
            // Implementation...
            results.Add(InstallResult.Success(item.Repo, InstallSourceType.GithubRelease, 
                installedPath, $"Installed v{version}"));
        }
        
        return results;
    }
}
```

### HttpDownloader with Retry

```csharp
public async Task<Stream> DownloadAsync(string url, CancellationToken ct)
{
    var delays = new[] { 1000, 2000, 4000 }; // Exponential backoff
    Exception? lastException = null;
    
    for (int attempt = 0; attempt <= delays.Length; attempt++)
    {
        try
        {
            var response = await _client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync(ct);
        }
        catch (HttpRequestException ex) when (attempt < delays.Length)
        {
            lastException = ex;
            await Task.Delay(delays[attempt], ct);
        }
    }
    
    throw lastException!;
}
```

### Command Pattern (following LinkCommand)

```csharp
public sealed class InstallCommand : Command<InstallCommandSettings>
{
    public override int Execute(CommandContext context, InstallCommandSettings settings)
    {
        var repoRoot = RepoRootFinder.Find();
        if (repoRoot is null) return 1;
        
        var profile = LoadAndResolveProfile(settings, repoRoot, out var exitCode);
        if (profile is null) return exitCode;
        
        if (profile.Install is null)
        {
            AnsiConsole.MarkupLine("[yellow]No install block defined in profile.[/]");
            return 0;
        }
        
        var installContext = new InstallContext
        {
            RepoRoot = repoRoot,
            GithubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN"),
            HasSudo = SudoChecker.HasSudo(),
            DryRun = settings.DryRun
        };
        
        var orchestrator = new InstallOrchestrator();
        var results = orchestrator.InstallAsync(profile.Install, installContext).Result;
        
        // Display results...
        return results.Any(r => r.Status == InstallStatus.Failed) ? 1 : 0;
    }
}
```

## Integration Test Pattern

### Scenario Structure

```
tests/integration/scenarios/install-github-release/
├── dottie.yml      # Configuration to test
├── README.md       # Test description
└── validate.sh     # Validation script
```

### Example dottie.yml

```yaml
profiles:
  default:
    dotfiles: []
    install:
      github:
        - repo: jqlang/jq
          asset: "jq-linux-amd64"
          binary: jq
```

### Example validate.sh

```bash
#!/bin/bash
TEST_DIR="$1"

echo "  Validating install-github-release scenario"

# Check binary was installed
if [ ! -f "$HOME/bin/jq" ]; then
    echo "  ERROR: jq binary not found in ~/bin/"
    exit 1
fi

# Check binary is executable
if [ ! -x "$HOME/bin/jq" ]; then
    echo "  ERROR: jq binary is not executable"
    exit 1
fi

# Check binary works
if ! "$HOME/bin/jq" --version > /dev/null 2>&1; then
    echo "  ERROR: jq binary failed to run"
    exit 1
fi

echo "  ✓ jq installed and working"
exit 0
```

## Testing Guidelines

### Unit Test Structure

```csharp
public class GithubReleaseInstallerTests
{
    [Fact]
    public async Task InstallAsync_WithValidRepo_DownloadsAndExtractsBinary()
    {
        // Arrange - use mock HttpClient
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.When("https://api.github.com/repos/*/releases/latest")
            .Respond("application/json", ReleaseJson);
        
        var installer = new GithubReleaseInstaller(new HttpClient(mockHandler));
        var context = CreateTestContext();
        var block = new InstallBlock { Github = [TestGithubItem] };
        
        // Act
        var results = await installer.InstallAsync(block, context);
        
        // Assert
        results.Should().ContainSingle()
            .Which.Status.Should().Be(InstallStatus.Success);
    }
}
```

### Coverage Requirements

- ≥90% line coverage
- ≥80% branch coverage
- Run `dotnet test --collect:"XPlat Code Coverage"` to verify

## Common Pitfalls

1. **Forgetting TDD**: Always write test FIRST, then implement
2. **Missing sudo check**: Check sudo availability before APT/snap operations
3. **Hardcoded paths**: Use `InstallContext.BinDirectory` and `FontDirectory`
4. **Network in unit tests**: Mock `HttpClient`, don't make real network calls
5. **Missing cancellation**: Pass `CancellationToken` through async chain

## Debugging

```bash
# Run single test
dotnet test --filter "FullyQualifiedName~GithubReleaseInstallerTests"

# Run with verbose output
dotnet test -v detailed

# Debug integration test
cd tests/integration
docker run -it --entrypoint /bin/bash dottie-integration-test
```

## Reference Files

- [LinkCommand.cs](../../src/Dottie.Cli/Commands/LinkCommand.cs) - Command pattern example
- [LinkingOrchestrator.cs](../../src/Dottie.Configuration/Linking/LinkingOrchestrator.cs) - Orchestrator pattern
- [InstallBlockValidator.cs](../../src/Dottie.Configuration/Validation/InstallBlockValidator.cs) - Existing validation
- [run-scenarios.sh](../../tests/integration/scripts/run-scenarios.sh) - Integration test runner
