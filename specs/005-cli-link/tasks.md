# Tasks: CLI Link Command

**Input**: Design documents from `/specs/005-cli-link/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: This feature requires TDD per the constitution. All new code must have tests written first.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Verify existing implementation and prepare for enhancements

- [X] T001 Verify existing tests pass with `dotnet test`
- [X] T002 [P] Create test file tests/Dottie.Cli.Tests/Commands/LinkCommandSettingsTests.cs with basic structure

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure changes that multiple user stories depend on

**⚠️ CRITICAL**: These changes affect multiple user stories and must be complete first

### Tests for Foundational

- [X] T003 [P] Write test for backup naming convention (.dottie-backup-YYYYMMDD-HHMMSS) in tests/Dottie.Configuration.Tests/Linking/BackupServiceTests.cs
- [X] T004 [P] Write test for Windows symlink error message in tests/Dottie.Configuration.Tests/Linking/SymlinkServiceTests.cs

### Implementation for Foundational

- [X] T005 Update BackupService.GenerateBackupPath() to use `.dottie-backup-YYYYMMDD-HHMMSS` format in src/Dottie.Configuration/Linking/BackupService.cs
- [X] T006 Add LastError property and Windows-specific error message to SymlinkService in src/Dottie.Configuration/Linking/SymlinkService.cs
- [X] T007 Update LinkResult to store detailed error messages from SymlinkService in src/Dottie.Configuration/Linking/LinkResult.cs

**Checkpoint**: Foundation ready - backup naming and error handling updated

---

## Phase 3: User Story 1 - Basic Dotfile Linking (Priority: P1) 🎯 MVP

**Goal**: Ensure basic linking works with progress bar and summary output

**Independent Test**: Run `dottie link` with valid config, verify symlinks created with progress bar and summary

### Tests for User Story 1

- [X] T008 [P] [US1] Write test for progress bar display during linking in tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs
- [X] T009 [P] [US1] Write test for summary output (linked/skipped/failed counts) in tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs

### Implementation for User Story 1

- [X] T010 [US1] Add progress bar wrapper method using Spectre.Console.Progress() in src/Dottie.Cli/Commands/LinkCommand.cs
- [X] T011 [US1] Refactor ExecuteLinking to use progress bar and return detailed counts in src/Dottie.Cli/Commands/LinkCommand.cs
- [X] T012 [US1] Add WriteSummary method to ConflictFormatter for final output in src/Dottie.Cli/Output/ConflictFormatter.cs
- [X] T013 [US1] Update Program.cs examples to show progress bar behavior in src/Dottie.Cli/Program.cs

**Checkpoint**: User Story 1 complete - basic linking with progress bar and summary

---

## Phase 4: User Story 2 - Profile-Specific Linking (Priority: P2)

**Goal**: Ensure profile selection works correctly (already implemented, verify behavior)

**Independent Test**: Run `dottie link --profile work` and verify only work profile dotfiles are linked

### Tests for User Story 2

- [X] T014 [P] [US2] Write test for default profile usage when no --profile specified in tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs
- [X] T015 [P] [US2] Write test for error message when profile not found in tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs

### Implementation for User Story 2

- [X] T016 [US2] Verify existing profile handling in LinkCommand matches spec in src/Dottie.Cli/Commands/LinkCommand.cs
- [X] T017 [US2] Ensure profile error messages are clear and actionable in src/Dottie.Cli/Commands/LinkCommand.cs

**Checkpoint**: User Story 2 complete - profile-specific linking verified

---

## Phase 5: User Story 3 - Preview Changes with Dry Run (Priority: P2)

**Goal**: Add --dry-run flag to preview linking operations without making changes

**Independent Test**: Run `dottie link --dry-run` and verify no symlinks created, preview output shown

### Tests for User Story 3

- [X] T018 [P] [US3] Write test for DryRun flag parsing in tests/Dottie.Cli.Tests/Commands/LinkCommandSettingsTests.cs
- [X] T019 [P] [US3] Write test that dry-run makes no filesystem changes in tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs
- [X] T020 [P] [US3] Write test for dry-run preview output format in tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs
- [X] T021 [P] [US3] Write test for mutually exclusive --dry-run and --force flags in tests/Dottie.Cli.Tests/Commands/LinkCommandSettingsTests.cs

### Implementation for User Story 3

- [X] T022 [US3] Add DryRun property with CommandOption attribute to LinkCommandSettings in src/Dottie.Cli/Commands/LinkCommandSettings.cs
- [X] T023 [US3] Implement Validate() method for mutually exclusive flags in src/Dottie.Cli/Commands/LinkCommandSettings.cs
- [X] T024 [US3] Add WriteDryRunPreview method to ConflictFormatter in src/Dottie.Cli/Output/ConflictFormatter.cs
- [X] T025 [US3] Add dry-run branch in LinkCommand.Execute() that shows preview without executing in src/Dottie.Cli/Commands/LinkCommand.cs
- [X] T026 [US3] Update Program.cs examples to include --dry-run in src/Dottie.Cli/Program.cs

**Checkpoint**: User Story 3 complete - dry-run preview mode working

---

## Phase 6: User Story 4 - Force Overwrite with Backup (Priority: P3)

**Goal**: Ensure force mode creates proper backups with new naming convention

**Independent Test**: Run `dottie link --force` with existing files, verify backups created with `.dottie-backup-*` naming

### Tests for User Story 4

- [X] T027 [P] [US4] Write test for backup creation with new naming format in tests/Dottie.Configuration.Tests/Linking/BackupServiceTests.cs
- [X] T028 [P] [US4] Write test for backup output display in tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs

### Implementation for User Story 4

- [X] T029 [US4] Update backup result display in ConflictFormatter to show new naming in src/Dottie.Cli/Output/ConflictFormatter.cs
- [X] T030 [US4] Verify force mode flow with updated backup naming in src/Dottie.Cli/Commands/LinkCommand.cs

**Checkpoint**: User Story 4 complete - force mode with proper backup naming

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements and validation

- [X] T031 [P] Verify exit code is non-zero when any mapping fails in tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs
- [X] T032 [P] Add integration test scenario for link command in tests/integration/
- [X] T033 Run quickstart.md validation steps manually
- [X] T034 Update README.md with link command documentation in README.md
- [X] T035 Code cleanup and verify all analyzer warnings resolved

---

## Fixes

- Fixed backup file pattern in conflict-handling integration test validation script to match new `.dottie-backup-` naming convention

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User stories can proceed in priority order (P1 → P2 → P3)
  - US2 and US3 both have P2 priority and can run in parallel
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - MVP, core functionality
- **User Story 2 (P2)**: Can start after Foundational - Verification of existing behavior
- **User Story 3 (P2)**: Can start after Foundational - New feature (dry-run)
- **User Story 4 (P3)**: Depends on Foundational backup naming change - Force mode verification

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD)
- Implementation follows test definitions
- Story complete before moving to next priority

### Parallel Opportunities

- T002 can run in parallel with T001
- All Foundational tests (T003, T004) can run in parallel
- Within each user story, tests marked [P] can run in parallel
- US2 and US3 (both P2) can run in parallel after US1 complete
- All Polish tasks marked [P] can run in parallel

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (Progress bar + Summary)
4. **STOP and VALIDATE**: Test `dottie link` with progress output
5. Deploy/demo if ready

### Full Feature Delivery

1. Setup + Foundational → Foundation ready
2. User Story 1 → Progress bar and summary ✓
3. User Story 2 → Profile selection verified ✓
4. User Story 3 → Dry-run preview mode ✓
5. User Story 4 → Force with proper backups ✓
6. Polish → Documentation and cleanup ✓

---

## Summary

| Metric | Count |
|--------|-------|
| Total Tasks | 35 |
| Setup Tasks | 2 |
| Foundational Tasks | 5 |
| User Story 1 Tasks | 6 |
| User Story 2 Tasks | 4 |
| User Story 3 Tasks | 9 |
| User Story 4 Tasks | 4 |
| Polish Tasks | 5 |
| Parallelizable Tasks | 18 |

**MVP Scope**: Phases 1-3 (13 tasks) - Basic linking with progress bar and summary

**Independent Test Criteria**:
- US1: `dottie link` shows progress bar and summary
- US2: `dottie link --profile X` links only profile X's dotfiles
- US3: `dottie link --dry-run` shows preview without changes
- US4: `dottie link --force` creates `.dottie-backup-*` files

**Format Validation**: All tasks follow checkbox format with ID, [P] marker where applicable, [Story] label for user story phases, and file paths.
