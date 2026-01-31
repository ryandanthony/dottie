# Data Model: Conflict Handling for Dotfile Linking

**Feature**: 004-conflict-handling  
**Date**: 2026-01-30  
**Purpose**: Define entities for conflict detection, backup operations, and linking results

---

## Overview

This feature introduces new types in the `Dottie.Configuration.Linking` namespace to support conflict detection and safe file operations. These types integrate with the existing `DotfileEntry` model from configuration.

---

## Entity Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                     ResolvedProfile (existing)                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Dotfiles: List<DotfileEntry>   ← input for linking       │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ conflict detection
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       ConflictResult                             │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ HasConflicts: bool                                       │    │
│  │ Conflicts: IReadOnlyList<Conflict>                       │    │
│  │ SafeEntries: IReadOnlyList<DotfileEntry>                 │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ 0..*
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                          Conflict                                │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Entry: DotfileEntry             ← source dotfile entry   │    │
│  │ TargetPath: string              ← expanded target path   │    │
│  │ Type: ConflictType              ← file/dir/symlink       │    │
│  │ ExistingTarget: string?         ← for mismatched symlinks│    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ backup (with --force)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        BackupResult                              │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ IsSuccess: bool                                          │    │
│  │ OriginalPath: string                                     │    │
│  │ BackupPath: string?             ← null if failed         │    │
│  │ Error: string?                  ← null if success        │    │
│  │ Timestamp: DateTime                                      │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ link creation
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                         LinkResult                               │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ IsSuccess: bool                                          │    │
│  │ Entry: DotfileEntry                                      │    │
│  │ ExpandedTargetPath: string                               │    │
│  │ BackupResult: BackupResult?     ← if conflict existed    │    │
│  │ Error: string?                  ← null if success        │    │
│  │ WasSkipped: bool                ← true if already linked │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

## New Types

### ConflictType (Enum)

```csharp
// src/Dottie.Configuration/Linking/ConflictType.cs

namespace Dottie.Configuration.Linking;

/// <summary>
/// The type of conflict detected at a target path.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// No conflict - target doesn't exist or is already correctly linked.
    /// </summary>
    None = 0,

    /// <summary>
    /// Target path exists as a regular file.
    /// </summary>
    File = 1,

    /// <summary>
    /// Target path exists as a directory.
    /// </summary>
    Directory = 2,

    /// <summary>
    /// Target path is a symlink pointing to a different location than expected.
    /// </summary>
    MismatchedSymlink = 3
}
```

---

### Conflict

```csharp
// src/Dottie.Configuration/Linking/Conflict.cs

namespace Dottie.Configuration.Linking;

/// <summary>
/// Represents a conflict detected at a target path during dotfile linking.
/// </summary>
public sealed record Conflict
{
    /// <summary>
    /// Gets the dotfile entry that caused the conflict.
    /// </summary>
    public required DotfileEntry Entry { get; init; }

    /// <summary>
    /// Gets the expanded target path where the conflict was detected.
    /// </summary>
    /// <remarks>
    /// This is the fully expanded path (e.g., ~ expanded to /home/user).
    /// </remarks>
    public required string TargetPath { get; init; }

    /// <summary>
    /// Gets the type of conflict detected.
    /// </summary>
    public required ConflictType Type { get; init; }

    /// <summary>
    /// Gets the existing symlink target, if the conflict is a mismatched symlink.
    /// </summary>
    /// <remarks>
    /// Only populated when <see cref="Type"/> is <see cref="ConflictType.MismatchedSymlink"/>.
    /// </remarks>
    public string? ExistingTarget { get; init; }
}
```

---

### ConflictResult

