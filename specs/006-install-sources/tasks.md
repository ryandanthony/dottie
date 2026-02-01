# Tasks: Installation Sources

**Input**: Design documents from `/specs/006-install-sources/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: This project follows TDD (Test-Driven Development) per constitution. Unit tests are written FIRST,
then implementation. Integration tests validate end-to-end workflows.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/Dottie.Configuration/`, `src/Dottie.Cli/`
- **Unit Tests**: `tests/Dottie.Configuration.Tests/`, `tests/Dottie.Cli.Tests/`
- **Integration Tests**: `tests/integration/scenarios/`

---

## Phase 1: Setup

**Purpose**: Create core infrastructure types and utilities shared by all installers

- [X] T001 Create Installing namespace directory at src/Dottie.Configuration/Installing/
- [X] T002 Create Utilities subdirectory at src/Dottie.Configuration/Installing/Utilities/
- [X] T003 [P] Create test directory at tests/Dottie.Configuration.Tests/Installing/
- [X] T004 [P] Create test utilities directory at tests/Dottie.Configuration.Tests/Installing/Utilities/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core types and utilities that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Unit Tests (Write FIRST - must FAIL before implementation)

- [X] T005 [P] Create InstallResultTests.cs at tests/Dottie.Configuration.Tests/Installing/InstallResultTests.cs
- [X] T006 [P] Create InstallContextTests.cs at tests/Dottie.Configuration.Tests/Installing/InstallContextTests.cs
- [X] T007 [P] Create HttpDownloaderTests.cs at tests/Dottie.Configuration.Tests/Installing/Utilities/HttpDownloaderTests.cs
- [X] T008 [P] Create ArchiveExtractorTests.cs at tests/Dottie.Configuration.Tests/Installing/Utilities/ArchiveExtractorTests.cs
- [X] T009 [P] Create SudoCheckerTests.cs at tests/Dottie.Configuration.Tests/Installing/Utilities/SudoCheckerTests.cs

### Implementation

- [X] T010 [P] Create InstallStatus enum and InstallSourceType enum in src/Dottie.Configuration/Installing/InstallResult.cs
- [X] T011 [P] Create InstallResult record in src/Dottie.Configuration/Installing/InstallResult.cs
- [X] T012 [P] Create InstallContext record in src/Dottie.Configuration/Installing/InstallContext.cs
- [X] T013 [P] Create IInstallSource interface in src/Dottie.Configuration/Installing/IInstallSource.cs
- [X] T014 Create HttpDownloader with retry logic in src/Dottie.Configuration/Installing/Utilities/HttpDownloader.cs
- [X] T015 Create ArchiveExtractor (.tar.gz, .zip, .tgz) in src/Dottie.Configuration/Installing/Utilities/ArchiveExtractor.cs
- [X] T016 Create SudoChecker utility in src/Dottie.Configuration/Installing/Utilities/SudoChecker.cs

**Checkpoint**: Foundation ready - all core types tested and working ✅ COMPLETE

---

## Phase 3: User Story 1 - GitHub Releases (Priority: P1) 🎯 MVP

**Goal**: Download and install binaries from GitHub releases to ~/bin/

**Independent Test**: Run `dottie install` with a GitHub release config entry; verify binary in ~/bin/

### Unit Tests (Write FIRST - must FAIL before implementation)

- [x] T017 [P] [US1] Create GithubReleaseInstallerTests.cs at tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs
- [x] T018 [P] [US1] Create InstallOrchestratorTests.cs at tests/Dottie.Configuration.Tests/Installing/InstallOrchestratorTests.cs
- [x] T019 [P] [US1] Create InstallCommandTests.cs at tests/Dottie.Cli.Tests/Commands/InstallCommandTests.cs
- [x] T020 [P] [US1] Create InstallCommandSettingsTests.cs at tests/Dottie.Cli.Tests/Commands/InstallCommandSettingsTests.cs
- [x] T021 [P] [US1] Create InstallProgressRendererTests.cs at tests/Dottie.Cli.Tests/Output/InstallProgressRendererTests.cs

### Implementation

- [x] T022 [US1] Implement GithubReleaseInstaller in src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs
- [x] T023 [US1] Implement InstallOrchestrator (GitHub only) in src/Dottie.Configuration/Installing/InstallOrchestrator.cs
- [x] T024 [US1] Create InstallCommandSettings in src/Dottie.Cli/Commands/InstallCommandSettings.cs
- [x] T025 [US1] Implement InstallCommand in src/Dottie.Cli/Commands/InstallCommand.cs
- [x] T026 [US1] Create InstallProgressRenderer in src/Dottie.Cli/Output/InstallProgressRenderer.cs
- [x] T027 [US1] Register InstallCommand in src/Dottie.Cli/Program.cs

