# Research: Conflict Handling for Dotfile Linking

**Feature**: 004-conflict-handling  
**Date**: 2026-01-30  
**Purpose**: Analyze filesystem operations, symlink patterns, and backup strategies

---

## 1. Existing Codebase Analysis

### Decision: **Create new Linking/ namespace for conflict and symlink logic**

### Current State

| Component | Location | Status |
|-----------|----------|--------|
| `DotfileEntry` model | `src/Dottie.Configuration/Models/DotfileEntry.cs` | ✅ Complete - has Source/Target paths |
| `ResolvedProfile` | `src/Dottie.Configuration/Inheritance/ResolvedProfile.cs` | ✅ Complete - provides merged dotfiles list |
| `ProfileAwareSettings` | `src/Dottie.Cli/Commands/ProfileAwareSettings.cs` | ✅ Can extend with `--force` flag |
| `ErrorFormatter` | `src/Dottie.Cli/Output/ErrorFormatter.cs` | ✅ Pattern to follow for ConflictFormatter |
| Symlink creation | N/A | ❌ Not implemented - new feature |
| Conflict detection | N/A | ❌ Not implemented - new feature |
| Backup service | N/A | ❌ Not implemented - new feature |

### Rationale

The existing infrastructure provides:
- Dotfile entries with source/target paths from configuration
- Profile resolution to get the merged list of dotfiles
- CLI command patterns to follow
- Output formatting patterns for errors

This feature adds the core linking functionality that operates on resolved dotfile entries.

---

## 2. Symlink Operations in .NET

### Decision: **Use System.IO.File and System.IO.Directory native symlink APIs**

### .NET Symlink Support

.NET 6+ provides native symlink support without P/Invoke:

```csharp
// Create symlink
File.CreateSymbolicLink(linkPath, targetPath);
Directory.CreateSymbolicLink(linkPath, targetPath);

// Check if symlink
FileSystemInfo info = new FileInfo(path);
info.LinkTarget; // null if not a symlink, target path if symlink

// Check symlink target
FileInfo linkInfo = new FileInfo(symlinkPath);
string? target = linkInfo.LinkTarget;
bool pointsToCorrectTarget = target == expectedSource;
```

### Rationale

- Native .NET APIs are available since .NET 6
- No external dependencies required
- Cross-platform support (Linux primary target)
- Simpler than using System.IO.Abstractions for MVP

### Alternatives Considered

| Approach | Rejected Because |
|----------|------------------|
| System.IO.Abstractions | Adds dependency; overkill for initial implementation |
| P/Invoke to libc | Unnecessary with .NET 6+ native support |
| Shell commands (ln -s) | Non-portable, subprocess overhead |

---

## 3. Conflict Detection Strategy

### Decision: **Pre-scan all targets before any modifications**

### Rationale (FR-001, FR-002, FR-003)

The specification requires:
1. Detect ALL conflicts before failing (not fail on first)
2. Report all conflicts in one error message
3. Make NO modifications when conflicts exist (without --force)

### Implementation Approach

```csharp
public sealed class ConflictDetector
{
    public ConflictResult DetectConflicts(
        IReadOnlyList<DotfileEntry> dotfiles,
        string repoRoot)
    {
        var conflicts = new List<Conflict>();
        
        foreach (var entry in dotfiles)
        {
            var targetPath = ExpandPath(entry.Target);
            var sourcePath = Path.Combine(repoRoot, entry.Source);
            
            var conflict = CheckForConflict(targetPath, sourcePath);
            if (conflict is not null)
            {
                conflicts.Add(conflict);
            }
        }
        
        return conflicts.Count > 0
            ? ConflictResult.WithConflicts(conflicts)
            : ConflictResult.NoConflicts();
    }
}
```

### Conflict Types (FR-010, FR-011)

| Scenario | ConflictType | Action |
|----------|--------------|--------|
| Target doesn't exist | None | Create symlink |
| Target is file | `File` | Conflict - backup required |
| Target is directory | `Directory` | Conflict - backup required |
| Target is symlink → correct source | None | Skip (already linked) |
| Target is symlink → different path | `MismatchedSymlink` | Conflict - backup required |

---

## 4. Backup Strategy

### Decision: **In-place backup with timestamped suffix**

### Naming Pattern (FR-007)

```
<original-filename>.backup.<YYYYMMDD-HHMMSS>[.N]
```

Examples:
- `.bashrc` → `.bashrc.backup.20260130-143022`
- `.config` (dir) → `.config.backup.20260130-143022`
- Collision: `.bashrc.backup.20260130-143022.1`, `.bashrc.backup.20260130-143022.2`

### Implementation (FR-006, FR-008, FR-012)

```csharp
public sealed class BackupService
{
    public BackupResult Backup(string originalPath)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var backupPath = GetUniqueBackupPath(originalPath, timestamp);
        
        try
        {
            if (Directory.Exists(originalPath))
            {
                Directory.Move(originalPath, backupPath);
            }
            else
            {
                File.Move(originalPath, backupPath);
            }
            
            return BackupResult.Success(originalPath, backupPath);
        }
        catch (Exception ex)
        {
            return BackupResult.Failure(originalPath, ex.Message);
        }
    }
    
    private string GetUniqueBackupPath(string original, string timestamp)
    {
        var basePath = $"{original}.backup.{timestamp}";
        if (!Path.Exists(basePath))
            return basePath;
            
        // Add numeric suffix for uniqueness
        int suffix = 1;
        while (Path.Exists($"{basePath}.{suffix}"))
            suffix++;
        return $"{basePath}.{suffix}";
    }
}
```

