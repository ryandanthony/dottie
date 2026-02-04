# Tasks: CLI Command `install`

**Input**: Design documents from `/specs/007-cli-install/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Included per TDD requirement in constitution. Tests written FIRST, must FAIL before implementation.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/Dottie.Cli/`, `src/Dottie.Configuration/`
- **Tests**: `tests/Dottie.Cli.Tests/`, `tests/Dottie.Configuration.Tests/`

---

## Phase 1: Setup

**Purpose**: No new project setup needed - enhancing existing codebase

- [X] T001 Verify build passes with `dotnet build --warnaserror` in solution root
- [X] T002 Verify existing tests pass with `dotnet test` in solution root

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core changes that enable all user stories

**⚠️ CRITICAL**: These must complete before user story implementation

- [X] T003 Add tests for binary existence check method in tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs
- [X] T004 Implement binary existence check (~/bin/ then PATH via `which`) in src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs
- [X] T005 Add tests for grouped failure summary in tests/Dottie.Cli.Tests/Output/InstallProgressRendererTests.cs
- [X] T006 Implement `RenderGroupedFailures()` method in src/Dottie.Cli/Output/InstallProgressRenderer.cs

**Checkpoint**: Foundation ready - binary detection and failure grouping available for all stories

---

## Phase 3: User Story 1 - Basic Software Installation (Priority: P1) 🎯 MVP

**Goal**: Install all software from config with priority ordering and already-installed detection

**Independent Test**: Run `dottie install` with valid config, verify all tools installed in priority order, already-installed tools skipped

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T007 [P] [US1] Add test: install executes sources in priority order in tests/Dottie.Cli.Tests/Commands/InstallCommandTests.cs
- [X] T008 [P] [US1] Add test: install skips already-installed GitHub binaries in tests/Dottie.Cli.Tests/Commands/InstallCommandTests.cs
- [X] T009 [P] [US1] Add test: install displays grouped failure summary in tests/Dottie.Cli.Tests/Commands/InstallCommandTests.cs

### Implementation for User Story 1

- [X] T010 [US1] Update `ExecuteInstallAsync` to call `RenderGroupedFailures` after processing in src/Dottie.Cli/Commands/InstallCommand.cs
- [X] T011 [US1] Integrate binary existence check into GitHub installer flow in src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs
- [X] T012 [US1] Verify existing priority order loop in `RunInstallersAsync` matches spec (GitHub→APT→APT-repo→Scripts→Fonts→Snap) in src/Dottie.Cli/Commands/InstallCommand.cs

**Checkpoint**: User Story 1 complete - basic installation with idempotency working

---

## Phase 4: User Story 4 - Skip Already Installed Tools (Priority: P1)

**Goal**: Detect and skip pre-installed tools (idempotency guarantee)

**Independent Test**: Run `dottie install` on machine with some tools pre-installed, verify those tools skipped with "already installed" message

### Tests for User Story 4

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T013 [P] [US4] Add test: binary in ~/bin/ is detected as already installed in tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs
- [X] T014 [P] [US4] Add test: binary in PATH but not ~/bin/ is detected as already installed in tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs
- [X] T015 [P] [US4] Add test: binary not found anywhere triggers installation in tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs

### Implementation for User Story 4

- [X] T016 [US4] Add `IsBinaryInstalled` method checking ~/bin/ first in src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs
- [X] T017 [US4] Add `CheckPathForBinary` method using `which` command in src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs
- [X] T018 [US4] Update `InstallGithubReleaseItemAsync` to check before downloading in src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs

**Checkpoint**: User Story 4 complete - idempotent installation guaranteed

---

## Phase 5: User Story 2 - Profile-Specific Installation (Priority: P2)

**Goal**: Support --profile flag with full inheritance resolution

**Independent Test**: Run `dottie install --profile work` where "work" extends "default", verify both profile's sources installed

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T019 [P] [US2] Add test: install uses ProfileMerger for inheritance resolution in tests/Dottie.Cli.Tests/Commands/InstallCommandTests.cs
- [X] T020 [P] [US2] Add test: install includes inherited profile's install sources in tests/Dottie.Cli.Tests/Commands/InstallCommandTests.cs
- [X] T021 [P] [US2] Add test: install with nonexistent profile returns error with available profiles in tests/Dottie.Cli.Tests/Commands/InstallCommandTests.cs

### Implementation for User Story 2

- [X] T022 [US2] Replace direct profile lookup with `ProfileMerger.Resolve()` in src/Dottie.Cli/Commands/InstallCommand.cs
- [X] T023 [US2] Update error message to list available profiles when profile not found in src/Dottie.Cli/Commands/InstallCommand.cs
- [X] T024 [US2] Use `ResolvedProfile.Install` instead of `ConfigProfile.Install` in src/Dottie.Cli/Commands/InstallCommand.cs

**Checkpoint**: User Story 2 complete - profile inheritance working

---

## Phase 6: User Story 3 - Preview Changes with Dry Run (Priority: P2)

**Goal**: --dry-run shows accurate "would install" vs "would skip" based on system state

