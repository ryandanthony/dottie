# Data Model: CLI Command `status`

**Feature**: 010-cli-status
**Date**: February 3, 2026

## Overview

This feature introduces new status-checking entities to represent the state of dotfiles and software installations. These models are read-only representations used for display purposes.

## New Entities

### DotfileLinkState (Enum)

**Location**: `src/Dottie.Configuration/Status/DotfileLinkState.cs`

Represents the current state of a dotfile link.

| Value | Description |
|-------|-------------|
| Linked | Symlink exists and points to the correct source file |
| Missing | Target path does not exist (file not linked yet) |
| Broken | Symlink exists but points to a non-existent file |
| Conflicting | A file or directory exists at target but is not the expected symlink |
| Unknown | State cannot be determined (permission error, access issue) |

### DotfileStatusEntry (Record)

**Location**: `src/Dottie.Configuration/Status/DotfileStatusEntry.cs`

Result of checking a single dotfile entry's status.

| Property | Type | Description |
|----------|------|-------------|
| Entry | DotfileEntry | The dotfile configuration being checked |
| State | DotfileLinkState | Current state of the link |
| Message | string? | Additional details (conflict type, error reason, existing target) |
| ExpandedTarget | string | Target path with ~ expanded |

### DotfileStatusChecker (Class)

**Location**: `src/Dottie.Configuration/Status/DotfileStatusChecker.cs`

Service that checks the status of dotfile links.

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| CheckStatus | IReadOnlyList\<DotfileEntry\>, string repoRoot | IReadOnlyList\<DotfileStatusEntry\> | Checks all dotfiles and returns their states |

### SoftwareInstallState (Enum)

**Location**: `src/Dottie.Configuration/Status/SoftwareInstallState.cs`

Represents the current installation state of a software item.

| Value | Description |
|-------|-------------|
| Installed | Software is installed and version matches (if pinned) |
| Missing | Software is not installed |
| Outdated | Software is installed but version doesn't match pinned version |
| Unknown | State cannot be determined (detection error) |

### SoftwareStatusEntry (Record)

**Location**: `src/Dottie.Configuration/Status/SoftwareStatusEntry.cs`

Result of checking a single software item's installation status.

| Property | Type | Description |
|----------|------|-------------|
| ItemName | string | Display name for the software item |
| SourceType | InstallSourceType | The source type (GitHub, APT, Snap, Font, etc.) |
| State | SoftwareInstallState | Current installation state |
| InstalledVersion | string? | Currently installed version (if detectable) |
| TargetVersion | string? | Configured/pinned version (if specified) |
| InstalledPath | string? | Path where the software is installed |
| Message | string? | Additional details (error message for Unknown state) |

### SoftwareStatusChecker (Class)

**Location**: `src/Dottie.Configuration/Status/SoftwareStatusChecker.cs`

Service that checks the installation status of software items.

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| CheckStatusAsync | InstallBlock, InstallContext, CancellationToken | Task\<IReadOnlyList\<SoftwareStatusEntry\>\> | Checks all software items and returns their states |

**Dependencies**:
- `IProcessRunner` for executing detection commands (dpkg, snap, which, --version)

### StatusReport (Record)

**Location**: `src/Dottie.Configuration/Status/StatusReport.cs`

Aggregated status report for display.

| Property | Type | Description |
|----------|------|-------------|
| ProfileName | string | Name of the resolved profile |
| InheritanceChain | IReadOnlyList\<string\> | Chain of inherited profile names |
| DotfileStatuses | IReadOnlyList\<DotfileStatusEntry\> | Status of all dotfiles |
| SoftwareStatuses | IReadOnlyList\<SoftwareStatusEntry\> | Status of all software items |
| HasDotfiles | bool | True if profile has dotfile entries |
| HasSoftware | bool | True if profile has software items |

**Computed Properties**:

| Property | Type | Description |
|----------|------|-------------|
| LinkedCount | int | Number of dotfiles in Linked state |
| MissingDotfilesCount | int | Number of dotfiles in Missing state |
| BrokenCount | int | Number of dotfiles in Broken state |
| ConflictingCount | int | Number of dotfiles in Conflicting state |
| InstalledCount | int | Number of software items in Installed state |
| MissingSoftwareCount | int | Number of software items in Missing state |
| OutdatedCount | int | Number of software items in Outdated state |