### Safety Guarantees (FR-012)

1. Backup is created BEFORE deleting/moving original
2. If backup fails, original remains untouched
3. Atomic operation: Move() is typically atomic on same filesystem

---

## 5. Path Expansion

### Decision: **Reuse existing PathExpander utility**

### Current State

The codebase already has path expansion in `Dottie.Configuration.Utilities.PathExpander`:

```csharp
// Existing - expands ~ to home directory
PathExpander.Expand("~/.bashrc") // → /home/user/.bashrc
```

### Usage in Conflict Detection

```csharp
var targetPath = PathExpander.Expand(entry.Target);
```

---

## 6. CLI Flag Pattern

### Decision: **Add --force to ProfileAwareSettings base class**

### Rationale

- `--force` applies to both `link` and `apply` commands
- Follows existing pattern where `--profile` is in base class
- Consistent with Unix conventions (`-f`, `--force`)

### Implementation

```csharp
// Extend existing base class
public abstract class ProfileAwareSettings : CommandSettings
{
    [Description("Profile to use (default: 'default')")]
    [CommandOption("-p|--profile")]
    public string? ProfileName { get; set; }

    [Description("Path to the configuration file")]
    [CommandOption("-c|--config")]
    public string? ConfigPath { get; set; }

    // NEW: Add force flag
    [Description("Force linking by backing up conflicting files")]
    [CommandOption("-f|--force")]
    public bool Force { get; set; }
}
```

---

## 7. Output Formatting

### Decision: **Structured list format following ErrorFormatter pattern**

### Conflict Output (without --force)

```
Error: Conflicting files detected. Use --force to backup and overwrite.

Conflicts:
  • ~/.bashrc (file)
  • ~/.config/nvim (directory)
  • ~/.gitconfig (symlink → /other/path)

Found 3 conflict(s).
```

### Success Output (with --force)

```
Backed up and linked 3 files:
  • ~/.bashrc → ~/.bashrc.backup.20260130-143022
  • ~/.config/nvim → ~/.config/nvim.backup.20260130-143022
  • ~/.gitconfig → ~/.gitconfig.backup.20260130-143022
```

### Implementation

```csharp
public static class ConflictFormatter
{
    public static void WriteConflicts(IReadOnlyList<Conflict> conflicts)
    {
        AnsiConsole.MarkupLine(
            "[red]Error:[/] Conflicting files detected. Use [yellow]--force[/] to backup and overwrite.");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("Conflicts:");
        foreach (var conflict in conflicts)
        {
            var typeLabel = conflict.Type switch
            {
                ConflictType.File => "file",
                ConflictType.Directory => "directory",
                ConflictType.MismatchedSymlink => $"symlink → {conflict.ExistingTarget}",
                _ => "unknown"
            };
            AnsiConsole.MarkupLine($"  [red]•[/] {conflict.TargetPath} ({typeLabel})");
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[red]Found {conflicts.Count} conflict(s).[/]");
    }
    
    public static void WriteBackupResults(IReadOnlyList<BackupResult> results)
    {
        AnsiConsole.MarkupLine($"[green]Backed up and linked {results.Count} file(s):[/]");
        foreach (var result in results)
        {
            AnsiConsole.MarkupLine($"  [green]•[/] {result.OriginalPath} → {result.BackupPath}");
        }
    }
}
```

---

## 8. Error Handling

### Decision: **Fail-fast with clear error messages**

### Error Scenarios (FR-013)

| Scenario | Behavior |
|----------|----------|
| Permission denied (backup) | Fail with "Permission denied: cannot backup {path}" |
| Disk full | Fail with "Insufficient disk space to backup {path}" |
| Permission denied (symlink) | Fail with "Permission denied: cannot create symlink at {path}" |
| Source doesn't exist | Fail with "Source file not found: {path}" |

### Transaction-like Behavior

While not truly transactional, the implementation should:
1. Validate ALL operations can succeed before starting
2. If backup fails mid-operation, report which files were modified
3. Never leave system in inconsistent state (partial backup)

---

## 9. Testing Strategy

### Unit Tests

| Component | Test Focus |
|-----------|------------|
| `ConflictDetector` | All conflict type detection, path expansion |
| `BackupService` | File/directory backup, timestamp naming, collision handling |
| `SymlinkService` | Symlink creation, target verification |
| `ConflictFormatter` | Output format for conflicts and backup results |

### Integration Tests

New scenario: `tests/integration/scenarios/conflict-handling/`
- Pre-create conflicting files
- Run `dottie link` → verify failure
- Run `dottie link --force` → verify backups and symlinks

### Test Fixtures

Use temporary directories with `Path.GetTempPath()` to avoid filesystem pollution.

---

## Summary: Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Symlink API | System.IO native | .NET 6+ support, no dependencies |
| Conflict detection | Pre-scan all before action | Report all at once per spec |
| Backup location | In-place with suffix | Discoverable, simple |
| Timestamp format | YYYYMMDD-HHMMSS | Sortable, readable |
| CLI flag | `-f/--force` in base class | Reusable across commands |
| Output format | Spectre.Console structured | Consistent with existing |