### Integration Test

- [x] T028 [US1] Create install-github-release scenario at tests/integration/scenarios/install-github-release/
- [x] T029 [US1] Create dottie.yml config for GitHub release test
- [x] T030 [US1] Create validate.sh to verify binary installation

**Checkpoint**: User Story 1 complete - GitHub release installation works independently

---

## Phase 4: User Story 2 - APT Packages (Priority: P2)

**Goal**: Install standard Ubuntu packages via apt-get

**Independent Test**: Run `dottie install` with APT package list; verify packages installed

### Unit Tests (Write FIRST - must FAIL before implementation)

- [x] T031 [P] [US2] Create AptPackageInstallerTests.cs at tests/Dottie.Configuration.Tests/Installing/AptPackageInstallerTests.cs

### Implementation

- [x] T032 [US2] Implement AptPackageInstaller in src/Dottie.Configuration/Installing/AptPackageInstaller.cs
- [x] T033 [US2] Update InstallOrchestrator to include AptPackageInstaller in src/Dottie.Configuration/Installing/InstallOrchestrator.cs

### Integration Test

- [x] T034 [US2] Create install-apt-packages scenario at tests/integration/scenarios/install-apt-packages/
- [x] T035 [US2] Create dottie.yml config for APT package test
- [x] T036 [US2] Create validate.sh to verify package installation

**Checkpoint**: User Story 2 complete - APT package installation works independently

---

## Phase 5: User Story 3 - Private APT Repositories (Priority: P3)

**Goal**: Add third-party APT repos with GPG keys and install packages from them

**Independent Test**: Run `dottie install` with private repo config; verify repo added and packages installed

### Unit Tests (Write FIRST - must FAIL before implementation)

- [ ] T037 [P] [US3] Create AptRepoInstallerTests.cs at tests/Dottie.Configuration.Tests/Installing/AptRepoInstallerTests.cs

### Implementation

- [ ] T038 [US3] Implement AptRepoInstaller in src/Dottie.Configuration/Installing/AptRepoInstaller.cs
- [ ] T039 [US3] Update InstallOrchestrator to include AptRepoInstaller in src/Dottie.Configuration/Installing/InstallOrchestrator.cs

### Integration Test

- [ ] T040 [US3] Create install-apt-repo scenario at tests/integration/scenarios/install-apt-repo/
- [ ] T041 [US3] Create dottie.yml config for private APT repo test
- [ ] T042 [US3] Create validate.sh to verify repo setup and package installation

**Checkpoint**: User Story 3 complete - Private APT repo installation works independently

---

## Phase 6: User Story 4 - Shell Scripts (Priority: P4)

**Goal**: Execute custom installation scripts from the repository

**Independent Test**: Run `dottie install` with script config; verify script executed and artifacts created

### Unit Tests (Write FIRST - must FAIL before implementation)

- [ ] T043 [P] [US4] Create ScriptRunnerTests.cs at tests/Dottie.Configuration.Tests/Installing/ScriptRunnerTests.cs

### Implementation

- [ ] T044 [US4] Implement ScriptRunner in src/Dottie.Configuration/Installing/ScriptRunner.cs
- [ ] T045 [US4] Update InstallOrchestrator to include ScriptRunner in src/Dottie.Configuration/Installing/InstallOrchestrator.cs

### Integration Test

- [ ] T046 [US4] Create install-scripts scenario at tests/integration/scenarios/install-scripts/
- [ ] T047 [US4] Create dottie.yml config with script reference
- [ ] T048 [US4] Create test script at tests/integration/scenarios/install-scripts/scripts/test-script.sh
- [ ] T049 [US4] Create validate.sh to verify script execution

**Checkpoint**: User Story 4 complete - Script execution works independently

---

## Phase 7: User Story 5 - Fonts (Priority: P5)

**Goal**: Download and install fonts to ~/.local/share/fonts/ with cache refresh

**Independent Test**: Run `dottie install` with font config; verify fonts in directory and cache refreshed

### Unit Tests (Write FIRST - must FAIL before implementation)

