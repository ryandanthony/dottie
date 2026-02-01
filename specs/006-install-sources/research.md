# Research: Installation Sources

**Feature**: 006-install-sources  
**Date**: January 31, 2026  
**Status**: Complete

## Executive Summary

This feature implements the `dottie install` command to install tools from 6 different sources. The configuration models already exist in the codebase; this research documents the implementation patterns and design decisions for the installation services.

## Technology Stack (Confirmed from Codebase)

| Aspect | Decision | Source |
|--------|----------|--------|
| **Language/Version** | C# / .NET 10.0 | `Dottie.Cli.csproj` |
| **CLI Framework** | Spectre.Console.Cli 0.50.0 | `Dottie.Cli.csproj` |
| **Console Output** | Spectre.Console 0.50.0 | `Dottie.Cli.csproj` |
| **YAML Parsing** | YamlDotNet 16.3.0 | `Dottie.Configuration.csproj` |
| **Testing Framework** | xUnit + FluentAssertions | Existing test projects |
| **Integration Testing** | Docker-based scenarios | `tests/integration/` |
| **Target Platform** | Linux (Ubuntu-first) | Constitution |

## Existing Implementation Analysis

### Already Implemented ✅

| Component | Implementation | Location |
|-----------|----------------|----------|
| GithubReleaseItem model | Complete with all fields | `Models/InstallBlocks/GithubReleaseItem.cs` |
| AptRepoItem model | Complete with all fields | `Models/InstallBlocks/AptRepoItem.cs` |
| FontItem model | Complete with all fields | `Models/InstallBlocks/FontItem.cs` |
| SnapItem model | Complete with all fields | `Models/InstallBlocks/SnapItem.cs` |
| InstallBlock container | All sources grouped | `Models/InstallBlocks/InstallBlock.cs` |
| ConfigProfile.Install | Install block reference | `Models/ConfigProfile.cs` |
| InstallBlockValidator | Validation for all items | `Validation/InstallBlockValidator.cs` |
| ConfigurationLoader | YAML parsing | `Parsing/ConfigurationLoader.cs` |
| ProfileMerger | Profile inheritance | `Inheritance/ProfileMerger.cs` |
| Command pattern | LinkCommand as template | `Commands/LinkCommand.cs` |
| Integration test runner | Docker + scenarios | `tests/integration/` |

### Gaps to Implement ❌

| Requirement | Gap | Proposed Solution |
|-------------|-----|-------------------|
| Install command | Not implemented | Create `InstallCommand` following `LinkCommand` pattern |
| GitHub release downloads | Not implemented | `GithubReleaseInstaller` with HTTP client |
| Archive extraction | Not implemented | `ArchiveExtractor` for .tar.gz, .zip, .tgz |
| APT package installation | Not implemented | `AptPackageInstaller` wrapping apt-get |
| APT repo setup | Not implemented | `AptRepoInstaller` for GPG + sources |
| Script execution | Not implemented | `ScriptRunner` with bash |
| Font installation | Not implemented | `FontInstaller` with fc-cache |
| Snap installation | Not implemented | `SnapPackageInstaller` wrapping snap |
| Progress display | Not implemented | `InstallProgressRenderer` |
| Sudo detection | Not implemented | `SudoChecker` utility |
| HTTP download with retry | Not implemented | `HttpDownloader` with exponential backoff |

## Design Decisions

### Decision 1: Installer Service Pattern

**Decision**: Create an `IInstallSource` interface with one implementation per source type.

**Rationale**:
- Follows existing `Linking/` namespace pattern with composable services
- Each installer can be unit tested in isolation
- Orchestrator coordinates but doesn't implement installation logic
- Easy to add new source types in the future

**Interface**:
```csharp
public interface IInstallSource
{
    string SourceName { get; }  // e.g., "GitHub Releases"
    int Priority { get; }       // Lower = higher priority
    Task<IReadOnlyList<InstallResult>> InstallAsync(
        InstallContext context,
        CancellationToken cancellationToken = default);
}
```

### Decision 2: HTTP Download Strategy

**Decision**: Use `HttpClient` with Polly-style retry logic (3 retries, exponential backoff).

**Rationale**:
- .NET's `HttpClient` is the standard for HTTP operations
- Exponential backoff prevents hammering servers on transient failures
- 3 retries balances resilience vs. user wait time
- GitHub API rate limiting returns 403/429, which are detectable

**Retry Pattern**:
```csharp
// Delays: 1s, 2s, 4s (exponential backoff)
var delays = new[] { 1000, 2000, 4000 };
```

### Decision 3: Archive Extraction Strategy

**Decision**: Use `System.IO.Compression` for .zip and `SharpCompress` or process-based tar for .tar.gz/.tgz.

**Rationale**:
- `System.IO.Compression.ZipFile` handles .zip natively
- .tar.gz requires either a library or shelling out to `tar`
- Ubuntu always has `tar` available, making process-based extraction reliable
- Simpler than adding a NuGet dependency for tar handling

**Alternatives Considered**:
- SharpCompress NuGet: More dependencies, but cross-platform
- Process-based tar: Simpler, Ubuntu-only (acceptable per constitution)

**Decision**: Use process-based `tar` for .tar.gz/.tgz on Linux.

