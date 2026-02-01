# Implementation Plan: CLI Link Command

**Branch**: `005-cli-link` | **Date**: January 31, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-cli-link/spec.md`

## Summary

Enhance the existing `dottie link` command to support dry-run preview mode, progress bar output, improved error messages (especially for Windows symlink permissions), and mutually exclusive flag validation. The link command creates symbolic links from dotfiles in the repository to their target locations in the user's filesystem.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: Spectre.Console 0.50.0, Spectre.Console.Cli 0.50.0, YamlDotNet 16.3.0  
**Storage**: Filesystem (symlinks, backups)  
**Testing**: xUnit, FluentAssertions (existing patterns)  
**Target Platform**: Linux (Ubuntu-first), Windows  
**Project Type**: Single CLI application with library  
**Performance Goals**: ≤5 seconds for 100 dotfile mappings (SC-006)  
**Constraints**: Must work without admin on Linux; Windows requires Developer Mode or Admin  
**Scale/Scope**: Personal dotfile repositories (typically 10-50 mappings)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Safety & Explicitness | ✅ PASS | --dry-run previews; --force required for overwrites |
| Idempotency | ✅ PASS | Skips already-correct symlinks |
| Security | ✅ PASS | Uses symlinks only; no fallback to less-secure options |
| Security values | ✅ PASS | No secrets involved; validates all inputs |
| Compatibility | ✅ PASS | Ubuntu-first; Windows-specific error guidance |
| Observability | ✅ PASS | Progress bar, summary output, clear errors |
| Decision rules | ✅ PASS | Reuses existing services; minimal new code |
| Guiding principles | ✅ PASS | Simple additions to working implementation |
| Testing philosophy | ✅ PASS | TDD for all new code; behavior-focused |

## Project Structure

### Documentation (this feature)

```text
specs/005-cli-link/
├── plan.md              # This file
├── research.md          # Phase 0 output - technology analysis
├── data-model.md        # Phase 1 output - entity documentation
├── quickstart.md        # Phase 1 output - developer guide
├── contracts/           # Phase 1 output - interface contracts
│   ├── cli-interface.md
│   └── orchestrator-service.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Dottie.Cli/
│   ├── Commands/
│   │   ├── LinkCommand.cs           # [MODIFY] Add dry-run, progress bar
│   │   ├── LinkCommandSettings.cs   # [MODIFY] Add DryRun, validation
│   │   ├── ProfileAwareSettings.cs  # [EXISTING] Base settings class
│   │   └── ValidateCommand.cs       # [REFERENCE] Pattern example
│   ├── Output/
│   │   ├── ConflictFormatter.cs     # [MODIFY] Add dry-run preview methods
│   │   └── ErrorFormatter.cs        # [REFERENCE] Error output pattern
│   ├── Utilities/
│   │   └── RepoRootFinder.cs        # [EXISTING] Utility
│   ├── Program.cs                   # [EXISTING] Command registration
│   └── Dottie.Cli.csproj
└── Dottie.Configuration/
    ├── Linking/
    │   ├── BackupService.cs         # [MODIFY] Update naming convention
    │   ├── ConflictDetector.cs      # [EXISTING] Conflict detection
    │   ├── LinkingOrchestrator.cs   # [EXISTING] Main orchestration
    │   ├── SymlinkService.cs        # [MODIFY] Windows error handling
    │   └── *.cs                     # Other existing types
    ├── Models/
    │   └── *.cs                     # Existing model types
    └── Dottie.Configuration.csproj

tests/
├── Dottie.Cli.Tests/
│   └── Commands/
│       └── LinkCommandSettingsTests.cs  # [CREATE] Flag validation tests
└── Dottie.Configuration.Tests/
    └── Linking/
        ├── BackupServiceTests.cs        # [MODIFY] Update naming tests
        ├── LinkingOrchestratorTests.cs  # [EXISTING] Reference
        └── SymlinkServiceTests.cs       # [MODIFY] Windows error tests
```

**Structure Decision**: Single project structure maintained. Changes are incremental additions to existing well-structured codebase.

## Implementation Summary

### What's Already Done (12 of 23 FRs)
- FR-001 through FR-008: Configuration loading, validation, profile resolution
- FR-011, FR-012: Force flag, backup creation
- FR-013, FR-014: Directory symlinks, path expansion
- FR-016: Continue on failure

### What Needs Implementation (11 of 23 FRs)
- FR-009, FR-010: Dry-run flag and preview output
- FR-015: Enhanced error messages
- FR-017: Exit code verification (likely already works)
- FR-018, FR-019: Progress bar and summary output
- FR-020: Symlinks only (verify no fallback)
- FR-021: Windows symlink permission error message
- FR-022: Backup naming convention update
- FR-023: Mutually exclusive flag validation

## Complexity Tracking

No constitution violations requiring justification. All changes follow existing patterns.

| Change | Complexity | Justification |
|--------|------------|---------------|
| DryRun flag | Low | Standard Spectre.Console.Cli pattern |
| Progress bar | Low | Built-in Spectre.Console.Progress |
| Flag validation | Low | IValidatableSettings pattern |
| Windows error | Low | Platform detection + message |
| Backup naming | Low | String format change only |
