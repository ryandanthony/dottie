# Tasks: Conflict Handling for Dotfile Linking

**Input**: Design documents from `/specs/004-conflict-handling/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included per TDD requirements in constitution.md. All production code requires failing tests first.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create directory structure and base types for linking functionality

- [X] T001 Create `src/Dottie.Configuration/Linking/` directory structure
- [X] T002 [P] Create `ConflictType` enum in `src/Dottie.Configuration/Linking/ConflictType.cs`
- [X] T003 [P] Create `Conflict` record in `src/Dottie.Configuration/Linking/Conflict.cs`
- [X] T004 [P] Create `ConflictResult` record in `src/Dottie.Configuration/Linking/ConflictResult.cs`
- [X] T005 [P] Create `BackupResult` record in `src/Dottie.Configuration/Linking/BackupResult.cs`
- [X] T006 [P] Create `LinkResult` record in `src/Dottie.Configuration/Linking/LinkResult.cs`
- [X] T007 [P] Create `LinkOperationResult` record in `src/Dottie.Configuration/Linking/LinkOperationResult.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core services that ALL user stories depend on - MUST complete before any user story work

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests (TDD - Write First, Verify FAIL)

- [X] T008 [P] Create test class `tests/Dottie.Configuration.Tests/Linking/ConflictDetectorTests.cs` with failing tests for: `DetectConflicts_WhenNoTargetsExist_ReturnsNoConflicts`, `DetectConflicts_WhenTargetFileExists_ReturnsFileConflict`, `DetectConflicts_WhenTargetDirectoryExists_ReturnsDirectoryConflict`, `DetectConflicts_WhenTargetIsCorrectSymlink_ReturnsNoConflict`, `DetectConflicts_WhenTargetIsMismatchedSymlink_ReturnsMismatchedSymlinkConflict`, `DetectConflicts_WithMultipleConflicts_ReturnsAllConflicts`
- [X] T009 [P] Create test class `tests/Dottie.Configuration.Tests/Linking/BackupServiceTests.cs` with failing tests for: `Backup_WhenFileExists_CreatesBackupWithTimestamp`, `Backup_WhenDirectoryExists_CreatesBackupWithTimestamp`, `Backup_WhenBackupNameExists_AppendsNumericSuffix`, `Backup_PreservesOriginalContent`
- [X] T010 [P] Create test class `tests/Dottie.Configuration.Tests/Linking/SymlinkServiceTests.cs` with failing tests for: `CreateSymlink_WhenTargetDoesNotExist_CreatesSymlink`, `CreateSymlink_WhenParentDirectoryMissing_CreatesDirectoryAndSymlink`, `IsCorrectSymlink_WhenPointsToExpectedTarget_ReturnsTrue`, `IsCorrectSymlink_WhenPointsToDifferentTarget_ReturnsFalse`

### Implementation (Make Tests GREEN)

- [X] T011 Implement `ConflictDetector` class in `src/Dottie.Configuration/Linking/ConflictDetector.cs` with `DetectConflicts(IReadOnlyList<DotfileEntry>, string)` method
- [X] T012 Implement `BackupService` class in `src/Dottie.Configuration/Linking/BackupService.cs` with `Backup(string)` method using timestamp pattern `YYYYMMDD-HHMMSS` and numeric suffix for collisions
- [X] T013 Implement `SymlinkService` class in `src/Dottie.Configuration/Linking/SymlinkService.cs` with `CreateSymlink(string, string)` and `IsCorrectSymlink(string, string)` methods

**Checkpoint**: Foundation ready - all foundational tests pass, user story implementation can begin

---

## Phase 3: User Story 1 - Safe Conflict Detection (Priority: P1) 🎯 MVP

**Goal**: Implement `dottie link` command that detects conflicts and fails safely with a structured error list when conflicts exist (without modifying any files)

**Independent Test**: Run `dottie link` on a config where target files exist → command fails with exit code 1 and lists all conflicting files

### Tests for User Story 1 (TDD - Write First, Verify FAIL)