**Independent Test**: Run `dottie install --dry-run`, verify no changes made, output shows which tools would be installed vs skipped

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T025 [P] [US3] Add test: dry-run checks system state for GitHub binaries in tests/Dottie.Cli.Tests/Commands/InstallCommandTests.cs
- [X] T026 [P] [US3] Add test: dry-run shows "would skip" for already-installed tools in tests/Dottie.Cli.Tests/Commands/InstallCommandTests.cs
- [X] T027 [P] [US3] Add test: dry-run makes no filesystem changes in tests/Dottie.Cli.Tests/Commands/InstallCommandTests.cs

### Implementation for User Story 3

- [X] T028 [US3] Update GitHub installer dry-run to check binary existence before reporting in src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs
- [X] T029 [US3] Update dry-run result messages to show "would install" vs "would skip" in src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs
- [X] T030 [US3] Verify dry-run header displays "Dry Run Mode: Previewing installation without making changes" in src/Dottie.Cli/Commands/InstallCommand.cs

**Checkpoint**: User Story 3 complete - accurate dry-run preview working

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation

- [X] T031 [P] Run all unit tests with `dotnet test` and verify 100% pass
- [X] T032 [P] Run integration tests with `.\tests\run-integration-tests.ps1` and verify pass
- [X] T033 [P] Verify code coverage meets 90% line / 80% branch requirement
- [X] T034 Validate quickstart.md scenarios manually in specs/007-cli-install/quickstart.md
- [X] T035 Update README.md if install command usage has changed
- [X] T036 [P] Add integration test: install-idempotency (binary skip on second run)
- [X] T037 [P] Add integration test: install-profile-inheritance (extends parent sources)

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup ──────────────────────────┐
                                         │
Phase 2: Foundational ───────────────────┤
                                         │
         ┌───────────────────────────────┘
         │
         ▼
┌────────────────────┐  ┌────────────────────┐
│ Phase 3: US1 (P1)  │  │ Phase 4: US4 (P1)  │
│ Basic Installation │  │ Skip Installed     │
└────────┬───────────┘  └─────────┬──────────┘
         │                        │
         ▼                        ▼
┌────────────────────┐  ┌────────────────────┐
│ Phase 5: US2 (P2)  │  │ Phase 6: US3 (P2)  │
│ Profile Support    │  │ Dry Run Preview    │
└────────┬───────────┘  └─────────┬──────────┘
         │                        │
         └───────────┬────────────┘
                     ▼
            Phase 7: Polish
```

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational (Phase 2) - Core installation flow
- **US4 (P1)**: Depends on Foundational (Phase 2) - Binary detection (can parallel with US1)
- **US2 (P2)**: Can start after Foundational - Profile inheritance (can parallel with US1/US4)
- **US3 (P2)**: Can start after Foundational - Dry run enhancement (can parallel with US1/US4)

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Implementation tasks in sequence within each story
- Story complete before marking checkpoint

### Parallel Opportunities

**After Foundational Phase (T003-T006):**
- T007, T008, T009 can run in parallel (US1 tests)
- T013, T014, T015 can run in parallel (US4 tests)
- T019, T020, T021 can run in parallel (US2 tests)
- T025, T026, T027 can run in parallel (US3 tests)
- T031, T032, T033 can run in parallel (Polish tests)

---

## Parallel Example: User Story 1 & 4 (P1 Stories)

```bash
# After Foundational phase, launch both P1 story test suites:
# US1 Tests:
Task: T007 - test: install executes sources in priority order
Task: T008 - test: install skips already-installed GitHub binaries
Task: T009 - test: install displays grouped failure summary

# US4 Tests (parallel with US1 tests):
Task: T013 - test: binary in ~/bin/ is detected as already installed
Task: T014 - test: binary in PATH but not ~/bin/ is detected as already installed
Task: T015 - test: binary not found anywhere triggers installation
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 4)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational (T003-T006) - **CRITICAL GATE**
3. Complete Phase 3: User Story 1 (T007-T012)
4. Complete Phase 4: User Story 4 (T013-T018)
5. **STOP and VALIDATE**: Basic idempotent installation working
6. Demo MVP: `dottie install` works with skip-if-installed

### Incremental Delivery

1. Setup + Foundational → Core ready
2. Add US1 + US4 → Test → MVP with idempotency ✓
3. Add US2 → Test → Profile inheritance working ✓
4. Add US3 → Test → Dry run accurate ✓
5. Polish → Full feature complete ✓

---

## Summary

| Phase | Tasks | Stories | Status |
|-------|-------|---------|--------|
| Setup | T001-T002 | - | Prerequisites |
| Foundational | T003-T006 | - | Blocking |
| User Story 1 | T007-T012 | US1 (P1) | MVP |
| User Story 4 | T013-T018 | US4 (P1) | MVP |
| User Story 2 | T019-T024 | US2 (P2) | Enhancement |
| User Story 3 | T025-T030 | US3 (P2) | Enhancement |
| Polish | T031-T035 | - | Finalization |

**Total Tasks**: 35  
**MVP Scope**: T001-T018 (18 tasks) - US1 + US4  
**Full Scope**: T001-T035 (35 tasks) - All stories
