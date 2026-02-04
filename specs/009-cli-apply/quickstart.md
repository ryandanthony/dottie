# Quickstart: CLI Command `apply`

**Feature**: 009-cli-apply  
**Date**: February 3, 2026

## Overview

This quickstart provides implementation guidance for the `apply` command, which unifies `link` and `install` operations into a single command.

## File Locations

| File | Purpose |
|------|---------|
| `src/Dottie.Cli/Commands/ApplyCommand.cs` | Main command implementation |
| `src/Dottie.Cli/Commands/ApplyCommandSettings.cs` | Command settings |
| `src/Dottie.Cli/Output/ApplyProgressRenderer.cs` | Verbose summary renderer |
| `src/Dottie.Cli/Models/ApplyResult.cs` | Result aggregation model |
| `src/Dottie.Cli/Models/LinkPhaseResult.cs` | Link phase result wrapper |
| `src/Dottie.Cli/Models/InstallPhaseResult.cs` | Install phase result wrapper |
| `tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs` | Unit tests |
| `tests/Dottie.Cli.Tests/Commands/ApplyCommandSettingsTests.cs` | Settings tests |

## Implementation Steps

### Step 1: Create ApplyCommandSettings

```csharp
// src/Dottie.Cli/Commands/ApplyCommandSettings.cs
using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Settings for the apply command.
/// </summary>
public sealed class ApplyCommandSettings : ProfileAwareSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to force apply.
    /// </summary>
    [Description("Force apply by backing up conflicting files")]
    [CommandOption("-f|--force")]
    public bool Force { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to perform a dry run.
    /// </summary>
    [Description("Preview changes without making them")]
    [CommandOption("-d|--dry-run")]
    public bool DryRun { get; set; }
}
```

### Step 2: Create Result Models

```csharp
// src/Dottie.Cli/Models/LinkPhaseResult.cs
using Dottie.Configuration.Linking;

namespace Dottie.Cli.Models;

/// <summary>
/// Result of the link phase of apply.
/// </summary>
public sealed class LinkPhaseResult
{
    public bool WasExecuted { get; init; }
    public bool WasBlocked { get; init; }
    public LinkExecutionResult? ExecutionResult { get; init; }
    
    public bool HasFailures => ExecutionResult?.LinkResult?.FailedLinks.Count > 0;

    public static LinkPhaseResult NotExecuted() => new() { WasExecuted = false };
    
    public static LinkPhaseResult Blocked(LinkExecutionResult result) => 
        new() { WasExecuted = true, WasBlocked = true, ExecutionResult = result };
    
    public static LinkPhaseResult Executed(LinkExecutionResult result) => 
        new() { WasExecuted = true, ExecutionResult = result };
}
```

```csharp
// src/Dottie.Cli/Models/InstallPhaseResult.cs
using Dottie.Configuration.Installing;

namespace Dottie.Cli.Models;

/// <summary>
/// Result of the install phase of apply.
/// </summary>
public sealed class InstallPhaseResult
{
    public bool WasExecuted { get; init; }
    public IReadOnlyList<InstallResult> Results { get; init; } = [];
    
    public bool HasFailures => Results.Any(r => r.Status == InstallStatus.Failed);

    public static InstallPhaseResult NotExecuted() => new() { WasExecuted = false };
    
    public static InstallPhaseResult Executed(IReadOnlyList<InstallResult> results) => 
        new() { WasExecuted = true, Results = results };
}
```

```csharp
// src/Dottie.Cli/Models/ApplyResult.cs
namespace Dottie.Cli.Models;

/// <summary>
/// Aggregated result of the apply operation.
/// </summary>
public sealed class ApplyResult
{
    public required LinkPhaseResult LinkPhase { get; init; }
    public required InstallPhaseResult InstallPhase { get; init; }
    
    public bool OverallSuccess => 
        !LinkPhase.WasBlocked && 
        !LinkPhase.HasFailures && 
        !InstallPhase.HasFailures;
}
```

### Step 3: Create ApplyCommand

