# Tasks: CLI Command `apply`

**Input**: Design documents from `/specs/009-cli-apply/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Project structure for apply command (minimal - most infrastructure exists)

- [X] T001 [P] Create Models directory in src/Dottie.Cli/Models/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Result models and renderer interface that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T002 [P] Create LinkPhaseResult model in src/Dottie.Cli/Models/LinkPhaseResult.cs
- [X] T003 [P] Create InstallPhaseResult model in src/Dottie.Cli/Models/InstallPhaseResult.cs
- [X] T004 Create ApplyResult model in src/Dottie.Cli/Models/ApplyResult.cs (depends on T002, T003)
- [X] T005 [P] Create IApplyProgressRenderer interface in src/Dottie.Cli/Output/IApplyProgressRenderer.cs
- [X] T006 Create ApplyCommandSettings in src/Dottie.Cli/Commands/ApplyCommandSettings.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Apply Full Profile Configuration (Priority: P1) 🎯 MVP

**Goal**: Single command that links dotfiles and installs software for a profile

**Independent Test**: Run `dottie apply` with a complete config and verify all symlinks created and software installed

### Tests for User Story 1

> **TDD: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T007 [P] [US1] Create ApplyCommandSettingsTests in tests/Dottie.Cli.Tests/Commands/ApplyCommandSettingsTests.cs
- [X] T008 [P] [US1] Create ApplyCommandTests scaffold in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs
- [X] T009 [US1] Add test: ExecuteAsync_WithValidProfile_LinksAndInstalls in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs
- [X] T010 [US1] Add test: ExecuteAsync_WithProfileFlag_UsesSpecifiedProfile in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs
- [X] T011 [US1] Add test: ExecuteAsync_LinksBeforeInstalls in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs

### Implementation for User Story 1

- [X] T012 [US1] Create ApplyCommand class skeleton in src/Dottie.Cli/Commands/ApplyCommand.cs
- [X] T013 [US1] Implement FindRepoRoot helper (extract from existing commands) in src/Dottie.Cli/Commands/ApplyCommand.cs
- [X] T014 [US1] Implement LoadAndResolveProfile helper in src/Dottie.Cli/Commands/ApplyCommand.cs
- [X] T015 [US1] Implement ExecuteLinkPhase method in src/Dottie.Cli/Commands/ApplyCommand.cs
- [X] T016 [US1] Implement ExecuteInstallPhaseAsync method in src/Dottie.Cli/Commands/ApplyCommand.cs
- [X] T017 [US1] Implement ExecuteApplyAsync orchestration in src/Dottie.Cli/Commands/ApplyCommand.cs
- [X] T018 [US1] Implement basic ApplyProgressRenderer (minimal output) in src/Dottie.Cli/Output/ApplyProgressRenderer.cs
- [X] T019 [US1] Register apply command in src/Dottie.Cli/Program.cs

**Checkpoint**: User Story 1 complete - `dottie apply` works with default profile ✅

---

## Phase 4: User Story 2 - Preview Changes with Dry Run (Priority: P2)

**Goal**: `--dry-run` flag shows planned operations without making changes

**Independent Test**: Run `dottie apply --dry-run` and verify no filesystem changes occur

### Tests for User Story 2

- [ ] T020 [P] [US2] Add test: ExecuteAsync_WithDryRun_DoesNotModifyFilesystem in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs
- [ ] T021 [P] [US2] Add test: ExecuteAsync_WithDryRun_ShowsPlannedOperations in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs

### Implementation for User Story 2

- [ ] T022 [US2] Implement RenderDryRunPreview in src/Dottie.Cli/Output/ApplyProgressRenderer.cs
- [ ] T023 [US2] Implement RenderDryRunLinkPreview helper in src/Dottie.Cli/Output/ApplyProgressRenderer.cs
- [ ] T024 [US2] Implement RenderDryRunInstallPreview helper in src/Dottie.Cli/Output/ApplyProgressRenderer.cs
- [ ] T025 [US2] Wire dry-run path in ApplyCommand.ExecuteAsync in src/Dottie.Cli/Commands/ApplyCommand.cs

**Checkpoint**: User Story 2 complete - `dottie apply --dry-run` previews without changes

---

## Phase 5: User Story 3 - Force Apply with Conflict Resolution (Priority: P3)

**Goal**: `--force` flag backs up conflicting files and overwrites them

**Independent Test**: Create conflicting file, run `dottie apply --force`, verify backup created and symlink created

### Tests for User Story 3

- [ ] T026 [P] [US3] Add test: ExecuteAsync_WithConflicts_FailsWithoutForce in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs
- [ ] T027 [P] [US3] Add test: ExecuteAsync_WithForceAndConflicts_BacksUpAndLinks in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs
- [ ] T028 [US3] Add test: ExecuteAsync_WithForce_ReportsBackupLocations in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs

### Implementation for User Story 3

- [ ] T029 [US3] Implement conflict handling in ExecuteLinkPhase in src/Dottie.Cli/Commands/ApplyCommand.cs
- [ ] T030 [US3] Implement RenderConflictError in src/Dottie.Cli/Output/ApplyProgressRenderer.cs
- [ ] T031 [US3] Implement backup location reporting in RenderVerboseSummary in src/Dottie.Cli/Output/ApplyProgressRenderer.cs

**Checkpoint**: User Story 3 complete - `dottie apply --force` handles conflicts safely

---

## Phase 6: User Story 4 - Apply with Profile Inheritance (Priority: P3)

**Goal**: Profile inheritance chains resolve correctly when applying

**Independent Test**: Create profile that extends another, run `dottie apply --profile child`, verify both configs applied

### Tests for User Story 4

- [ ] T032 [P] [US4] Add test: ExecuteAsync_WithInheritedProfile_AppliesBothProfiles in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs
- [ ] T033 [US4] Add test: ExecuteAsync_WithMultiLevelInheritance_ResolvesCorrectly in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs

### Implementation for User Story 4

- [ ] T034 [US4] Verify ProfileMerger integration in LoadAndResolveProfile in src/Dottie.Cli/Commands/ApplyCommand.cs
- [ ] T035 [US4] Add profile name display in RenderVerboseSummary in src/Dottie.Cli/Output/ApplyProgressRenderer.cs

**Checkpoint**: User Story 4 complete - profile inheritance works correctly

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verbose output (FR-010), error handling, documentation

- [ ] T036 [P] Implement RenderLinkPhaseSummary (verbose output) in src/Dottie.Cli/Output/ApplyProgressRenderer.cs
- [ ] T037 [P] Implement RenderInstallPhaseSummary (verbose output) in src/Dottie.Cli/Output/ApplyProgressRenderer.cs
- [ ] T038 Implement RenderOverallSummary (totals) in src/Dottie.Cli/Output/ApplyProgressRenderer.cs
- [ ] T039 [P] Implement RenderError method in src/Dottie.Cli/Output/ApplyProgressRenderer.cs
- [X] T040 [P] Add integration test: apply-basic in tests/integration/scenarios/apply-basic/
- [X] T041 [P] Add integration test: apply-dry-run in tests/integration/scenarios/apply-dry-run/
- [X] T042 [P] Add integration test: apply-force in tests/integration/scenarios/apply-force/
- [ ] T043 Update README.md with apply command documentation
- [ ] T044 Run all tests and verify coverage meets requirements

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
- **Polish (Phase 7)**: Depends on User Story 1 minimum; best after all stories

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational - Uses same ApplyCommand
- **User Story 3 (P3)**: Can start after Foundational - Extends conflict handling
- **User Story 4 (P3)**: Can start after Foundational - Tests inheritance (already implemented in ProfileMerger)

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD)
- Models before command logic
- Core implementation before rendering
- Story complete before moving to next priority

### Parallel Opportunities

Within Foundational (Phase 2):
```
T002 (LinkPhaseResult) ─┐
T003 (InstallPhaseResult) ─┼─► T004 (ApplyResult)
T005 (IApplyProgressRenderer) ─┘
T006 (ApplyCommandSettings) - independent
```

Within User Story 1 (Phase 3):
```
T007 (Settings tests) ─┐
T008 (Command tests)  ─┴─► T009-T011 (specific tests) ─► T012-T019 (implementation)
```

---

## Parallel Example: User Story 1 Tests

```bash
# Launch all tests for User Story 1 together:
Task T007: "ApplyCommandSettingsTests in tests/Dottie.Cli.Tests/Commands/ApplyCommandSettingsTests.cs"
Task T008: "ApplyCommandTests scaffold in tests/Dottie.Cli.Tests/Commands/ApplyCommandTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test `dottie apply` independently
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add User Story 1 → Test → MVP! (`dottie apply` works)
3. Add User Story 2 → Test → `--dry-run` works
4. Add User Story 3 → Test → `--force` works
5. Add User Story 4 → Test → inheritance works
6. Polish → verbose output, docs, integration tests