- [ ] T050 [P] [US5] Create FontInstallerTests.cs at tests/Dottie.Configuration.Tests/Installing/FontInstallerTests.cs

### Implementation

- [ ] T051 [US5] Implement FontInstaller in src/Dottie.Configuration/Installing/FontInstaller.cs
- [ ] T052 [US5] Update InstallOrchestrator to include FontInstaller in src/Dottie.Configuration/Installing/InstallOrchestrator.cs

### Integration Test

- [ ] T053 [US5] Create install-fonts scenario at tests/integration/scenarios/install-fonts/
- [ ] T054 [US5] Create dottie.yml config for font installation test
- [ ] T055 [US5] Create validate.sh to verify font files and fc-cache

**Checkpoint**: User Story 5 complete - Font installation works independently

---

## Phase 8: User Story 6 - Snap Packages (Priority: P6)

**Goal**: Install snap packages with optional classic confinement

**Independent Test**: Run `dottie install` with snap config; verify snap installed

### Unit Tests (Write FIRST - must FAIL before implementation)

- [ ] T056 [P] [US6] Create SnapPackageInstallerTests.cs at tests/Dottie.Configuration.Tests/Installing/SnapPackageInstallerTests.cs

### Implementation

- [ ] T057 [US6] Implement SnapPackageInstaller in src/Dottie.Configuration/Installing/SnapPackageInstaller.cs
- [ ] T058 [US6] Update InstallOrchestrator to include SnapPackageInstaller in src/Dottie.Configuration/Installing/InstallOrchestrator.cs

### Integration Test

- [ ] T059 [US6] Create install-snap scenario at tests/integration/scenarios/install-snap/
- [ ] T060 [US6] Create dottie.yml config for snap installation test
- [ ] T061 [US6] Create validate.sh to verify snap package installed

**Checkpoint**: User Story 6 complete - Snap installation works independently

---

## Phase 9: Polish & Integration Testing

**Purpose**: Final integration, edge cases, and documentation

### Combined Integration Tests

- [ ] T062 Create install-combined scenario at tests/integration/scenarios/install-combined/
- [ ] T063 Create dottie.yml config with all 6 source types
- [ ] T064 Create setup.sh script at tests/integration/scenarios/install-combined/scripts/setup.sh
- [ ] T065 Create validate.sh to verify priority order and all sources

### No-Sudo Degradation Test

- [ ] T066 Create install-no-sudo scenario at tests/integration/scenarios/install-no-sudo/
- [ ] T067 Create dottie.yml config testing graceful degradation
- [ ] T068 Create validate.sh to verify warnings and non-sudo sources work

### Infrastructure Updates

- [ ] T069 Update Dockerfile at tests/integration/Dockerfile for install command support (add fc-cache, test dependencies)
- [ ] T070 Update run-scenarios.sh at tests/integration/scripts/run-scenarios.sh for install scenarios

**Checkpoint**: All integration tests working, cross-cutting concerns addressed

---

## Phase 10: Dry-Run Validation Mode

**Purpose**: Implement --dry-run flag with full validation (FR-024 through FR-027)

### Unit Tests (Write FIRST - must FAIL before implementation)

- [ ] T071 [P] Create DryRunValidatorTests.cs at tests/Dottie.Configuration.Tests/Installing/DryRunValidatorTests.cs
- [ ] T072 [P] Add --dry-run test cases to InstallCommandSettingsTests.cs
- [ ] T073 [P] Add --dry-run test cases to GithubReleaseInstallerTests.cs (validate GitHub releases exist)
- [ ] T074 [P] Add --dry-run test cases to ScriptRunnerTests.cs (validate scripts exist in repo)
- [ ] T075 [P] Add --dry-run test cases to AptRepoInstallerTests.cs (validate URLs reachable)
- [ ] T076 [P] Add --dry-run test cases to FontInstallerTests.cs (validate URLs reachable)

### Implementation

- [ ] T077 Create DryRunValidator service in src/Dottie.Configuration/Installing/DryRunValidator.cs
- [ ] T078 Update InstallCommand to support --dry-run flag in src/Dottie.Cli/Commands/InstallCommand.cs
- [ ] T079 Update InstallCommandSettings to include --dry-run boolean flag in src/Dottie.Cli/Commands/InstallCommandSettings.cs
- [ ] T080 Update GithubReleaseInstaller to support dry-run validation in src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs
- [ ] T081 Update ScriptRunner to support dry-run validation in src/Dottie.Configuration/Installing/ScriptRunner.cs
- [ ] T082 Update HttpDownloader to support URL reachability checking in src/Dottie.Configuration/Installing/Utilities/HttpDownloader.cs
- [ ] T083 Update InstallOrchestrator to skip execution in dry-run mode in src/Dottie.Configuration/Installing/InstallOrchestrator.cs

