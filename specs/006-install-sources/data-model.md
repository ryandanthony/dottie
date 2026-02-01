# Data Model: Installation Sources

**Feature**: 006-install-sources  
**Date**: January 31, 2026

## Entity Overview

This feature operates on existing configuration entities and introduces new result/context entities for installation operations.

```
┌─────────────────────┐      ┌─────────────────────┐
│  DottieConfiguration │──────│    ConfigProfile    │
│  (from FEATURE-01)  │  1:n │   (from FEATURE-02) │
└─────────────────────┘      └──────────┬──────────┘
                                        │ 0..1
                                        ▼
                             ┌─────────────────────┐
                             │    InstallBlock     │
                             │  (from FEATURE-01)  │
                             └──────────┬──────────┘
                                        │ contains
        ┌───────────────┬───────────────┼───────────────┬───────────────┐
        ▼               ▼               ▼               ▼               ▼
┌───────────────┐ ┌───────────┐ ┌───────────────┐ ┌───────────┐ ┌───────────┐
│GithubRelease  │ │ AptPackage│ │   AptRepo     │ │  Script   │ │   Font    │
│    Item       │ │  (string) │ │    Item       │ │  (string) │ │   Item    │
└───────┬───────┘ └─────┬─────┘ └───────┬───────┘ └─────┬─────┘ └─────┬─────┘
        │               │               │               │             │
        └───────────────┴───────────────┼───────────────┴─────────────┘
                                        │ 1:1 (install operation)
                                        ▼
                             ┌─────────────────────┐
                             │   InstallResult     │
                             │   (this feature)    │
                             └─────────────────────┘
```

## Existing Entities (Reference)

### InstallBlock (from Dottie.Configuration.Models.InstallBlocks)

Container for all installation specifications organized by source type.

```csharp
public sealed record InstallBlock
{
    public IList<GithubReleaseItem> Github { get; init; } = [];
    public IList<string> Apt { get; init; } = [];
    public IList<AptRepoItem> AptRepos { get; init; } = [];
    public IList<string> Scripts { get; init; } = [];
    public IList<FontItem> Fonts { get; init; } = [];
    public IList<SnapItem> Snaps { get; init; } = [];
}
```

### GithubReleaseItem (from Dottie.Configuration.Models.InstallBlocks)

Specification for downloading a binary from a GitHub release.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Repo | string | Yes | Repository in owner/name format (e.g., "junegunn/fzf") |
| Asset | string | Yes | Glob pattern for release asset filename |
| Binary | string | Yes | Name of binary to extract from archive |
| Version | string? | No | Specific version tag (default: latest) |

### AptRepoItem (from Dottie.Configuration.Models.InstallBlocks)

Specification for a private APT repository.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Name | string | Yes | Descriptive name for the repository |
| KeyUrl | string | Yes | URL to download GPG key |
| Repo | string | Yes | Repository source line for sources.list |
| Packages | IList\<string\> | Yes | Packages to install from this repo |

### FontItem (from Dottie.Configuration.Models.InstallBlocks)

Specification for downloading and installing a font.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Name | string | Yes | Display name for the font |
| Url | string | Yes | URL to download font archive |

### SnapItem (from Dottie.Configuration.Models.InstallBlocks)

Specification for a snap package.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| Name | string | Yes | Snap package name |
| Classic | bool | No | Whether to use --classic confinement |

## New Entities (This Feature)

### InstallContext

Shared context passed to all installer services.

| Field | Type | Description |
|-------|------|-------------|
| RepoRoot | string | Absolute path to repository root |
| BinDirectory | string | Target directory for binaries (default: ~/bin/) |
| FontDirectory | string | Target directory for fonts (default: ~/.local/share/fonts/) |
| GithubToken | string? | Optional GitHub API token from environment |
| HasSudo | bool | Whether sudo is available |
| DryRun | bool | Whether to preview without making changes |

```csharp
public sealed record InstallContext
{
    public required string RepoRoot { get; init; }
    public string BinDirectory { get; init; } = 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "bin");
    public string FontDirectory { get; init; } = 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                     ".local", "share", "fonts");
    public string? GithubToken { get; init; }
    public bool HasSudo { get; init; }
    public bool DryRun { get; init; }
}
```

### InstallResult

Result of a single installation operation.

| Field | Type | Description |
|-------|------|-------------|
| ItemName | string | Identifier for the item (e.g., repo name, package name) |
| SourceType | InstallSourceType | Which source type processed this item |
| Status | InstallStatus | Success, Skipped, Warning, or Failed |
| Message | string? | Additional details (version installed, skip reason, error) |
| InstalledPath | string? | Path where item was installed (if applicable) |