```csharp
// src/Dottie.Configuration/Linking/ConflictResult.cs

namespace Dottie.Configuration.Linking;

/// <summary>
/// The result of conflict detection for a set of dotfile entries.
/// </summary>
public sealed record ConflictResult
{
    /// <summary>
    /// Gets a value indicating whether any conflicts were detected.
    /// </summary>
    public bool HasConflicts => Conflicts.Count > 0;

    /// <summary>
    /// Gets the list of conflicts detected.
    /// </summary>
    public required IReadOnlyList<Conflict> Conflicts { get; init; }

    /// <summary>
    /// Gets the list of dotfile entries that are safe to link (no conflicts).
    /// </summary>
    /// <remarks>
    /// Includes entries where the target doesn't exist or is already correctly linked.
    /// </remarks>
    public required IReadOnlyList<DotfileEntry> SafeEntries { get; init; }

    /// <summary>
    /// Gets the list of entries that are already correctly linked (skipped).
    /// </summary>
    public required IReadOnlyList<DotfileEntry> AlreadyLinked { get; init; }

    /// <summary>
    /// Creates a result with no conflicts.
    /// </summary>
    public static ConflictResult NoConflicts(
        IReadOnlyList<DotfileEntry> safeEntries,
        IReadOnlyList<DotfileEntry> alreadyLinked) =>
        new()
        {
            Conflicts = [],
            SafeEntries = safeEntries,
            AlreadyLinked = alreadyLinked
        };

    /// <summary>
    /// Creates a result with conflicts.
    /// </summary>
    public static ConflictResult WithConflicts(
        IReadOnlyList<Conflict> conflicts,
        IReadOnlyList<DotfileEntry> safeEntries,
        IReadOnlyList<DotfileEntry> alreadyLinked) =>
        new()
        {
            Conflicts = conflicts,
            SafeEntries = safeEntries,
            AlreadyLinked = alreadyLinked
        };
}
```

---

### BackupResult

```csharp
// src/Dottie.Configuration/Linking/BackupResult.cs

namespace Dottie.Configuration.Linking;

/// <summary>
/// The result of a backup operation.
/// </summary>
public sealed record BackupResult
{
    /// <summary>
    /// Gets a value indicating whether the backup succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the original path that was backed up.
    /// </summary>
    public required string OriginalPath { get; init; }

    /// <summary>
    /// Gets the backup path where the original was moved.
    /// </summary>
    /// <remarks>
    /// Null if the backup failed.
    /// </remarks>
    public string? BackupPath { get; init; }

    /// <summary>
    /// Gets the error message if the backup failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets the timestamp when the backup was created.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Creates a successful backup result.
    /// </summary>
    public static BackupResult Success(string originalPath, string backupPath, DateTime timestamp) =>
        new()
        {
            IsSuccess = true,
            OriginalPath = originalPath,
            BackupPath = backupPath,
            Timestamp = timestamp
        };

    /// <summary>
    /// Creates a failed backup result.
    /// </summary>
    public static BackupResult Failure(string originalPath, string error) =>
        new()
        {
            IsSuccess = false,
            OriginalPath = originalPath,
            Error = error,
            Timestamp = DateTime.Now
        };
}
```

---

### LinkResult

```csharp
// src/Dottie.Configuration/Linking/LinkResult.cs

namespace Dottie.Configuration.Linking;

/// <summary>
/// The result of linking a single dotfile entry.
/// </summary>
public sealed record LinkResult
{
    /// <summary>
    /// Gets a value indicating whether the link operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the dotfile entry that was linked.
    /// </summary>
    public required DotfileEntry Entry { get; init; }

    /// <summary>
    /// Gets the expanded target path where the symlink was created.
    /// </summary>
    public required string ExpandedTargetPath { get; init; }

    /// <summary>
    /// Gets the backup result if a conflict was resolved.
    /// </summary>
    /// <remarks>
    /// Only populated when --force was used and a conflict existed.
    /// </remarks>
    public BackupResult? BackupResult { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entry was skipped because it was already correctly linked.
    /// </summary>
    public bool WasSkipped { get; init; }

    /// <summary>
    /// Creates a successful link result.
    /// </summary>
    public static LinkResult Success(
        DotfileEntry entry,
        string expandedTargetPath,
        BackupResult? backupResult = null) =>
        new()
        {
            IsSuccess = true,
            Entry = entry,
            ExpandedTargetPath = expandedTargetPath,
            BackupResult = backupResult
        };

    /// <summary>
    /// Creates a skipped result (already linked).
    /// </summary>
    public static LinkResult Skipped(DotfileEntry entry, string expandedTargetPath) =>
        new()
        {
            IsSuccess = true,
            Entry = entry,
            ExpandedTargetPath = expandedTargetPath,
            WasSkipped = true
        };

    /// <summary>
    /// Creates a failed link result.
    /// </summary>
    public static LinkResult Failure(DotfileEntry entry, string expandedTargetPath, string error) =>
        new()
        {
            IsSuccess = false,
            Entry = entry,
            ExpandedTargetPath = expandedTargetPath,
            Error = error
        };
}
```

---

### LinkOperationResult