### Decision 4: Sudo Detection Strategy

**Decision**: Check sudo availability upfront before starting installation.

**Rationale**:
- User knows immediately which sources will be skipped
- Avoids partial installation failures mid-process
- Can prompt user to run with sudo if desired
- Follows constitution's "Observable, Supportable Tooling" principle

**Detection Method**:
```csharp
// Check if running as root or sudo is available
var isRoot = Environment.GetEnvironmentVariable("USER") == "root" 
          || Mono.Unix.Native.Syscall.getuid() == 0;
var hasSudo = !isRoot && CanExecuteSudo();
```

### Decision 5: GitHub Authentication

**Decision**: Support optional `GITHUB_TOKEN` environment variable for API authentication.

**Rationale**:
- Unauthenticated: 60 requests/hour (sufficient for most users)
- Authenticated: 5,000 requests/hour (power users)
- Environment variable is standard pattern (used by gh CLI, GitHub Actions)
- No secrets in config files

**Implementation**:
```csharp
var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
if (!string.IsNullOrEmpty(token))
{
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
```

### Decision 6: Progress Display Pattern

**Decision**: Use Spectre.Console.Status for per-item progress, summary table at end.

**Rationale**:
- Matches constitution's observability requirements
- Status spinner shows current operation without overwhelming output
- Summary table clearly shows success/warning/failure counts
- Follows existing LinkCommand output patterns

**Example Output**:
```
Installing from GitHub Releases...
  ⠋ Downloading fzf from junegunn/fzf...
  ✓ Installed fzf v0.54.0 to ~/bin/fzf
  ✓ Installed ripgrep v14.1.0 to ~/bin/rg

Installing APT packages...
  ⚠ Skipping APT packages (sudo not available)

Summary:
┌─────────────────┬─────────┬──────────┬────────┐
│ Source          │ Success │ Warnings │ Failed │
├─────────────────┼─────────┼──────────┼────────┤
│ GitHub Releases │ 2       │ 0        │ 0      │
│ APT Packages    │ 0       │ 3        │ 0      │
│ Scripts         │ 1       │ 0        │ 0      │
└─────────────────┴─────────┴──────────┴────────┘
```

### Decision 7: Script Security Model

**Decision**: Only execute scripts that exist within the repository; reject external URLs.

**Rationale**:
- Constitution requires "Secure-by-Default Installations"
- Scripts from repo are version-controlled and auditable
- External script URLs could be modified without user knowledge
- Validates path is under repo root before execution

**Validation**:
```csharp
var fullPath = Path.GetFullPath(Path.Combine(repoRoot, scriptPath));
if (!fullPath.StartsWith(repoRoot))
{
    return InstallResult.Failed(scriptPath, "Script path escapes repository root");
}
```

### Decision 8: Font Cache Refresh

**Decision**: Run `fc-cache -fv` once after all fonts are installed (not per-font).

**Rationale**:
- fc-cache is expensive; running once is more efficient
- User sees one cache refresh instead of N
- Follows idempotency principle (same result regardless of font count)

## Best Practices Applied

### From Constitution

| Principle | Application |
|-----------|-------------|
| Safety & Explicitness | --dry-run previews; sudo check with warnings before proceeding |
| Idempotency | Skip already-installed binaries (check ~/bin/); skip installed APT packages |
| Secure-by-Default | Scripts in repo only; GPG key verification; optional auth via env var |
| Observable | Source-level progress; clear error messages with remediation |
| Simplicity | One service per source; reuse existing validation |

### From Existing Patterns

| Pattern | Source | Application |
|---------|--------|-------------|
| Command structure | LinkCommand | InstallCommand follows same pattern |
| Settings validation | LinkCommandSettings | InstallCommandSettings with --dry-run |
| Error formatting | ErrorFormatter | Reuse for installation errors |
| Integration tests | tests/integration/ | New scenarios following existing pattern |
| Service composition | Linking/ namespace | Installing/ namespace mirrors structure |

## External Dependencies

### GitHub API

- **Rate Limits**: 60/hour (unauthenticated), 5000/hour (authenticated)
- **Release API**: `GET /repos/{owner}/{repo}/releases/latest` or `/tags/{tag}`
- **Asset Download**: Direct download URL from release asset
- **Authentication**: Bearer token via `GITHUB_TOKEN` env var

### System Commands (Ubuntu)

| Command | Used For | Requires Sudo |
|---------|----------|---------------|
| `apt-get update` | Refresh package list | Yes |
| `apt-get install -y` | Install packages | Yes |
| `snap install` | Install snap packages | Yes |
| `tar -xzf` | Extract .tar.gz archives | No |
| `fc-cache -fv` | Refresh font cache | No |
| `bash` | Execute scripts | No |
| `chmod +x` | Make binary executable | No |

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| GitHub rate limiting | Medium | Medium | Support GITHUB_TOKEN; clear error message |
| Network failures | Medium | Low | Retry with backoff; continue with other sources |
| Missing sudo | High | Medium | Check upfront; skip affected sources with warning |
| Script failures | Low | Medium | Report with script name and exit code |
| Archive format errors | Low | Low | Validate extension; clear error on unknown format |