## Existing Entities (No Changes)

### DotfileEntry

**Location**: `src/Dottie.Configuration/Models/DotfileEntry.cs`

Used as input to status checking. No modifications needed.

| Property | Type | Description |
|----------|------|-------------|
| Source | string | Path to source file relative to repo root |
| Target | string | Destination path (supports ~ expansion) |

### InstallBlock

**Location**: `src/Dottie.Configuration/Models/InstallBlocks/InstallBlock.cs`

Used as input to software status checking. No modifications needed.

### ResolvedProfile

**Location**: `src/Dottie.Configuration/Inheritance/ResolvedProfile.cs`

Used to get merged dotfiles and install block. No modifications needed.

### InstallSourceType

**Location**: `src/Dottie.Configuration/Installing/InstallSourceType.cs`

Reused to categorize software items in status display. No modifications needed.

### InstallContext

**Location**: `src/Dottie.Configuration/Installing/InstallContext.cs`

Reused to provide context for software detection. No modifications needed.

## CLI Entities

### StatusCommandSettings (Class)

**Location**: `src/Dottie.Cli/Commands/StatusCommandSettings.cs`

Settings for the status command.

| Property | Type | Source | Description |
|----------|------|--------|-------------|
| ProfileName | string? | Inherited from ProfileAwareSettings | Profile to check (default: "default") |
| ConfigPath | string? | Inherited from ProfileAwareSettings | Path to config file |

### StatusCommand (Class)

**Location**: `src/Dottie.Cli/Commands/StatusCommand.cs`

Command implementation following existing patterns.

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| ExecuteAsync | CommandContext, StatusCommandSettings | Task\<int\> | Executes status check and displays results |

### StatusFormatter (Static Class)

**Location**: `src/Dottie.Cli/Output/StatusFormatter.cs`

Formats status output for console display.

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| WriteStatusReport | StatusReport | void | Writes full status report to console |
| WriteDotfileSection | IReadOnlyList\<DotfileStatusEntry\>, bool hasItems | void | Writes dotfiles section |
| WriteSoftwareSection | IReadOnlyList\<SoftwareStatusEntry\>, bool hasItems | void | Writes software section |
| WriteSummary | StatusReport | void | Writes summary line |

## Entity Relationships

```
StatusCommand
    │
    ├── uses ProfileMerger to get ResolvedProfile
    │       └── ResolvedProfile
    │               ├── Dotfiles: List<DotfileEntry>
    │               └── Install: InstallBlock?
    │
    ├── uses DotfileStatusChecker
    │       └── produces List<DotfileStatusEntry>
    │               └── references DotfileEntry
    │
    ├── uses SoftwareStatusChecker
    │       └── produces List<SoftwareStatusEntry>
    │
    └── produces StatusReport
            ├── DotfileStatuses: List<DotfileStatusEntry>
            └── SoftwareStatuses: List<SoftwareStatusEntry>
```

## State Transition Notes

This feature is read-only; no state transitions occur. The status entities represent point-in-time snapshots of filesystem state.

### Dotfile State Determination Logic

```
Target path exists?
├── No → Missing
└── Yes
    └── Is it a symlink?
        ├── No → Conflicting (file or directory)
        └── Yes
            └── Symlink target exists?
                ├── No → Broken
                └── Yes
                    └── Points to expected source?
                        ├── Yes → Linked
                        └── No → Conflicting (mismatched symlink)

On any access error → Unknown
```

### Software State Determination Logic

```
GitHub Release:
├── Binary exists in ~/bin/ or PATH?
│   ├── No → Missing
│   └── Yes
│       └── Version pinned in config?
│           ├── No → Installed
│           └── Yes
│               └── Installed version matches?
│                   ├── Yes → Installed
│                   └── No → Outdated

APT Package:
└── dpkg -s <package> exit code 0?
    ├── No → Missing
    └── Yes → Installed

Snap Package:
└── snap list <package> exit code 0?
    ├── No → Missing
    └── Yes → Installed

Font:
└── Font files exist in fonts directory?
    ├── No → Missing
    └── Yes → Installed

Script:
└── N/A (scripts don't have persistent state)

On any detection error → Unknown
```
