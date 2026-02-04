# Data Model: CLI Command `install`

**Feature**: 007-cli-install
**Date**: February 1, 2026

## Overview

This feature uses existing data models from Dottie.Configuration. No new entities are required. This document references the existing models for clarity.

## Existing Entities (No Changes)

### InstallContext

**Location**: `src/Dottie.Configuration/Installing/InstallContext.cs`

Shared context passed to all installer services.

| Property | Type | Description |
|----------|------|-------------|
| RepoRoot | string | Absolute path to the repository root |
| BinDirectory | string | Target directory for binaries (default: ~/bin/) |
| FontDirectory | string | Target directory for fonts (default: ~/.local/share/fonts/) |
| GithubToken | string? | Optional GitHub API token from environment |
| HasSudo | bool | Whether sudo is available on the system |
| DryRun | bool | Whether to preview without making changes |

### InstallResult

**Location**: `src/Dottie.Configuration/Installing/InstallResult.cs`

Result of a single installation operation.

| Property | Type | Description |
|----------|------|-------------|
| ItemName | string | Identifier for the item (repo name, package name) |
| SourceType | InstallSourceType | Source type that processed this item |
| Status | InstallStatus | Status of the installation |
| Message | string? | Additional details (version, skip reason, error) |
| InstalledPath | string? | Path where item was installed, if applicable |

### InstallStatus

**Location**: `src/Dottie.Configuration/Installing/InstallStatus.cs`

| Value | Description |
|-------|-------------|
| Success | Installation completed successfully |
| Skipped | Item was skipped (already installed) |
| Failed | Installation failed with error |
| Warning | Installation completed with warnings |

### InstallSourceType

**Location**: `src/Dottie.Configuration/Installing/InstallSourceType.cs`

| Value | Priority | Description |
|-------|----------|-------------|
| GithubRelease | 1 | GitHub release binary downloads |
| AptPackage | 2 | Standard APT packages |
| AptRepo | 3 | Private APT repositories |
| Script | 4 | Shell scripts from repository |
| Font | 5 | Font installations |
| SnapPackage | 6 | Snap packages |

### ResolvedProfile

**Location**: `src/Dottie.Configuration/Inheritance/ResolvedProfile.cs`

A profile with all inheritance resolved and merged.

| Property | Type | Description |
|----------|------|-------------|
| Name | string | Profile name |
| Dotfiles | List\<DotfileEntry\> | Merged dotfile mappings |
| Install | InstallBlock? | Merged install block |
| InheritanceChain | List\<string\> | Ordered list of profiles in inheritance chain |

### InstallBlock

**Location**: `src/Dottie.Configuration/Models/InstallBlocks/InstallBlock.cs`

Contains all installation source configurations for a profile.

| Property | Type | Description |
|----------|------|-------------|
| Github | List\<GithubReleaseItem\>? | GitHub release items |
| Apt | List\<string\>? | APT package names |
| AptRepo | List\<AptRepoItem\>? | Private APT repository configs |
| Scripts | List\<string\>? | Script paths relative to repo root |
| Fonts | List\<FontItem\>? | Font download configurations |
| Snap | List\<SnapPackageItem\>? | Snap package configurations |

## Entity Relationships

```
DottieConfiguration
    └── Profiles (Dictionary<string, ConfigProfile>)
            └── ConfigProfile
                    ├── Extends (string?) → references another profile name
                    ├── Dotfiles (List<DotfileEntry>)
                    └── Install (InstallBlock?)
                            ├── Github (List<GithubReleaseItem>)
                            ├── Apt (List<string>)
                            ├── AptRepo (List<AptRepoItem>)
                            ├── Scripts (List<string>)
                            ├── Fonts (List<FontItem>)
                            └── Snap (List<SnapPackageItem>)

ProfileMerger.Resolve(profileName)
    → InheritanceResolveResult
        └── ResolvedProfile (merged from inheritance chain)
                ├── Dotfiles (merged, child overrides parent by target)
                └── Install (merged, lists appended, keyed items merged by identifier)
```

## State Transitions

### Installation Item Lifecycle

```
┌─────────────┐
│  Configured │  (item exists in InstallBlock)
└──────┬──────┘
       │
       ▼
┌─────────────┐     ┌──────────┐
│  Checking   │────►│  Skipped │  (already installed)
└──────┬──────┘     └──────────┘
       │
       ▼
┌─────────────┐     ┌──────────┐
│ Installing  │────►│  Failed  │  (error during install)
└──────┬──────┘     └──────────┘
       │
       ▼
┌─────────────┐
│   Success   │  (installation complete)
└─────────────┘
```

## Validation Rules

### Profile Name

- Must match existing profile in configuration OR be "default"
- "default" returns implicit empty profile if not explicitly defined

### GitHub Release Items

- `repo` must be in format "owner/repo"
- `asset` is a glob pattern for asset filename
- `binary` is the binary name inside the archive
- `version` is optional; defaults to "latest"
- If `version` is specified and not found, fail (no fallback)

### Binary Installation Location

- All GitHub release binaries go to ~/bin/
- Directory is created if it doesn't exist
- Binaries are made executable (chmod +x on Linux)

## No Schema Changes Required

This feature operates entirely within existing data models. The enhancements are behavioral:

1. **InstallCommand**: Use `ProfileMerger` instead of direct lookup
2. **GithubReleaseInstaller**: Add binary existence check
3. **InstallProgressRenderer**: Add grouped failure summary

All existing configuration files remain compatible.