- [X] T014 [P] [US1] Create test class `tests/Dottie.Cli.Tests/Output/ConflictFormatterTests.cs` with tests for: `WriteConflicts_WithFileConflict_OutputsStructuredList`, `WriteConflicts_WithDirectoryConflict_OutputsStructuredList`, `WriteConflicts_WithMismatchedSymlink_OutputsTargetPath`, `WriteConflicts_WithMultipleConflicts_OutputsAllConflicts`
- [X] T015 [P] [US1] Create test class `tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs` with tests for: `Execute_WhenNoConflicts_CreatesSymlinksAndReturnsZero`, `Execute_WhenConflictsExist_ReturnsOneWithConflictList`, `Execute_WhenConflictsExist_DoesNotModifyFiles`, `Execute_WhenAlreadyLinked_SkipsWithoutError`

### Implementation for User Story 1

- [X] T016 [P] [US1] Create `ConflictFormatter` class in `src/Dottie.Cli/Output/ConflictFormatter.cs` with `WriteConflicts(IReadOnlyList<Conflict>)` method following ErrorFormatter pattern
- [X] T017 [P] [US1] Create `LinkCommandSettings` class in `src/Dottie.Cli/Commands/LinkCommandSettings.cs` inheriting from `ProfileAwareSettings`
- [X] T018 [US1] Create `LinkCommand` class in `src/Dottie.Cli/Commands/LinkCommand.cs` implementing conflict detection flow: load config → resolve profile → detect conflicts → if conflicts: format and return 1, else: create symlinks and return 0
- [X] T019 [US1] Register `link` command in `src/Dottie.Cli/Program.cs` with description and examples

**Checkpoint**: User Story 1 complete - `dottie link` works, detects conflicts, fails safely without modifying files

---

## Phase 4: User Story 2 - Force Link with Automatic Backup (Priority: P2)

**Goal**: Add `--force` flag that backs up conflicting files before creating symlinks, displaying per-file backup paths

**Independent Test**: Run `dottie link --force` with existing target files → backups created with timestamp naming, symlinks created, output shows original → backup path mapping

### Tests for User Story 2 (TDD - Write First, Verify FAIL)

- [X] T020 [P] [US2] Add tests to `tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs`: `Execute_WithForceAndConflicts_BacksUpFilesAndCreatesSymlinks`, `Execute_WithForce_DisplaysBackupPaths`, `Execute_WithForceAndBackupCollision_AppendsNumericSuffix`
- [X] T021 [P] [US2] Add tests to `tests/Dottie.Cli.Tests/Output/ConflictFormatterTests.cs`: `WriteBackupResults_WithSuccessfulBackups_OutputsPathMapping`

### Implementation for User Story 2

- [X] T022 [US2] Modify `LinkCommandSettings` in `src/Dottie.Cli/Commands/LinkCommandSettings.cs` to add `[CommandOption("-f|--force")]` property
- [X] T023 [US2] Add `WriteBackupResults(IReadOnlyList<BackupResult>)` method to `ConflictFormatter` in `src/Dottie.Cli/Output/ConflictFormatter.cs`
- [X] T024 [US2] Extend `LinkCommand` in `src/Dottie.Cli/Commands/LinkCommand.cs` to handle `--force` flag: when force && conflicts → backup each conflict → create symlinks → display backup results

**Checkpoint**: User Story 2 complete - `dottie link --force` backs up and links, User Story 1 still works unchanged

---

## Phase 5: User Story 3 - Conflict Reporting in Apply Command (Priority: P3)

**Goal**: Ensure `dottie apply` (when implemented) uses the same conflict handling behavior for its linking portion

**Independent Test**: Run `dottie apply` with conflicting files → same behavior as `dottie link`; run `dottie apply --force` → same backup behavior as `dottie link --force`

### Implementation for User Story 3

> **Note**: The `apply` command is defined in FEATURE-08 and not yet implemented. This phase prepares the linking logic for reuse.

- [X] T025 [P] [US3] Extract shared linking logic from `LinkCommand` into reusable `LinkingOrchestrator` class in `src/Dottie.Configuration/Linking/LinkingOrchestrator.cs` with `ExecuteLink(ResolvedProfile, string repoRoot, bool force)` method
- [X] T026 [US3] Refactor `LinkCommand` to use `LinkingOrchestrator` in `src/Dottie.Cli/Commands/LinkCommand.cs`
- [X] T027 [P] [US3] Add test `tests/Dottie.Configuration.Tests/Linking/LinkingOrchestratorTests.cs` to verify orchestrator logic is reusable

