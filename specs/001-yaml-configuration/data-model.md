# Data Model: YAML Configuration System

**Feature**: 001-yaml-configuration  
**Date**: 2026-01-28  
**Purpose**: Define domain entities as C# record types

---

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     DottieConfiguration                          │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Profiles: Dictionary<string, Profile>                    │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ 1..*
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                          Profile                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Name: string                                             │    │
│  │ Extends: string?                                         │    │
│  │ Dotfiles: List<DotfileEntry>                            │    │
│  │ Install: InstallBlock?                                   │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
          │                                    │
          │ 0..*                               │ 0..1
          ▼                                    ▼
┌──────────────────────┐         ┌────────────────────────────────┐
│    DotfileEntry      │         │         InstallBlock           │
│  ┌────────────────┐  │         │  ┌────────────────────────┐    │
│  │ Source: string │  │         │  │ Github: List<...>      │    │
│  │ Target: string │  │         │  │ Apt: List<string>      │    │
│  └────────────────┘  │         │  │ AptRepo: List<...>     │    │
└──────────────────────┘         │  │ Scripts: List<string>  │    │
                                 │  │ Fonts: List<...>       │    │
                                 │  │ Snap: List<...>        │    │
                                 │  └────────────────────────┘    │
                                 └────────────────────────────────┘
                                              │
              ┌───────────────┬───────────────┼───────────────┬───────────────┐
              │               │               │               │               │
              ▼               ▼               ▼               ▼               ▼
    ┌─────────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
    │ GithubRelease   │ │ AptRepoItem │ │  FontItem   │ │  SnapItem   │ │  (strings)  │
    │   Item          │ │             │ │             │ │             │ │  apt/scripts│
    └─────────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘
```

---

## Type Definitions

### Root Configuration

```csharp
namespace Dottie.Configuration.Models;

/// <summary>
/// Root configuration object representing a dottie.yaml file.
/// </summary>
public sealed record DottieConfiguration
{
    /// <summary>
    /// Named profiles defined in this configuration.
    /// Key is the profile name (e.g., "default", "work").
    /// </summary>
    public required Dictionary<string, Profile> Profiles { get; init; }
}
```

### Profile

```csharp
/// <summary>
/// A named configuration set containing dotfile mappings and install specifications.
/// May extend another profile via inheritance.
/// </summary>
public sealed record Profile
{
    /// <summary>
    /// Name of another profile to inherit from.
    /// When set, this profile's settings are merged with the parent.
    /// </summary>
    public string? Extends { get; init; }

    /// <summary>
    /// List of dotfile source-to-target mappings.
    /// </summary>
    public List<DotfileEntry> Dotfiles { get; init; } = [];

    /// <summary>
    /// Software installation specifications organized by install method.
    /// </summary>
    public InstallBlock? Install { get; init; }
}
```

### Dotfile Entry

```csharp
/// <summary>
/// A mapping from a source path (in the repository) to a target path (on the filesystem).
/// </summary>
public sealed record DotfileEntry
{
    /// <summary>
    /// Path to the source file or directory, relative to repository root.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Destination path where the dotfile should be linked.
    /// Supports tilde (~) expansion for home directory.
    /// </summary>
    public required string Target { get; init; }
}
```

### Install Block

```csharp
/// <summary>
/// Categorized collection of software to install, organized by installation method.
/// </summary>
public sealed record InstallBlock
{
    /// <summary>
    /// Binaries to download from GitHub releases.
    /// </summary>
    public List<GithubReleaseItem> Github { get; init; } = [];

    /// <summary>
    /// Package names to install via apt.
    /// </summary>
    public List<string> Apt { get; init; } = [];

    /// <summary>
    /// Third-party apt repositories to add before installing packages.
    /// </summary>
    public List<AptRepoItem> AptRepo { get; init; } = [];

    /// <summary>
    /// Script paths (relative to repo root) to execute.
    /// </summary>
    public List<string> Scripts { get; init; } = [];

    /// <summary>
    /// Fonts to download and install.
    /// </summary>
    public List<FontItem> Fonts { get; init; } = [];

    /// <summary>
    /// Snap packages to install.
    /// </summary>
    public List<SnapItem> Snap { get; init; } = [];
}
```

### GitHub Release Item

```csharp
/// <summary>
/// Specification for downloading a binary from a GitHub release.
/// </summary>
public sealed record GithubReleaseItem
{
    /// <summary>
    /// Repository in owner/name format (e.g., "junegunn/fzf").
    /// </summary>
    public required string Repo { get; init; }

    /// <summary>
    /// Glob pattern to match release asset filename (e.g., "fzf-*-linux_amd64.tar.gz").
    /// </summary>
    public required string Asset { get; init; }

    /// <summary>
    /// Name of the binary to extract from the archive.
    /// </summary>
    public required string Binary { get; init; }

    /// <summary>
    /// Specific version tag to download. Defaults to latest release if not specified.
    /// </summary>
    public string? Version { get; init; }