---

## Summary

| Phase | Tasks | Description |
|-------|-------|-------------|
| Phase 1 | T001 | Setup (1 task) |
| Phase 2 | T002-T006 | Foundational (5 tasks) |
| Phase 3 | T007-T019 | User Story 1 - MVP (13 tasks) |
| Phase 4 | T020-T025 | User Story 2 - Dry Run (6 tasks) |
| Phase 5 | T026-T031 | User Story 3 - Force (6 tasks) |
| Phase 6 | T032-T035 | User Story 4 - Inheritance (4 tasks) |
| Phase 7 | T036-T044 | Polish (9 tasks) |
| **Total** | **44 tasks** | |

### Task Counts by User Story

- Setup/Foundational: 6 tasks
- User Story 1 (P1): 13 tasks
- User Story 2 (P2): 6 tasks
- User Story 3 (P3): 6 tasks
- User Story 4 (P3): 4 tasks
- Polish: 9 tasks

### Parallel Opportunities

- Phase 2: T002, T003, T005 can run in parallel
- Phase 3: T007, T008 can run in parallel
- Phase 4: T020, T021 can run in parallel
- Phase 5: T026, T027 can run in parallel
- Phase 6: T032 independent
- Phase 7: T036, T037, T039, T040, T041, T042 can run in parallel

### MVP Scope

User Story 1 alone delivers a working `dottie apply` command. Total tasks for MVP: **19 tasks** (Phase 1 + Phase 2 + Phase 3).

### Format Validation

✅ All tasks follow checklist format: `- [ ] [TaskID] [P?] [Story?] Description with file path`