**Checkpoint**: Linking logic is reusable - ready for `apply` command integration in FEATURE-08

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Integration testing, documentation, and cleanup

- [X] T028 [P] Create integration test scenario `tests/integration/scenarios/conflict-handling/dottie.yaml` with dotfiles that will conflict
- [X] T029 [P] Create integration test validation script `tests/integration/scenarios/conflict-handling/validate.sh` testing both conflict detection and force backup scenarios
- [X] T030 [P] Create integration test README `tests/integration/scenarios/conflict-handling/README.md` documenting the scenario
- [X] T031 Run `quickstart.md` validation steps and verify all tests pass with ≥90% line / ≥80% branch coverage
- [X] T032 Run `dotnet build` with no analyzer warnings (FestinaLente.CodeStandards)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 - BLOCKS all user stories
- **Phase 3-5 (User Stories)**: All depend on Phase 2 completion
- **Phase 6 (Polish)**: Depends on at least User Story 1 being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Phase 2 - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Phase 2 - Builds on US1 `LinkCommand` but independently testable
- **User Story 3 (P3)**: Can start after Phase 2 - Refactors for reuse, requires US1 implementation

### Within Each Phase

- Tests MUST be written and FAIL before implementation
- Types/enums before services
- Services before commands
- Core implementation before CLI registration

### Parallel Opportunities

**Phase 1 (after T001):**
```
T002, T003, T004, T005, T006, T007 — all in parallel (different files)
```

**Phase 2 (tests):**
```
T008, T009, T010 — all in parallel (different test files)
```

**Phase 2 (implementation):**
```
T011, T012, T013 — sequential (services may share types)
```

**Phase 3 (User Story 1):**
```
T014, T015 — tests in parallel
T016, T017 — implementation in parallel (different files)
T018 → T019 — sequential (command then registration)
```

**Phase 4 (User Story 2):**
```
T020, T021 — tests in parallel
T022 → T023 → T024 — sequential (settings → formatter → command)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T007)
2. Complete Phase 2: Foundational (T008-T013)
3. Complete Phase 3: User Story 1 (T014-T019)
4. **STOP and VALIDATE**: Test `dottie link` independently
5. Deploy/demo if ready - safe conflict detection works!

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add User Story 1 → Test → **MVP: Safe conflict detection**
3. Add User Story 2 → Test → **Backup-and-force functionality**
4. Add User Story 3 → Test → **Reusable for apply command**
5. Polish → Integration tests, coverage verification

---

## Summary

| Phase | Task Count | Description |
|-------|------------|-------------|
| Phase 1: Setup | 7 | Directory structure and data types |
| Phase 2: Foundational | 6 | Core services (ConflictDetector, BackupService, SymlinkService) |
| Phase 3: US1 (P1) | 6 | Safe conflict detection with `dottie link` |
| Phase 4: US2 (P2) | 5 | `--force` flag with automatic backup |
| Phase 5: US3 (P3) | 3 | Reusable orchestrator for `apply` command |
| Phase 6: Polish | 5 | Integration tests, coverage, cleanup |
| **Total** | **32** | |

### Parallel Opportunities

- **Phase 1**: 6 tasks can run in parallel (after T001)
- **Phase 2**: 3 test tasks in parallel, then 3 implementation tasks
- **Phase 3**: 2 test tasks in parallel, 2 implementation tasks in parallel
- **Phase 4**: 2 test tasks in parallel
- **Phase 6**: 3 integration test tasks in parallel

### Independent Test Criteria

| User Story | Independent Test |
|------------|------------------|
| US1 (P1) | Run `dottie link` → fails with conflict list, no files modified |
| US2 (P2) | Run `dottie link --force` → backups created, symlinks created, paths displayed |
| US3 (P3) | `LinkingOrchestrator` can be called from any command context |

### MVP Scope

Complete through **Phase 3 (User Story 1)** for minimum viable product delivering safe conflict detection.
