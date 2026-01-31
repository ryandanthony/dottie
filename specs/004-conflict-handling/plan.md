# Implementation Plan: Conflict Handling for Dotfile Linking

**Branch**: `004-conflict-handling` | **Date**: 2026-01-30 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-conflict-handling/spec.md`

**Note**: This template is filled in by the `/speckit.plan` workflow. In this repo, see
`.specify/scripts/powershell/setup-plan.ps1` and `.specify/scripts/powershell/create-new-feature.ps1`.

## Summary

Implement safe conflict detection for dotfile linking operations (`dottie link`, `dottie apply`) that prevents data loss by default and provides a `--force` flag for backup-and-overwrite behavior. When conflicts are detected without `--force`, the operation fails with a structured list of all conflicting files. With `--force`, conflicting files are backed up using timestamped naming before symlink creation.

## Technical Context

**Language/Version**: .NET 10 (C# 13), Native AOT / trimmed self-contained deployment  
**Primary Dependencies**:
- `Spectre.Console` — Rich terminal output for conflict and backup reporting
- `Spectre.Console.Cli` — Command-line argument parsing with `--force` flag
- `System.IO` / `System.IO.Abstractions` — Filesystem operations (symlinks, file/directory backup)

**Storage**: Filesystem only (dotfiles and backups)  
**Testing**: xUnit + FluentAssertions, extending existing test patterns  
**Target Platform**: Linux x64/arm64 (Ubuntu/Debian), single-file trimmed executable  
**Project Type**: Single CLI application (existing)  
**Performance Goals**: Conflict detection <5s for up to 50 dotfile entries (SC-005)  
**Constraints**: No new external dependencies; must work with existing filesystem permissions  
**Scale/Scope**: Typical configs with 10-50 dotfile entries

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **Safety & Explicitness** | ✅ Pass | Default behavior is safe (fail on conflicts); `--force` explicitly required for overwrites; backups preserve original data |
| **Idempotency** | ✅ Pass | Re-running with same config converges to same state; existing correct symlinks are skipped |
| **Security** | ✅ Pass | No external network calls; validates filesystem permissions before operations; no privilege escalation |
| **Ubuntu-First** | ✅ Pass | Uses standard POSIX symlink operations available on Ubuntu |
| **Observability** | ✅ Pass | Clear error messages listing all conflicts; backup paths displayed after `--force` operations |
| **Simplicity** | ✅ Pass | Extends existing infrastructure; minimal new types (Conflict, BackupResult) |
| **Testing** | ✅ Pass | All conflict detection and backup logic testable via existing patterns |

**Constitution violations**: None identified.

## Project Structure

### Documentation (this feature)

```text
specs/004-conflict-handling/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── examples/
│       ├── dotfiles-with-conflicts.yaml
│       └── dotfiles-force-backup.yaml
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Dottie.Cli/
│   ├── Commands/
│   │   ├── ProfileAwareSettings.cs      # MODIFY: Add --force flag to base class
│   │   ├── LinkCommand.cs               # NEW: dottie link command
│   │   ├── LinkCommandSettings.cs       # NEW: Settings for link command
│   │   └── ValidateCommand.cs           # EXISTING (no changes)
│   ├── Output/
│   │   ├── ErrorFormatter.cs            # EXISTING (no changes)
│   │   └── ConflictFormatter.cs         # NEW: Format conflict output
│   └── Dottie.Cli.csproj
│
├── Dottie.Configuration/
│   ├── Linking/                         # NEW: Directory for linking logic
│   │   ├── ConflictDetector.cs          # NEW: Detect conflicts
│   │   ├── ConflictResult.cs            # NEW: Result type for conflicts
│   │   ├── Conflict.cs                  # NEW: Single conflict entity
│   │   ├── ConflictType.cs              # NEW: Enum for conflict types
│   │   ├── BackupService.cs             # NEW: Backup file/directory
│   │   ├── BackupResult.cs              # NEW: Result type for backups
│   │   └── SymlinkService.cs            # NEW: Create symlinks
│   └── Dottie.Configuration.csproj

tests/
├── Dottie.Configuration.Tests/
│   ├── Linking/                         # NEW: Tests for linking logic
│   │   ├── ConflictDetectorTests.cs     # NEW: Conflict detection tests
│   │   ├── BackupServiceTests.cs        # NEW: Backup service tests
│   │   └── SymlinkServiceTests.cs       # NEW: Symlink service tests
│   └── Fixtures/
│       ├── linking/                     # NEW: Test fixtures for linking
│       │   ├── basic-conflict/
│       │   └── force-backup/
│       └── [existing fixtures]
└── Dottie.Cli.Tests/
    └── Commands/
        └── LinkCommandTests.cs          # NEW: Link command tests

tests/integration/
└── scenarios/
    ├── basic-symlinks/                  # EXISTING
    └── conflict-handling/               # NEW: Integration test scenario
        ├── dottie.yml
        ├── README.md
        └── validate.sh
```

**Structure Decision**: Extend existing single project structure following patterns from 003-profiles. Create new `Linking/` directory in Dottie.Configuration for conflict and backup logic. The `--force` flag added to `ProfileAwareSettings` base class for reuse across commands.

## Complexity Tracking

> No constitution violations — this section is empty.