    // --- Merge key for inheritance ---
    /// <summary>
    /// Unique identifier for merging during profile inheritance.
    /// </summary>
    internal string MergeKey => Repo;
}
```

### Apt Repository Item

```csharp
/// <summary>
/// Specification for adding a third-party apt repository.
/// </summary>
public sealed record AptRepoItem
{
    /// <summary>
    /// Identifier name for this repository configuration.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL to the GPG key for repository verification.
    /// </summary>
    public required string KeyUrl { get; init; }

    /// <summary>
    /// The apt repository line (e.g., "deb [arch=amd64] https://... stable main").
    /// </summary>
    public required string Repo { get; init; }

    /// <summary>
    /// Packages to install from this repository.
    /// </summary>
    public required List<string> Packages { get; init; }

    // --- Merge key for inheritance ---
    internal string MergeKey => Name;
}
```

### Font Item

```csharp
/// <summary>
/// Specification for downloading and installing a font.
/// </summary>
public sealed record FontItem
{
    /// <summary>
    /// Display name of the font.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL to download the font archive (zip file).
    /// </summary>
    public required string Url { get; init; }

    // --- Merge key for inheritance ---
    internal string MergeKey => Name;
}
```

### Snap Item

```csharp
/// <summary>
/// Specification for a snap package to install.
/// </summary>
public sealed record SnapItem
{
    /// <summary>
    /// Name of the snap package.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether to install with --classic confinement.
    /// </summary>
    public bool Classic { get; init; } = false;

    // --- Merge key for inheritance ---
    internal string MergeKey => Name;
}
```

---

## Validation Types

```csharp
namespace Dottie.Configuration.Validation;

/// <summary>
/// Represents a validation error with location information.
/// </summary>
public sealed record ValidationError(
    /// <summary>
    /// JSON-path style location (e.g., "profiles.work.dotfiles[0].source").
    /// </summary>
    string Path,
    
    /// <summary>
    /// Human-readable error message.
    /// </summary>
    string Message,
    
    /// <summary>
    /// Line number in the YAML file, if available.
    /// </summary>
    int? Line = null,
    
    /// <summary>
    /// Column number in the YAML file, if available.
    /// </summary>
    int? Column = null
);

/// <summary>
/// Result of configuration validation.
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<ValidationError> Errors { get; init; } = [];
    
    public static ValidationResult Success() => new();
    public static ValidationResult Failure(params ValidationError[] errors) => 
        new() { Errors = [..errors] };
}
```

---

## Resolved Profile (Post-Inheritance)

```csharp
namespace Dottie.Configuration.Models;

/// <summary>
/// A fully-resolved profile with all inherited values merged.
/// This is the type consumers work with after loading configuration.
/// </summary>
public sealed record ResolvedProfile
{
    /// <summary>
    /// The original profile name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Inheritance chain from root to this profile (e.g., ["base", "default", "work"]).
    /// </summary>
    public required List<string> InheritanceChain { get; init; }

    /// <summary>
    /// Merged dotfile mappings (parent entries first, then child entries).
    /// </summary>
    public required List<DotfileEntry> Dotfiles { get; init; }

    /// <summary>
    /// Merged install block with all inheritance rules applied.
    /// </summary>
    public required InstallBlock Install { get; init; }
}
```

---

## YAML Mapping

The YAML structure maps directly to these types:

```yaml
# Maps to: DottieConfiguration
profiles:                           # Dictionary<string, Profile>
  default:                          # Profile
    dotfiles:                       # List<DotfileEntry>
      - source: dotfiles/bashrc     # DotfileEntry.Source
        target: ~/.bashrc           # DotfileEntry.Target
    install:                        # InstallBlock
      apt:                          # List<string>
        - git
      github:                       # List<GithubReleaseItem>
        - repo: junegunn/fzf
          asset: fzf-*-linux_amd64.tar.gz
          binary: fzf
      snap:                         # List<SnapItem>
        - name: code
          classic: true
  work:                             # Profile
    extends: default                # Profile.Extends
    install:
      apt:
        - awscli                    # Appended to parent apt list
```

---

## Type Relationships Summary

| Parent Type | Child Property | Child Type | Relationship |
|-------------|---------------|------------|--------------|
| `DottieConfiguration` | `Profiles` | `Profile` | 1:* (dictionary) |
| `Profile` | `Dotfiles` | `DotfileEntry` | 1:* (list) |
| `Profile` | `Install` | `InstallBlock` | 1:0..1 (optional) |
| `InstallBlock` | `Github` | `GithubReleaseItem` | 1:* (list) |
| `InstallBlock` | `AptRepo` | `AptRepoItem` | 1:* (list) |
| `InstallBlock` | `Fonts` | `FontItem` | 1:* (list) |
| `InstallBlock` | `Snap` | `SnapItem` | 1:* (list) |
| `InstallBlock` | `Apt`, `Scripts` | `string` | 1:* (list) |
