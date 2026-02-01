# Data Model: CLI Link Command

**Feature**: 005-cli-link  
**Date**: January 31, 2026

## Entity Overview

This feature operates primarily on existing entities defined in prior specifications. The link command creates relationships (symlinks) between source files in the repository and target locations in the filesystem.

```
┌─────────────────────┐      ┌─────────────────────┐
│  DottieConfiguration │──────│    ConfigProfile    │
│  (from FEATURE-01)  │  1:n │   (from FEATURE-02) │
└─────────────────────┘      └──────────┬──────────┘
                                        │ 1:n
                                        ▼
                             ┌─────────────────────┐
                             │    DotfileEntry     │
                             │  (from FEATURE-01)  │
                             └──────────┬──────────┘
                                        │ 1:1 (link operation)
                                        ▼
                             ┌─────────────────────┐
                             │    LinkResult       │
                             │  (this feature)     │
                             └─────────────────────┘
```

## Existing Entities (Reference)

### DotfileEntry (from Dottie.Configuration.Models)

Represents a single source-to-target mapping.

```csharp
public sealed class DotfileEntry
{
    public string Source { get; set; }  // Relative path in repo
    public string Target { get; set; }  // Target path (supports ~)
}
```

### ResolvedProfile (from Dottie.Configuration.Inheritance)

A fully resolved profile with all inherited dotfiles merged.

```csharp
public sealed class ResolvedProfile
{
    public string Name { get; }
    public IReadOnlyList<DotfileEntry> Dotfiles { get; }
    public IReadOnlyList<string> InheritanceChain { get; }
}
```

## Link Command Entities

### LinkCommandSettings

Command-line arguments for the link command.

| Field | Type | Default | Validation |
|-------|------|---------|------------|
| ProfileName | string? | "default" | Must exist in config |
| ConfigPath | string? | dottie.yaml in repo root | Must exist |
| Force | bool | false | - |
| DryRun | bool | false | Mutually exclusive with Force |

### LinkResult

Result of a single link operation.

| Field | Type | Description |
|-------|------|-------------|
| Entry | DotfileEntry | The dotfile mapping processed |
| ExpandedTargetPath | string | Full path with ~ expanded |
| Status | LinkStatus | Success, Skipped, Failed |
| Error | string? | Error message if failed |
| BackupResult | BackupResult? | Backup info if force was used |

### LinkStatus (Enum)

```
Success  - Symlink created successfully
Skipped  - Already correctly linked
Failed   - Could not create symlink
```

### BackupResult

Result of backing up an existing file before overwrite.

| Field | Type | Description |
|-------|------|-------------|
| OriginalPath | string | Path of file that was backed up |
| BackupPath | string? | Path where backup was stored |
| Timestamp | DateTimeOffset | When backup was created |
| IsSuccess | bool | Whether backup succeeded |
| Error | string? | Error message if failed |

**Backup Naming Convention**: `{originalPath}.dottie-backup-YYYYMMDD-HHMMSS`

### Conflict

Represents a conflict detected before linking.

| Field | Type | Description |
|-------|------|-------------|
| Entry | DotfileEntry | The dotfile mapping with conflict |
| TargetPath | string | Expanded target path |
| ConflictType | ConflictType | Type of conflict |
| ExistingTarget | string? | What the existing symlink points to |

### ConflictType (Enum)

```
ExistingFile      - Target exists as a regular file
ExistingDirectory - Target exists as a regular directory
WrongSymlink      - Target is a symlink pointing elsewhere
```

### LinkExecutionResult

Aggregate result of the entire link operation.

| Field | Type | Description |
|-------|------|-------------|
| IsBlocked | bool | True if conflicts prevented execution |
| ConflictResult | ConflictResult? | Conflict details if blocked |
| LinkResult | LinkOperationResult? | Results if executed |
| BackupResults | IReadOnlyList<BackupResult> | Backups created |

### LinkOperationResult

Summary of all link operations performed.

| Field | Type | Description |
|-------|------|-------------|
| SuccessfulLinks | IReadOnlyList<LinkResult> | Links created |
| SkippedLinks | IReadOnlyList<LinkResult> | Already correct |
| FailedLinks | IReadOnlyList<LinkResult> | Could not create |
| IsSuccess | bool | True if no failures |

## State Transitions

### Link Operation State Machine

```
                    ┌─────────────────┐
                    │   Initial       │
                    └────────┬────────┘
                             │ Load config
                             ▼
                    ┌─────────────────┐
                    │   Validating    │──────────► Error (invalid config)
                    └────────┬────────┘
                             │ Resolve profile
                             ▼
                    ┌─────────────────┐
                    │ Detecting       │
                    │ Conflicts       │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
        ┌──────────┐   ┌──────────┐   ┌──────────┐
        │ Blocked  │   │ Dry Run  │   │ Linking  │
        │ (no --f) │   │ (--dry)  │   │          │
        └──────────┘   └──────────┘   └────┬─────┘
                             │              │
                             ▼              ▼
                       ┌──────────┐   ┌──────────┐
                       │ Preview  │   │ Complete │
                       │ Output   │   │          │
                       └──────────┘   └──────────┘
```

## Validation Rules

### DotfileEntry Validation (Pre-Link)
- Source path must exist in repository
- Source path must be relative (no leading /)
- Target path must be non-empty

### Symlink Creation Validation (Runtime)
- Target parent directory must be creatable
- User must have symlink permission (Windows)
- Target must not be a circular reference

## Relationships to Other Features

| Related Feature | Relationship |
|-----------------|--------------|
| FEATURE-01 (Configuration) | Provides DottieConfiguration, DotfileEntry |
| FEATURE-02 (Profiles) | Provides profile resolution and inheritance |
| FEATURE-04 (Conflict Handling) | Defines conflict detection patterns |