```csharp
// src/Dottie.Configuration/Linking/LinkOperationResult.cs

namespace Dottie.Configuration.Linking;

/// <summary>
/// The aggregate result of a link operation across all dotfile entries.
/// </summary>
public sealed record LinkOperationResult
{
    /// <summary>
    /// Gets a value indicating whether the entire operation succeeded.
    /// </summary>
    public bool IsSuccess => FailedLinks.Count == 0;

    /// <summary>
    /// Gets the list of successfully linked entries.
    /// </summary>
    public required IReadOnlyList<LinkResult> SuccessfulLinks { get; init; }

    /// <summary>
    /// Gets the list of skipped entries (already correctly linked).
    /// </summary>
    public required IReadOnlyList<LinkResult> SkippedLinks { get; init; }

    /// <summary>
    /// Gets the list of failed link attempts.
    /// </summary>
    public required IReadOnlyList<LinkResult> FailedLinks { get; init; }

    /// <summary>
    /// Gets the total number of entries processed.
    /// </summary>
    public int TotalProcessed => SuccessfulLinks.Count + SkippedLinks.Count + FailedLinks.Count;
}
```

---

## Existing Types (No Changes Needed)

### DotfileEntry

```csharp
// src/Dottie.Configuration/Models/DotfileEntry.cs
// Already has Source/Target - NO CHANGES NEEDED

public sealed record DotfileEntry
{
    public required string Source { get; init; }
    public required string Target { get; init; }
}
```

---

## CLI Settings Extension

### ProfileAwareSettings (Modification)

```csharp
// src/Dottie.Cli/Commands/ProfileAwareSettings.cs
// ADD --force flag

public abstract class ProfileAwareSettings : CommandSettings
{
    [Description("Profile to use (default: 'default')")]
    [CommandOption("-p|--profile")]
    public string? ProfileName { get; set; }

    [Description("Path to the configuration file")]
    [CommandOption("-c|--config")]
    public string? ConfigPath { get; set; }

    // NEW
    [Description("Force linking by backing up conflicting files")]
    [CommandOption("-f|--force")]
    public bool Force { get; set; }
}
```

---

## Service Interfaces

### ConflictDetector

```csharp
// src/Dottie.Configuration/Linking/ConflictDetector.cs

namespace Dottie.Configuration.Linking;

/// <summary>
/// Detects conflicts for dotfile linking operations.
/// </summary>
public sealed class ConflictDetector
{
    /// <summary>
    /// Detects conflicts for the given dotfile entries.
    /// </summary>
    /// <param name="dotfiles">The dotfile entries to check.</param>
    /// <param name="repoRoot">The repository root path.</param>
    /// <returns>The conflict detection result.</returns>
    public ConflictResult DetectConflicts(
        IReadOnlyList<DotfileEntry> dotfiles,
        string repoRoot);
}
```

### BackupService

```csharp
// src/Dottie.Configuration/Linking/BackupService.cs

namespace Dottie.Configuration.Linking;

/// <summary>
/// Creates backups of files and directories.
/// </summary>
public sealed class BackupService
{
    /// <summary>
    /// Creates a backup of the specified path.
    /// </summary>
    /// <param name="path">The path to backup.</param>
    /// <returns>The backup result.</returns>
    public BackupResult Backup(string path);
}
```

### SymlinkService

```csharp
// src/Dottie.Configuration/Linking/SymlinkService.cs

namespace Dottie.Configuration.Linking;

/// <summary>
/// Creates and verifies symbolic links.
/// </summary>
public sealed class SymlinkService
{
    /// <summary>
    /// Creates a symbolic link.
    /// </summary>
    /// <param name="linkPath">The path where the symlink will be created.</param>
    /// <param name="targetPath">The path the symlink will point to.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool CreateSymlink(string linkPath, string targetPath);

    /// <summary>
    /// Checks if the path is a symlink pointing to the expected target.
    /// </summary>
    public bool IsCorrectSymlink(string linkPath, string expectedTarget);
}
```

---

## Summary

| Type | Purpose | Location |
|------|---------|----------|
| `ConflictType` | Enum for conflict categories | `Linking/ConflictType.cs` |
| `Conflict` | Single conflict instance | `Linking/Conflict.cs` |
| `ConflictResult` | Aggregate conflict detection result | `Linking/ConflictResult.cs` |
| `BackupResult` | Single backup operation result | `Linking/BackupResult.cs` |
| `LinkResult` | Single link operation result | `Linking/LinkResult.cs` |
| `LinkOperationResult` | Aggregate link operation result | `Linking/LinkOperationResult.cs` |
| `ConflictDetector` | Service for detecting conflicts | `Linking/ConflictDetector.cs` |
| `BackupService` | Service for creating backups | `Linking/BackupService.cs` |
| `SymlinkService` | Service for symlink operations | `Linking/SymlinkService.cs` |