```csharp
// src/Dottie.Cli/Commands/ApplyCommand.cs
using Dottie.Cli.Models;
using Dottie.Cli.Output;
using Dottie.Cli.Utilities;
using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Linking;
using Dottie.Configuration.Parsing;
using Dottie.Configuration.Validation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Command to apply profile: link dotfiles and install software.
/// </summary>
public sealed class ApplyCommand : AsyncCommand<ApplyCommandSettings>
{
    private readonly IApplyProgressRenderer _renderer;

    public ApplyCommand(IApplyProgressRenderer? renderer = null)
    {
        _renderer = renderer ?? new ApplyProgressRenderer();
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ApplyCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var repoRoot = FindRepoRoot();
        if (repoRoot is null) return 1;

        var profile = LoadAndResolveProfile(settings, repoRoot, out var exitCode);
        if (profile is null) return exitCode;

        var profileName = settings.ProfileName ?? "default";

        if (settings.DryRun)
        {
            _renderer.RenderDryRunPreview(profile, repoRoot);
            return 0;
        }

        var result = await ExecuteApplyAsync(profile, repoRoot, settings.Force);
        _renderer.RenderVerboseSummary(result, profileName);

        return result.OverallSuccess ? 0 : 1;
    }

    private async Task<ApplyResult> ExecuteApplyAsync(ResolvedProfile profile, string repoRoot, bool force)
    {
        // Phase 1: Link dotfiles
        var linkPhase = ExecuteLinkPhase(profile, repoRoot, force);
        
        // If blocked, don't proceed to install
        if (linkPhase.WasBlocked)
        {
            return new ApplyResult
            {
                LinkPhase = linkPhase,
                InstallPhase = InstallPhaseResult.NotExecuted()
            };
        }

        // Phase 2: Install software
        var installPhase = await ExecuteInstallPhaseAsync(profile, repoRoot);

        return new ApplyResult
        {
            LinkPhase = linkPhase,
            InstallPhase = installPhase
        };
    }

    private static LinkPhaseResult ExecuteLinkPhase(ResolvedProfile profile, string repoRoot, bool force)
    {
        if (profile.Dotfiles.Count == 0)
        {
            return LinkPhaseResult.NotExecuted();
        }

        var orchestrator = new LinkingOrchestrator();
        var result = orchestrator.ExecuteLink(profile, repoRoot, force);

        return result.IsBlocked 
            ? LinkPhaseResult.Blocked(result) 
            : LinkPhaseResult.Executed(result);
    }

    private static async Task<InstallPhaseResult> ExecuteInstallPhaseAsync(ResolvedProfile profile, string repoRoot)
    {
        if (profile.Install is null)
        {
            return InstallPhaseResult.NotExecuted();
        }

        var context = new InstallContext
        {
            RepoRoot = repoRoot,
            HasSudo = new SudoChecker().IsSudoAvailable(),
            DryRun = false
        };

        var results = await RunInstallersAsync(profile.Install, context);
        return InstallPhaseResult.Executed(results);
    }

    private static async Task<IReadOnlyList<InstallResult>> RunInstallersAsync(InstallBlock installBlock, InstallContext context)
    {
        var installers = new List<IInstallSource>
        {
            new GithubReleaseInstaller(),
            new AptPackageInstaller(),
            new AptRepoInstaller(),
            new ScriptRunner(),
            new FontInstaller(),
            new SnapPackageInstaller(),
        };

        var results = new List<InstallResult>();
        foreach (var installer in installers)
        {
            try
            {
                var installerResults = await ExecuteInstallerAsync(installer, installBlock, context);
                results.AddRange(installerResults);
            }
            catch (Exception ex)
            {
                results.Add(InstallResult.Failed("unknown", installer.SourceType, $"Installer error: {ex.Message}"));
            }
        }

        return results;
    }

    // ... helper methods (FindRepoRoot, LoadAndResolveProfile, ExecuteInstallerAsync)
    // These can be extracted from existing LinkCommand/InstallCommand
}
```

### Step 4: Register Command in Program.cs

```csharp
// Add to Program.cs Configure method
config.AddCommand<ApplyCommand>("apply")
    .WithDescription("Apply profile: create symlinks and install software")
    .WithExample("apply")
    .WithExample("apply", "--profile", "work")
    .WithExample("apply", "--dry-run")
    .WithExample("apply", "--force");
```

## Testing Approach

### Unit Test Structure

```csharp
// tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs
public sealed class ApplyCommandTests : IDisposable
{
    [Fact]
    public async Task ExecuteAsync_WithValidProfile_LinksAndInstalls()
    {
        // Arrange: Create temp directory with config
        // Act: Execute command
        // Assert: Both phases executed
    }

    [Fact]
    public async Task ExecuteAsync_WithDryRun_DoesNotModifyFilesystem()
    {
        // Arrange: Create temp directory with config
        // Act: Execute with --dry-run
        // Assert: No files created/modified
    }

    [Fact]
    public async Task ExecuteAsync_WithConflicts_FailsWithoutForce()
    {
        // Arrange: Create config and conflicting file
        // Act: Execute without --force
        // Assert: Returns exit code 1, no modifications
    }

    [Fact]
    public async Task ExecuteAsync_WithForceAndConflicts_BacksUpAndLinks()
    {
        // Arrange: Create config and conflicting file
        // Act: Execute with --force
        // Assert: Backup created, symlink created
    }
}
```

### Integration Test Scenarios

```powershell
# tests/integration/apply/
# apply-basic.ps1 - Basic apply with default profile
# apply-profile.ps1 - Apply specific profile
# apply-dry-run.ps1 - Verify dry-run makes no changes
# apply-force.ps1 - Force with conflicts
# apply-inheritance.ps1 - Profile inheritance chain
```

## Key Considerations

### Fail-Soft Behavior

Per clarification, installation failures don't stop processing:
- Continue with remaining installers
- Collect all failures
- Report in summary
- Return exit code 1 if any failures

### Idempotency

- `LinkingOrchestrator` already handles already-linked detection
- Installers already handle already-installed detection
- No special handling needed in `ApplyCommand`

### Error Messages

Follow existing patterns:
- Use `[red]Error:[/]` prefix for fatal errors
- Use `[yellow]Warning:[/]` for non-fatal issues
- Include actionable hints where possible
