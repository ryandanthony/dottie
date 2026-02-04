# Tasks: CLI Status Command

**Input**: Design documents from `/specs/010-cli-status/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Tests**: Included per project TDD mandate (constitution requirement).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Single project**: `src/Dottie.Cli/`, `src/Dottie.Configuration/`, `tests/`
- Paths follow existing repository structure

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create Status namespace and supporting types

- [X] T001 Create Status directory structure in src/Dottie.Configuration/Status/
- [X] T002 [P] Create DotfileLinkState enum in src/Dottie.Configuration/Status/DotfileLinkState.cs
- [X] T003 [P] Create SoftwareInstallState enum in src/Dottie.Configuration/Status/SoftwareInstallState.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core status entry models and report that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 [P] Create DotfileStatusEntry record in src/Dottie.Configuration/Status/DotfileStatusEntry.cs
- [X] T005 [P] Create SoftwareStatusEntry record in src/Dottie.Configuration/Status/SoftwareStatusEntry.cs
- [X] T006 Create StatusReport record in src/Dottie.Configuration/Status/StatusReport.cs
- [X] T007 [P] Create StatusCommandSettings class in src/Dottie.Cli/Commands/StatusCommandSettings.cs
- [X] T008 Register StatusCommand in Program.cs CLI configuration in src/Dottie.Cli/Program.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - View Dotfile Link Status (Priority: P1) 🎯 MVP

**Goal**: Display dotfile link states (linked/missing/broken/conflicting/unknown)

**Independent Test**: Run `dottie status` with mixed dotfile states and verify correct categorization

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation (TDD mandate)**

- [X] T009 [P] [US1] Create DotfileStatusCheckerTests test class in tests/Dottie.Configuration.Tests/Status/DotfileStatusCheckerTests.cs
- [X] T010 [P] [US1] Add test: CheckStatus_WhenSymlinkPointsToCorrectSource_ReturnsLinkedState in tests/Dottie.Configuration.Tests/Status/DotfileStatusCheckerTests.cs
- [X] T011 [P] [US1] Add test: CheckStatus_WhenTargetDoesNotExist_ReturnsMissingState in tests/Dottie.Configuration.Tests/Status/DotfileStatusCheckerTests.cs
- [X] T012 [P] [US1] Add test: CheckStatus_WhenSymlinkTargetDoesNotExist_ReturnsBrokenState in tests/Dottie.Configuration.Tests/Status/DotfileStatusCheckerTests.cs
- [X] T013 [P] [US1] Add test: CheckStatus_WhenFileExistsButIsNotSymlink_ReturnsConflictingState in tests/Dottie.Configuration.Tests/Status/DotfileStatusCheckerTests.cs
- [X] T014 [P] [US1] Add test: CheckStatus_WhenAccessDenied_ReturnsUnknownState in tests/Dottie.Configuration.Tests/Status/DotfileStatusCheckerTests.cs

### Implementation for User Story 1

- [X] T015 [US1] Create DotfileStatusChecker class in src/Dottie.Configuration/Status/DotfileStatusChecker.cs
- [X] T016 [US1] Implement CheckStatus method with all five state detection cases in src/Dottie.Configuration/Status/DotfileStatusChecker.cs
- [X] T017 [P] [US1] Create StatusFormatter static class in src/Dottie.Cli/Output/StatusFormatter.cs
- [X] T018 [US1] Implement WriteDotfileSection method with Spectre.Console table in src/Dottie.Cli/Output/StatusFormatter.cs
- [X] T019 [US1] Create StatusCommand skeleton implementing AsyncCommand<StatusCommandSettings> in src/Dottie.Cli/Commands/StatusCommand.cs
- [X] T020 [US1] Implement StatusCommand.ExecuteAsync to load config, resolve profile, check dotfile status, and display in src/Dottie.Cli/Commands/StatusCommand.cs
- [X] T021 [US1] Implement empty section handling ("No dotfiles configured") in StatusFormatter in src/Dottie.Cli/Output/StatusFormatter.cs

**Checkpoint**: User Story 1 complete - `dottie status` displays dotfile states

---

## Phase 4: User Story 2 - View Software Installation Status (Priority: P2)

**Goal**: Display software installation states (installed/missing/outdated)

**Independent Test**: Run `dottie status` with mixed software states and verify correct categorization

### Tests for User Story 2 ⚠️

- [X] T022 [P] [US2] Create SoftwareStatusCheckerTests test class in tests/Dottie.Configuration.Tests/Status/SoftwareStatusCheckerTests.cs
- [X] T023 [P] [US2] Add test: CheckStatusAsync_WhenGitHubBinaryExists_ReturnsInstalledState in tests/Dottie.Configuration.Tests/Status/SoftwareStatusCheckerTests.cs
- [X] T024 [P] [US2] Add test: CheckStatusAsync_WhenGitHubBinaryMissing_ReturnsMissingState in tests/Dottie.Configuration.Tests/Status/SoftwareStatusCheckerTests.cs
- [X] T025 [P] [US2] Add test: CheckStatusAsync_WhenGitHubVersionMismatch_ReturnsOutdatedState in tests/Dottie.Configuration.Tests/Status/SoftwareStatusCheckerTests.cs
- [X] T026 [P] [US2] Add test: CheckStatusAsync_WhenAptPackageInstalled_ReturnsInstalledState in tests/Dottie.Configuration.Tests/Status/SoftwareStatusCheckerTests.cs
- [X] T027 [P] [US2] Add test: CheckStatusAsync_WhenAptPackageMissing_ReturnsMissingState in tests/Dottie.Configuration.Tests/Status/SoftwareStatusCheckerTests.cs
- [X] T028 [P] [US2] Add test: CheckStatusAsync_WhenDetectionFails_ReturnsUnknownState in tests/Dottie.Configuration.Tests/Status/SoftwareStatusCheckerTests.cs

### Implementation for User Story 2

- [X] T029 [US2] Create SoftwareStatusChecker class in src/Dottie.Configuration/Status/SoftwareStatusChecker.cs
- [X] T030 [US2] Implement CheckStatusAsync method with GitHub binary detection in src/Dottie.Configuration/Status/SoftwareStatusChecker.cs
- [X] T031 [US2] Add APT package detection using dpkg -s in SoftwareStatusChecker in src/Dottie.Configuration/Status/SoftwareStatusChecker.cs
- [X] T032 [US2] Add Snap package detection using snap list in SoftwareStatusChecker in src/Dottie.Configuration/Status/SoftwareStatusChecker.cs
- [X] T033 [US2] Add Font detection via file existence in SoftwareStatusChecker in src/Dottie.Configuration/Status/SoftwareStatusChecker.cs
- [X] T034 [US2] Add version comparison for GitHub releases with pinned versions in src/Dottie.Configuration/Status/SoftwareStatusChecker.cs
- [X] T035 [US2] Implement WriteSoftwareSection method with Spectre.Console table in src/Dottie.Cli/Output/StatusFormatter.cs
- [X] T036 [US2] Integrate SoftwareStatusChecker into StatusCommand.ExecuteAsync in src/Dottie.Cli/Commands/StatusCommand.cs
- [X] T037 [US2] Implement empty section handling ("No software configured") in StatusFormatter in src/Dottie.Cli/Output/StatusFormatter.cs

**Checkpoint**: User Stories 1 AND 2 complete - `dottie status` displays both dotfiles and software

---

## Phase 5: User Story 3 - Status for Specific Profile (Priority: P2)

**Goal**: Support `--profile <name>` option to filter status

**Independent Test**: Run `dottie status --profile work` and verify only work profile items displayed

### Tests for User Story 3 ⚠️

- [X] T038 [P] [US3] Create StatusCommandTests test class in tests/Dottie.Cli.Tests/Commands/StatusCommandTests.cs
- [X] T039 [P] [US3] Add test: Execute_WithProfileOption_ShowsOnlyProfileItems in tests/Dottie.Cli.Tests/Commands/StatusCommandTests.cs
- [X] T040 [P] [US3] Add test: Execute_WithInheritedProfile_ShowsParentAndChildItems in tests/Dottie.Cli.Tests/Commands/StatusCommandTests.cs
- [X] T041 [P] [US3] Add test: Execute_WithNonExistentProfile_ReturnsErrorExitCode in tests/Dottie.Cli.Tests/Commands/StatusCommandTests.cs

### Implementation for User Story 3

- [X] T042 [US3] Implement profile resolution using ProfileMerger in StatusCommand in src/Dottie.Cli/Commands/StatusCommand.cs
- [X] T043 [US3] Add profile header display (Profile: work, inherited from: default) in StatusFormatter in src/Dottie.Cli/Output/StatusFormatter.cs
- [X] T044 [US3] Implement error handling for non-existent profiles in StatusCommand in src/Dottie.Cli/Commands/StatusCommand.cs

**Checkpoint**: User Stories 1, 2, AND 3 complete - `dottie status --profile <name>` works

---

## Phase 6: User Story 4 - Default Profile Resolution (Priority: P3)

**Goal**: Auto-use default profile when `--profile` not specified

**Independent Test**: Run `dottie status` without `--profile` and verify default profile is used

### Tests for User Story 4 ⚠️

- [X] T045 [P] [US4] Add test: Execute_WithoutProfileOption_UsesDefaultProfile in tests/Dottie.Cli.Tests/Commands/StatusCommandTests.cs
- [X] T046 [P] [US4] Add test: Execute_WithNoDefaultAndMultipleProfiles_ShowsError in tests/Dottie.Cli.Tests/Commands/StatusCommandTests.cs

### Implementation for User Story 4

- [X] T047 [US4] Implement default profile fallback logic in StatusCommand in src/Dottie.Cli/Commands/StatusCommand.cs
- [X] T048 [US4] Add error message when no default profile and multiple profiles exist in StatusCommand in src/Dottie.Cli/Commands/StatusCommand.cs

**Checkpoint**: All user stories complete - full `dottie status` functionality

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Summary output, integration tests, documentation

- [X] T049 [P] Implement WriteSummary method in StatusFormatter in src/Dottie.Cli/Output/StatusFormatter.cs
- [X] T050 Implement WriteStatusReport orchestration method in StatusFormatter in src/Dottie.Cli/Output/StatusFormatter.cs
- [X] T051 [P] Create integration test scenario directory in tests/integration/scenarios/status-command/
- [X] T052 [P] Create integration test dottie.yaml with mixed states in tests/integration/scenarios/status-command/dottie.yaml
- [X] T053 Create integration test validate.sh script in tests/integration/scenarios/status-command/validate.sh
- [X] T054 [P] Ensure exit code 0 returned on successful status check (informational command) in src/Dottie.Cli/Commands/StatusCommand.cs
- [X] T055 Run all unit tests and verify ≥90% line coverage
- [X] T056 Run integration tests via tests/run-integration-tests.ps1
- [X] T057 Run quickstart.md validation scenarios manually

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - Can proceed in priority order (P1 → P2 → P3)
  - Or in parallel if staffed
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Adds to US1 command but independent tests
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Uses profile infrastructure
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - Extends US3 profile handling

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD mandate)
- Models/records before services
- Services before CLI command integration
- Core implementation before formatters
- Story complete before moving to next priority

### Parallel Opportunities

- All Phase 1 Setup tasks marked [P] can run in parallel
- All Phase 2 Foundational tasks marked [P] can run in parallel
- All tests for a user story marked [P] can run in parallel
- StatusFormatter methods can be developed in parallel with checker implementations

---

## Parallel Example: User Story 1 Tests

```bash
# Launch all US1 tests together:
T009 [P] [US1] Create DotfileStatusCheckerTests test class
T010 [P] [US1] Add test: CheckStatus_WhenSymlinkPointsToCorrectSource_ReturnsLinkedState
T011 [P] [US1] Add test: CheckStatus_WhenTargetDoesNotExist_ReturnsMissingState
T012 [P] [US1] Add test: CheckStatus_WhenSymlinkTargetDoesNotExist_ReturnsBrokenState
T013 [P] [US1] Add test: CheckStatus_WhenFileExistsButIsNotSymlink_ReturnsConflictingState
T014 [P] [US1] Add test: CheckStatus_WhenAccessDenied_ReturnsUnknownState
```

---

## Parallel Example: User Story 2 Tests

```bash
# Launch all US2 tests together:
T022 [P] [US2] Create SoftwareStatusCheckerTests test class
T023 [P] [US2] Add test: CheckStatusAsync_WhenGitHubBinaryExists_ReturnsInstalledState
T024 [P] [US2] Add test: CheckStatusAsync_WhenGitHubBinaryMissing_ReturnsMissingState
T025 [P] [US2] Add test: CheckStatusAsync_WhenGitHubVersionMismatch_ReturnsOutdatedState
T026 [P] [US2] Add test: CheckStatusAsync_WhenAptPackageInstalled_ReturnsInstalledState
T027 [P] [US2] Add test: CheckStatusAsync_WhenAptPackageMissing_ReturnsMissingState
T028 [P] [US2] Add test: CheckStatusAsync_WhenDetectionFails_ReturnsUnknownState
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T008)
3. Complete Phase 3: User Story 1 (T009-T021)
4. **STOP and VALIDATE**: Test `dottie status` with dotfiles only
5. Demo/review if ready - shows dotfile status

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → MVP: `dottie status` shows dotfiles
3. Add User Story 2 → Adds software status to output
4. Add User Story 3 → Adds `--profile` option
5. Add User Story 4 → Adds default profile fallback
6. Each story adds value without breaking previous stories

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- **TDD MANDATE**: Tests must fail before implementation (constitution requirement)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Exit code is always 0 for successful status check (informational command per FR-014)