```csharp
public sealed record InstallResult
{
    public required string ItemName { get; init; }
    public required InstallSourceType SourceType { get; init; }
    public required InstallStatus Status { get; init; }
    public string? Message { get; init; }
    public string? InstalledPath { get; init; }
    
    public static InstallResult Success(string name, InstallSourceType source, string? path = null, string? message = null)
        => new() { ItemName = name, SourceType = source, Status = InstallStatus.Success, InstalledPath = path, Message = message };
    
    public static InstallResult Skipped(string name, InstallSourceType source, string reason)
        => new() { ItemName = name, SourceType = source, Status = InstallStatus.Skipped, Message = reason };
    
    public static InstallResult Warning(string name, InstallSourceType source, string reason)
        => new() { ItemName = name, SourceType = source, Status = InstallStatus.Warning, Message = reason };
    
    public static InstallResult Failed(string name, InstallSourceType source, string error)
        => new() { ItemName = name, SourceType = source, Status = InstallStatus.Failed, Message = error };
}
```

### InstallStatus (Enum)

```csharp
public enum InstallStatus
{
    Success,   // Item installed successfully
    Skipped,   // Item already installed or not applicable
    Warning,   // Item skipped due to missing capability (e.g., no sudo)
    Failed     // Item installation failed
}
```

### InstallSourceType (Enum)

```csharp
public enum InstallSourceType
{
    GithubRelease = 1,  // Priority order matches value
    AptPackage = 2,
    AptRepo = 3,
    Script = 4,
    Font = 5,
    SnapPackage = 6
}
```

### InstallCommandSettings

Command-line arguments for the install command.

| Field | Type | Default | Validation |
|-------|------|---------|------------|
| ProfileName | string? | "default" | Must exist in config |
| ConfigPath | string? | dottie.yaml in repo root | Must exist |
| DryRun | bool | false | - |

```csharp
public sealed class InstallCommandSettings : ProfileAwareSettings
{
    [CommandOption("--dry-run")]
    [Description("Preview what would be installed without making changes")]
    public bool DryRun { get; init; }
}
```

### IInstallSource (Interface)

Contract for installer service implementations.

```csharp
public interface IInstallSource
{
    /// <summary>
    /// Human-readable name for this source (e.g., "GitHub Releases").
    /// </summary>
    string SourceName { get; }
    
    /// <summary>
    /// Installation priority (lower = processed first).
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Installs items from this source.
    /// </summary>
    Task<IReadOnlyList<InstallResult>> InstallAsync(
        InstallBlock installBlock,
        InstallContext context,
        CancellationToken cancellationToken = default);
}
```

## Entity Relationships

### Configuration → Installation Flow

```
User Config (dottie.yaml)
        │
        ▼
┌───────────────────┐
│ ConfigurationLoader│ ──→ DottieConfiguration
└───────────────────┘
        │
        ▼
┌───────────────────┐
│   ProfileMerger   │ ──→ ResolvedProfile (with Install block)
└───────────────────┘
        │
        ▼
┌───────────────────┐
│InstallOrchestrator│ ──→ Coordinates IInstallSource implementations
└───────────────────┘
        │
        ├──→ GithubReleaseInstaller ──→ InstallResult[]
        ├──→ AptPackageInstaller    ──→ InstallResult[]
        ├──→ AptRepoInstaller       ──→ InstallResult[]
        ├──→ ScriptRunner           ──→ InstallResult[]
        ├──→ FontInstaller          ──→ InstallResult[]
        └──→ SnapPackageInstaller   ──→ InstallResult[]
```

### State Transitions

Installation items don't have persistent state; each run evaluates current system state:

```
┌──────────────┐
│ Not Installed│
└──────┬───────┘
       │ InstallAsync()
       ▼
┌──────────────┐     ┌──────────────┐
│   Success    │     │   Skipped    │ (already installed)
└──────────────┘     └──────────────┘
       │
       ▼
┌──────────────┐     ┌──────────────┐
│  Installed   │     │   Warning    │ (no sudo available)
└──────────────┘     └──────────────┘
                            │
                            ▼
                     ┌──────────────┐
                     │   Failed     │ (network, permissions, etc.)
                     └──────────────┘
```

## Validation Rules

### GithubReleaseItem Validation (existing)
- Repo: Required, must be in "owner/repo" format
- Asset: Required, non-empty glob pattern
- Binary: Required, non-empty

### AptRepoItem Validation (existing)
- Name: Required, non-empty
- KeyUrl: Required, must be valid URL
- Repo: Required, must start with "deb"
- Packages: Required, at least one package

### FontItem Validation (existing)
- Name: Required, non-empty
- Url: Required, must be valid URL

### SnapItem Validation (existing)
- Name: Required, non-empty

### Script Path Validation (new, at runtime)
- Path must be relative
- Resolved path must exist within repo root
- File must exist
