# Tasks: CI/CD Build Pipeline

**Input**: Design documents from `/specs/002-ci-build/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Feature**: CI/CD Build Pipeline with GitHub Actions, GitVersion semantic versioning, code coverage enforcement, cross-platform publishing, and automated releases.

**Tests**: No test tasks included - this is infrastructure configuration, not application code.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic CI/CD structure

- [x] T001 Create .NET SDK version pinning in global.json
- [x] T002 [P] Create GitVersion configuration in GitVersion.yml
- [x] T003 [P] Create scripts directory structure

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Create .github/workflows directory structure
- [x] T005 Create base GitHub Actions workflow file in .github/workflows/build.yml with triggers and permissions
- [x] T006 [P] Create integration test Dockerfile in tests/integration/Dockerfile (Ubuntu base, injects dottie binary + test scripts)
- [x] T006a [P] Create integration test scenario structure in tests/integration/scenarios/
- [x] T006b [P] Create integration test validation script in tests/integration/scripts/run-scenarios.sh
- [x] T007 [P] Create branch protection script in scripts/Set-BranchProtection.ps1

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Automated Build Verification on Pull Request (Priority: P1) 🎯 MVP

**Goal**: PR builds automatically trigger, run tests, collect coverage, and provide status feedback

**Independent Test**: Open a pull request and verify build runs, tests execute, coverage is collected, and status checks appear

### Implementation for User Story 1

- [x] T008 [P] [US1] Add checkout step with full history to .github/workflows/build.yml
- [x] T009 [P] [US1] Add .NET setup step to .github/workflows/build.yml
- [x] T010 [P] [US1] Add NuGet package caching to .github/workflows/build.yml
- [x] T011 [US1] Add dependency restore step to .github/workflows/build.yml
- [x] T012 [US1] Add build step with version to .github/workflows/build.yml
- [x] T013 [US1] Add unit test execution step to .github/workflows/build.yml
- [x] T014 [US1] Add integration test execution step to .github/workflows/build.yml (build image, run scenarios, validate outcomes)
- [x] T015 [US1] Add coverage collection and report generation to .github/workflows/build.yml
- [x] T016 [US1] Add coverage threshold enforcement (80%) to .github/workflows/build.yml
- [x] T017 [US1] Add coverage report upload as artifact to .github/workflows/build.yml
- [x] T018 [US1] Add coverage comment posting for PRs to .github/workflows/build.yml

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Automated Release on Main Branch (Priority: P2)

**Goal**: Successful main branch builds automatically create versioned GitHub Releases with binaries

**Independent Test**: Merge a PR to main and verify GitHub Release is created with correct version and downloadable binaries

### Implementation for User Story 2

- [x] T019 [P] [US2] Add linux-x64 publish step to .github/workflows/build.yml (done in T014 integration test build)
- [x] T020 [P] [US2] Add win-x64 publish step to .github/workflows/build.yml
- [x] T021 [US2] Add artifact upload step with 7-day retention to .github/workflows/build.yml
- [x] T022 [US2] Add conditional tag creation for main branch to .github/workflows/build.yml
- [x] T023 [US2] Add GitHub Release creation with binaries to .github/workflows/build.yml
- [x] T024 [US2] Configure release notes generation in .github/workflows/build.yml (use generate_release_notes: true in softprops/action-gh-release)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Semantic Version Calculation (Priority: P3)

**Goal**: Version numbers automatically calculated from git history using commit message conventions

**Independent Test**: Make commits with version keywords and verify calculated version changes appropriately

### Implementation for User Story 3

- [x] T025 [US3] Add GitVersion setup step to .github/workflows/build.yml (done in Phase 3)
- [x] T026 [US3] Configure GitVersion mainline mode in GitVersion.yml (done in Phase 2)
- [x] T027 [US3] Configure version bump patterns in GitVersion.yml (done in Phase 2)
- [x] T028 [US3] Configure starting version 0.1.0 in GitVersion.yml (done in Phase 2)
- [x] T029 [US3] Update build step to use GitVersion output in .github/workflows/build.yml (done in Phase 3)
- [x] T030 [US3] Update tag creation to use GitVersion format in .github/workflows/build.yml (done in Phase 4 - uses v${{ steps.gitversion.outputs.semVer }})

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: User Story 4 - Code Coverage Enforcement (Priority: P4)

**Goal**: Code coverage measured, reported, and enforced with 80% minimum threshold

**Independent Test**: Run a build and verify coverage is collected, reported, and enforced against threshold

### Implementation for User Story 4

- [x] T031 [US4] Configure coverage collection in unit test step in .github/workflows/build.yml (done in Phase 3)
- [x] T032 [US4] Configure coverage collection in integration test step in .github/workflows/build.yml (N/A - Docker integration tests don't use coverage)
- [x] T033 [US4] Add ReportGenerator step for HTML coverage report in .github/workflows/build.yml (done in Phase 3)
- [x] T034 [US4] Add coverage threshold validation step in .github/workflows/build.yml (done in Phase 3)
- [x] T035 [US4] Configure PR comment deletion and posting in .github/workflows/build.yml (done in Phase 3 with recreate: true)

**Checkpoint**: All user stories should now be independently functional

---

## Phase 7: Branch Protection Setup

**Purpose**: Configure repository branch protection rules

- [x] T036 Implement branch protection API calls in scripts/Set-BranchProtection.ps1 (done in Phase 2)
- [x] T037 [P] Add repository detection logic to scripts/Set-BranchProtection.ps1 (done in Phase 2)
- [x] T038 [P] Add WhatIf (dry-run) support to scripts/Set-BranchProtection.ps1 (done in Phase 2)
- [x] T039 [P] Add error handling and user feedback to scripts/Set-BranchProtection.ps1 (done in Phase 2)
- [x] T040 Test branch protection script execution (verified with -WhatIf)

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T041 [P] Update quickstart.md with final workflow details
- [x] T042 [P] Validate all workflow steps work end-to-end
- [x] T043 [P] Test failure scenarios (build failure, test failure, coverage failure)
- [x] T044 [P] Test version bumping with different commit message patterns
- [x] T045 [P] Verify artifact retention and download functionality
- [x] T046 Run complete quickstart.md validation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3 → P4)
- **Branch Protection (Phase 7)**: Can run in parallel with user stories
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Builds on US1 workflow structure
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Integrates with US1 and US2
- **User Story 4 (P4)**: Can start after Foundational (Phase 2) - Enhances US1 testing

### Within Each User Story

- Configuration files before workflow steps
- Basic workflow structure before advanced features
- Core functionality before conditional logic
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- Tasks within a story marked [P] can run in parallel
- Branch Protection (Phase 7) can run in parallel with user stories
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

If working on User Story 1 with multiple people:

**Person A**: T008, T009, T010 (setup steps)
**Person B**: T011, T012 (restore and build)
**Person C**: T013, T014 (testing steps)
**Person D**: T015, T016, T017, T018 (coverage and reporting)

Then integrate all changes into the single workflow file.

---

## Implementation Strategy

### MVP Approach

**Minimum Viable Product**: Complete User Story 1 (P1) only
- Provides immediate value: automated PR builds and testing
- Establishes foundation for remaining stories
- Can be deployed and used immediately

### Incremental Delivery

1. **Phase 1-2 + US1**: Basic CI for PRs (immediate value)
2. **+ US2**: Add automated releases (complete CI/CD)
3. **+ US3**: Add semantic versioning (professional versioning)
4. **+ US4**: Add coverage enforcement (quality gates)
5. **+ Phase 7**: Add branch protection (complete governance)

### Validation Checkpoints

After each user story:
1. Create test PR to verify functionality
2. Merge to main to test release process (US2+)
3. Test version bumping with commit messages (US3+)
4. Verify coverage reporting and enforcement (US4+)

---

## Total Task Count: 48

- **Setup**: 3 tasks
- **Foundational**: 6 tasks  
- **User Story 1**: 11 tasks
- **User Story 2**: 6 tasks
- **User Story 3**: 6 tasks
- **User Story 4**: 5 tasks
- **Branch Protection**: 5 tasks
- **Polish**: 6 tasks

**Parallel Opportunities**: 17 tasks marked [P] can run in parallel within their phases

**Independent Test Criteria**: Each user story has clear acceptance criteria and can be tested independently

**Suggested MVP Scope**: Phases 1-2 + User Story 1 (18 tasks) provides immediate CI value
