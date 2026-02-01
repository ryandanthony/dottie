# Research: CLI Link Command

**Feature**: 005-cli-link  
**Date**: January 31, 2026  
**Status**: Complete

## Executive Summary

The CLI Link command already has significant implementation in the codebase. This research documents the existing patterns and identifies gaps to fill for spec compliance.

## Technology Stack (Confirmed from Codebase)

| Aspect | Decision | Source |
|--------|----------|--------|
| **Language/Version** | C# / .NET 10.0 | `Dottie.Cli.csproj` |
| **CLI Framework** | Spectre.Console.Cli 0.50.0 | `Dottie.Cli.csproj` |
| **Console Output** | Spectre.Console 0.50.0 | `Dottie.Cli.csproj` |
| **YAML Parsing** | YamlDotNet 16.3.0 | `Dottie.Configuration.csproj` |
| **Testing Framework** | xUnit + FluentAssertions | `LinkingOrchestratorTests.cs` |
| **Target Platform** | Linux (Ubuntu-first), Windows | Constitution |

## Existing Implementation Analysis

### Already Implemented ✅

| Requirement | Implementation | Location |
|-------------|----------------|----------|
| FR-001: Read dotfile mappings | ConfigurationLoader + ProfileMerger | `Parsing/`, `Inheritance/` |
| FR-002: Validate source exists | ConflictDetector | `Linking/ConflictDetector.cs` |
| FR-003: Create parent directories | SymlinkService | `Linking/SymlinkService.cs:30-34` |
| FR-004: Create symlinks | SymlinkService.CreateSymlink() | `Linking/SymlinkService.cs` |
| FR-005: Skip correct symlinks | ConflictDetector → AlreadyLinked | `Linking/ConflictDetector.cs` |
| FR-006: Report conflicts | ConflictFormatter.WriteConflicts() | `Output/ConflictFormatter.cs` |
| FR-007: Default profile | ProfileAwareSettings.ProfileName | `Commands/ProfileAwareSettings.cs` |
| FR-008: Validate profile exists | ProfileMerger.Resolve() | `Inheritance/ProfileMerger.cs` |
| FR-011: --force flag | LinkCommandSettings.Force | `Commands/LinkCommandSettings.cs` |
| FR-012: Backup before overwrite | BackupService | `Linking/BackupService.cs` |
| FR-013: Directory symlinks | SymlinkService handles both | `Linking/SymlinkService.cs:37-44` |
| FR-014: Path expansion (~) | LinkingOrchestrator.ExpandPath() | `Linking/LinkingOrchestrator.cs:166-173` |
| FR-016: Continue on mapping failure | ProcessSafeEntries loop | `Linking/LinkingOrchestrator.cs` |

### Gaps to Implement ❌

| Requirement | Gap | Proposed Solution |
|-------------|-----|-------------------|
| FR-009: --dry-run flag | Not implemented | Add `DryRun` property to `LinkCommandSettings` |
| FR-010: Display planned operations | Not implemented | Add dry-run output in `LinkCommand` before orchestrator call |
| FR-015: Clear error messages | Partial - generic "Failed to create symlink" | Enhance error messages with specific causes |
| FR-017: Non-zero exit on any failure | Partial - returns 1 on failure | Already works, verify comprehensive |
| FR-018: Progress bar | Not implemented | Use `Spectre.Console.Progress()` |
| FR-019: Summary output | Partial - shows counts | Enhance to include all categories |
| FR-020: Symlinks only | Already implemented | Verify no fallback code |
| FR-021: Windows symlink error message | Not implemented | Detect `UnauthorizedAccessException` on Windows |
| FR-022: Backup naming convention | Different format used | Current: `.backup.YYYYMMDD-HHMMSS`, Spec: `.dottie-backup-YYYYMMDD-HHMMSS` |
| FR-023: Mutually exclusive flags | Not implemented | Add validation in settings or command |

## Design Decisions

### Decision 1: Dry Run Implementation Strategy

**Decision**: Implement dry-run at the command level, not orchestrator level.

**Rationale**: 
- The orchestrator already returns structured results (ConflictResult, LinkResult)
- Dry-run can reuse ConflictDetector to preview conflicts without executing
- Keeps orchestrator stateless and testable
- Follows existing pattern where command controls side effects

**Alternatives Rejected**:
- Adding `dryRun` parameter to orchestrator would leak presentation concerns into domain logic

### Decision 2: Progress Bar Integration

