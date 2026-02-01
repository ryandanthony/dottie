# Service Contract: LinkingOrchestrator

**Feature**: 005-cli-link  
**Date**: January 31, 2026

## Overview

The `LinkingOrchestrator` coordinates the linking process for dotfile entries. This contract documents the expected behavior and integration points.

## Interface

```csharp
public sealed class LinkingOrchestrator
{
    /// <summary>
    /// Executes the linking process for the given resolved profile.
    /// </summary>
    /// <param name="profile">The resolved profile containing dotfiles to link.</param>
    /// <param name="repoRoot">The repository root path.</param>
    /// <param name="force">Whether to force linking by backing up conflicts.</param>
    /// <returns>The result of the link operation.</returns>
    public LinkExecutionResult ExecuteLink(
        ResolvedProfile profile, 
        string repoRoot, 
        bool force);
}
```

## Preconditions

| Condition | Enforcement |
|-----------|-------------|
| `profile` is not null | Throws `ArgumentNullException` |
| `repoRoot` is not null or empty | Throws `ArgumentException` |
| Profile has been resolved (inheritance applied) | Caller responsibility |
| Configuration has been validated | Caller responsibility |

## Postconditions

### When `force = false` and conflicts exist:
- Returns `LinkExecutionResult` with `IsBlocked = true`
- `ConflictResult` contains all detected conflicts
- **No filesystem changes made**

### When `force = false` and no conflicts:
- Returns `LinkExecutionResult` with `IsBlocked = false`
- `LinkResult` contains successful, skipped, and failed operations
- Symlinks created for safe entries
- Already-correct symlinks skipped

### When `force = true`:
- Returns `LinkExecutionResult` with `IsBlocked = false`
- `BackupResults` contains backup operations performed
- Conflicting files backed up before overwriting
- Symlinks created for all entries (conflicts and safe)

## Result Contract

### LinkExecutionResult

```csharp
public sealed class LinkExecutionResult
{
    public bool IsBlocked { get; }
    public ConflictResult? ConflictResult { get; }
    public LinkOperationResult? LinkResult { get; }
    public IReadOnlyList<BackupResult> BackupResults { get; }
    
    public static LinkExecutionResult Blocked(ConflictResult conflicts);
    public static LinkExecutionResult Completed(
        LinkOperationResult result, 
        IReadOnlyList<BackupResult> backups);
}
```

### LinkOperationResult

```csharp
public sealed class LinkOperationResult
{
    public IReadOnlyList<LinkResult> SuccessfulLinks { get; }
    public IReadOnlyList<LinkResult> SkippedLinks { get; }
    public IReadOnlyList<LinkResult> FailedLinks { get; }
    public bool IsSuccess => FailedLinks.Count == 0;
}
```

## Dependency Contracts

### ConflictDetector

```csharp
public ConflictResult DetectConflicts(
    IReadOnlyList<DotfileEntry> dotfiles, 
    string repoRoot);
```

**Returns**:
- `Conflicts`: Entries where target exists and is not correct symlink
- `AlreadyLinked`: Entries where target is already correct symlink
- `SafeEntries`: Entries where target does not exist

### BackupService

```csharp
public BackupResult Backup(string path);
```

**Behavior**:
- Creates backup at `{path}.dottie-backup-YYYYMMDD-HHMMSS`
- Handles both files and directories
- Returns failure result on error (does not throw)

### SymlinkService

```csharp
public bool CreateSymlink(string linkPath, string targetPath);
public bool IsCorrectSymlink(string linkPath, string expectedTarget);
```

**CreateSymlink Behavior**:
- Creates parent directories if needed
- Handles both file and directory symlinks
- Returns false on error (does not throw)

## Error Handling

The orchestrator does **not throw exceptions** for filesystem errors. Instead:

1. Individual failures are recorded in `FailedLinks`
2. Processing continues to remaining entries
3. Caller determines exit behavior based on `IsSuccess`

## Thread Safety

The `LinkingOrchestrator` is **not thread-safe**. Create a new instance for each operation.

## Testing Seams

Constructor accepts optional dependencies for testing:

```csharp
public LinkingOrchestrator(
    ConflictDetector? conflictDetector = null,
    BackupService? backupService = null,
    SymlinkService? symlinkService = null)
```