### Integration Test

- [ ] T084 Create install-dry-run scenario at tests/integration/scenarios/install-dry-run/
- [ ] T085 Create dottie.yml config with all source types for dry-run test
- [ ] T086 Create validate.sh to verify --dry-run previews without executing (check no files created)
- [ ] T087 Create validate.sh to verify --dry-run validates GitHub releases exist
- [ ] T088 Create validate.sh to verify --dry-run validates scripts exist
- [ ] T089 Create validate.sh to verify --dry-run validates URLs reachable

**Checkpoint**: Dry-run mode fully functional with comprehensive validation

---

## Phase 11: Final Verification

**Purpose**: Quality gates and release readiness

### Final Verification

- [ ] T090 Run quickstart.md validation steps
- [ ] T091 Verify all unit tests pass with dotnet test
- [ ] T092 Verify all integration tests pass with ./tests/run-integration-tests.ps1
- [ ] T093 Verify code coverage meets requirements (≥90% line, ≥80% branch)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories
- **User Stories (Phases 3-8)**: All depend on Foundational phase completion
  - User stories can proceed in parallel (if staffed) or sequentially in priority order
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

| Story | Depends On | Can Parallelize After |
|-------|------------|----------------------|
| US1 (GitHub) | Foundational | Phase 2 complete |
| US2 (APT) | Foundational | Phase 2 complete |
| US3 (APT Repo) | Foundational + US2 (shares APT knowledge) | Phase 4 complete |
| US4 (Scripts) | Foundational | Phase 2 complete |
| US5 (Fonts) | Foundational | Phase 2 complete |
| US6 (Snap) | Foundational | Phase 2 complete |

### Within Each User Story

1. Unit tests MUST be written and FAIL before implementation
2. Implementation follows test-first pattern
3. Integration test validates end-to-end
4. Story complete before marking checkpoint

### Parallel Opportunities

**Phase 1 (Setup)** - All tasks can run in parallel:
```
T001, T002, T003, T004 → all parallel
```

**Phase 2 (Foundational)** - Tests parallel, then implementations:
```
T005, T006, T007, T008, T009 → all parallel (tests)
T010, T011, T012, T013 → all parallel (types)
T014, T015, T016 → sequential (utilities with dependencies)
```

**User Stories** - Can run in parallel by different developers:
```
Developer A: US1 (T017-T030)
Developer B: US2 (T031-T036)
Developer C: US4 (T043-T049)
Developer D: US5 (T050-T055)
```

**Phase 10 (Dry-Run)** - Tests can run in parallel:
```
T071, T072, T073, T074, T075, T076 → all parallel (tests)
T077-T083 → sequential (implementation builds on tests)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (GitHub Releases)
4. **STOP and VALIDATE**: Test US1 independently with integration test
5. Deploy/demo if ready - basic install command works

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 (GitHub) → Test → Deploy (MVP!)
3. Add US2 (APT) → Test → Deploy
4. Add US3 (APT Repo) → Test → Deploy
5. Add US4 (Scripts) → Test → Deploy
6. Add US5 (Fonts) → Test → Deploy
7. Add US6 (Snap) → Test → Deploy
8. Polish + Integration → All sources working together
9. Add Dry-Run mode → Preview mode ready
10. Final verification → Release ready

### Priority-Based Implementation

If time-constrained, implement in priority order:
- **P1 (US1)**: GitHub Releases - highest value, covers most modern CLI tools
- **P2 (US2)**: APT Packages - fundamental to Ubuntu
- **P3 (US3)**: Private APT Repos - Docker, VS Code, etc.
- **P4 (US4)**: Scripts - flexibility for edge cases
- **P5 (US5)**: Fonts - nice-to-have for terminal customization
- **P6 (US6)**: Snap - lowest priority, overlaps with APT

---

## Notes

- Follow TDD strictly per constitution: test FIRST, verify FAIL, then implement
- Each integration test scenario runs in Docker for isolation
- Commit after each task or logical group
- Run `dotnet test` after each implementation task
- Integration tests require Docker: `./tests/run-integration-tests.ps1`