**Decision**: Use Spectre.Console.Progress with AnsiConsole.Progress()

**Rationale**:
- Already using Spectre.Console for all output
- Provides configurable progress display
- Integrates seamlessly with existing markup
- Supports both interactive and non-interactive terminals

**Example Pattern**:
```csharp
AnsiConsole.Progress()
    .Start(ctx =>
    {
        var task = ctx.AddTask("[green]Linking dotfiles[/]");
        task.MaxValue = dotfiles.Count;
        foreach (var dotfile in dotfiles)
        {
            // Process...
            task.Increment(1);
        }
    });
```

### Decision 3: Windows Symlink Error Detection

**Decision**: Catch `UnauthorizedAccessException` on symlink creation and provide actionable guidance.

**Rationale**:
- .NET throws `UnauthorizedAccessException` when symlink privileges are missing
- Can detect Windows via `OperatingSystem.IsWindows()`
- Message should include both remediation options (Admin or Developer Mode)

**Message Template**:
```
Error: Unable to create symbolic link - insufficient permissions.

On Windows, symbolic links require either:
  • Run dottie as Administrator, OR
  • Enable Developer Mode in Windows Settings > Update & Security > For developers
```

### Decision 4: Backup Naming Convention Alignment

**Decision**: Update BackupService to use spec-compliant naming: `.dottie-backup-YYYYMMDD-HHMMSS`

**Rationale**:
- Current implementation uses `.backup.YYYYMMDD-HHMMSS`
- Spec explicitly requires `.dottie-backup-YYYYMMDD-HHMMSS`
- Aligning ensures consistency with user-facing documentation
- The `dottie-` prefix makes backups clearly attributable to this tool

**Migration**: No migration needed as this is a new feature. Change backup naming in BackupService.

### Decision 5: Flag Validation Approach

**Decision**: Validate mutually exclusive flags in `LinkCommandSettings.Validate()` override.

**Rationale**:
- Spectre.Console.Cli supports `IValidatableSettings` interface
- Validation runs before command execution
- Provides consistent error messaging
- Follows framework conventions

## Best Practices Applied

### From Constitution

| Principle | Application |
|-----------|-------------|
| Safety & Explicitness | --dry-run previews actions; --force requires explicit flag |
| Idempotency | Skip already-linked files; re-running converges to same state |
| Secure-by-Default | No fallback to less-secure link types |
| Observable | Progress bar, summary output, clear error messages |
| Simplicity | Reuse existing orchestrator; add minimal new code |

### From Existing Patterns

| Pattern | Example | Applied To |
|---------|---------|------------|
| Settings inheritance | `ProfileAwareSettings` | `LinkCommandSettings` already extends it |
| Service injection | `LinkingOrchestrator` constructor | Enables testing with mocks |
| Result types | `LinkExecutionResult`, `BackupResult` | Continue pattern for dry-run |
| Output formatters | `ConflictFormatter`, `ErrorFormatter` | Add dry-run output methods |

## Test Strategy

### Unit Tests (TDD - Write First)
- `LinkCommandSettingsTests` - Validate flag mutual exclusivity
- `LinkingOrchestrator` dry-run behavior (if added there)
- Windows-specific error message generation

### Integration Tests (Existing Pattern)
- End-to-end dry-run scenarios
- Progress bar output verification (mock console)
- Windows symlink error scenarios (platform-conditional)

## Performance Considerations

**SC-006**: Link command completes within 5 seconds for 100 dotfiles.

Current implementation is O(n) where n = number of dotfiles:
1. ConflictDetector.DetectConflicts - one pass
2. ProcessLinking - one pass with filesystem operations

No optimization needed. Filesystem operations are the bottleneck, not code execution.

## Dependencies

### Internal (Already Exists)
- `Dottie.Configuration` - Configuration loading, validation, profile resolution
- `Dottie.Configuration.Linking` - Orchestrator, services

### External (Already Referenced)
- Spectre.Console 0.50.0 - Progress bar, markup output
- Spectre.Console.Cli 0.50.0 - Command framework

### New Dependencies
- None required

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Backup naming change breaks expectations | Low - new feature | Document in release notes |
| Progress bar on non-interactive terminal | Medium | Spectre.Console auto-detects; fallback to simple output |
| Windows symlink detection unreliable | Low | Test on Windows CI; document in README |

## Open Items Resolved

All items from spec are resolved. No NEEDS CLARIFICATION remaining.
