# Tasks: Configuration Profiles

**Input**: Design documents from `/specs/003-profiles/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**⚠️ TDD COMPLIANCE**: All tasks follow Red→Green→Refactor. Within each user story:
1. Create test fixtures FIRST
2. Write failing tests (RED)
3. Implement minimum code to pass (GREEN)
4. Refactor while keeping tests green

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create test fixtures and stubs needed for TDD

- [ ] T001 Verify all existing tests pass with `dotnet test`
- [ ] T002 [P] Create profile-invalid-name.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/
- [ ] T003 [P] Create profile-dedup.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/
- [ ] T004 [P] Create profile-implicit-default.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: New types that support multiple user stories

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Create ProfileInfo record in src/Dottie.Configuration/ProfileInfo.cs
- [ ] T006 Create ProfileAwareSettings base class in src/Dottie.Cli/Commands/ProfileAwareSettings.cs

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 1 - Select a Named Profile (Priority: P1) 🎯 MVP

**Goal**: Enable `--profile` flag on commands with default profile fallback

**Independent Test**: Run `dottie validate --profile work` and verify profile is selected; run without flag and verify "default" is used

### 3.1 Write Failing Tests (RED)

- [ ] T007 [US1] Write ProfileResolver implicit default tests in tests/Dottie.Configuration.Tests/ProfileResolverTests.cs
  - Test: GetProfile_NullProfileName_ReturnsDefaultProfile
  - Test: GetProfile_EmptyProfileName_ReturnsDefaultProfile
  - Test: GetProfile_DefaultNotDefined_ReturnsImplicitEmptyDefault
  - **Run tests — verify they FAIL (RED)**

- [ ] T008 [US1] Write ValidateCommand profile flag tests in tests/Dottie.Cli.Tests/Commands/ValidateCommandTests.cs
  - Test: Execute_WithProfileFlag_UsesSpecifiedProfile
  - Test: Execute_WithoutProfileFlag_UsesDefaultProfile
  - Test: Execute_WithInvalidProfile_ShowsErrorWithAvailableProfiles
  - **Run tests — verify they FAIL (RED)**

### 3.2 Implementation (GREEN)

- [ ] T009 [US1] Update ProfileResolver.GetProfile() for implicit default in src/Dottie.Configuration/ProfileResolver.cs
  - **Run ProfileResolverTests — verify they PASS (GREEN)**

- [ ] T010 [US1] Update ValidateCommandSettings to inherit ProfileAwareSettings in src/Dottie.Cli/Commands/ValidateCommandSettings.cs
  - Change positional `[profile]` argument to `--profile` option via base class

- [ ] T011 [US1] Update ValidateCommand to use --profile flag in src/Dottie.Cli/Commands/ValidateCommand.cs
  - Use ProfileResolver with ProfileName from settings
  - **Run ValidateCommandTests — verify they PASS (GREEN)**

### 3.3 Refactor & Verify Coverage

- [ ] T012 [US1] Refactor US1 code for clarity; verify ≥90% line coverage, ≥80% branch coverage

**Checkpoint**: User Story 1 complete — `--profile` flag works, default fallback works

---

## Phase 4: User Story 2 - Profile Inheritance (Priority: P2)

**Goal**: Ensure inheritance chain works correctly with proper merge order (already implemented, needs validation)

**Independent Test**: Create config with A extends B extends C; verify merged result follows C→B→A order

### 4.1 Write Failing Tests (RED)

- [ ] T013 [P] [US2] Write ProfileMerger chain validation tests in tests/Dottie.Configuration.Tests/Inheritance/ProfileMergerTests.cs
  - Test: Resolve_ThreeLevelChain_MergesInCorrectOrder
  - Test: Resolve_ChildOverridesParentSetting_ChildWins
  - **Run tests — verify they FAIL (RED)**

### 4.2 Implementation (GREEN)

- [ ] T014 [US2] Validate existing ProfileMerger.Resolve() handles multi-level chains in src/Dottie.Configuration/Inheritance/ProfileMerger.cs
  - Existing implementation should pass — if not, fix merge order
  - **Run ProfileMergerTests — verify they PASS (GREEN)**

**Checkpoint**: User Story 2 complete — inheritance chains resolve correctly

---

## Phase 5: User Story 3 - List Management in Profiles (Priority: P3)

**Goal**: Dotfiles deduplicate by target path with child taking precedence

**Independent Test**: Create parent with dotfile A→~/.bashrc, child with B→~/.bashrc; verify only child's mapping remains

### 5.1 Write Failing Tests (RED)

- [ ] T015 [US3] Write ProfileMerger deduplication tests in tests/Dottie.Configuration.Tests/Inheritance/ProfileMergerTests.cs
  - Test: MergeDotfiles_SameTarget_ChildOverridesParent (uses profile-dedup.yaml)
  - Test: MergeDotfiles_DifferentTargets_BothIncluded
  - Test: MergeDotfiles_ThreeLevelChain_DeepestChildWins
  - **Run tests — verify they FAIL (RED)**

### 5.2 Implementation (GREEN)

- [ ] T016 [US3] Update ProfileMerger.MergeDotfiles() for target deduplication in src/Dottie.Configuration/Inheritance/ProfileMerger.cs
  - Change from simple append to dictionary-based merge by Target
  - **Run ProfileMergerTests — verify they PASS (GREEN)**

### 5.3 Refactor & Verify Coverage

- [ ] T017 [US3] Refactor US3 code for clarity; verify ≥90% line coverage, ≥80% branch coverage

**Checkpoint**: User Story 3 complete — dotfile deduplication works

---

## Phase 6: User Story 4 - View Available Profiles (Priority: P4)

**Goal**: List profiles with inheritance relationships using tree visualization

**Independent Test**: Run `dottie validate` with no profile; verify output shows all profiles with their extends relationships

### 6.1 Write Failing Tests (RED)

- [ ] T018 [P] [US4] Write ProfileResolver listing tests in tests/Dottie.Configuration.Tests/ProfileResolverTests.cs
  - Test: ListProfilesWithInfo_ReturnsProfileInfoWithExtends
  - Test: ListProfilesWithInfo_SortsAlphabetically
  - **Run tests — verify they FAIL (RED)**

### 6.2 Implementation (GREEN)

- [ ] T019 [US4] Add ListProfilesWithInfo() method to ProfileResolver in src/Dottie.Configuration/ProfileResolver.cs
  - Returns IReadOnlyList<ProfileInfo> with Name, Extends, DotfileCount, HasInstallBlock
  - **Run ProfileResolverTests — verify they PASS (GREEN)**

- [ ] T020 [US4] Update ValidateCommand to show inheritance tree in src/Dottie.Cli/Commands/ValidateCommand.cs
  - Use Spectre.Console.Tree for visualization when listing profiles
  - Show extends relationship for each profile

**Checkpoint**: User Story 4 complete — profile listing shows inheritance

---

## Phase 7: Profile Name Validation (Cross-Cutting)

**Goal**: Validate profile names contain only alphanumeric, hyphens, underscores (FR-011)

### 7.1 Write Failing Tests (RED)

- [ ] T021 [P] Write ProfileNameValidator tests in tests/Dottie.Configuration.Tests/Validation/ProfileNameValidatorTests.cs
  - Test: Validate_ValidName_ReturnsSuccess (alphanumeric, hyphens, underscores)
  - Test: Validate_NameWithSpaces_ReturnsError
  - Test: Validate_NameWithSpecialChars_ReturnsError (uses profile-invalid-name.yaml)
  - **Run tests — verify they FAIL (RED)**

### 7.2 Implementation (GREEN)

- [ ] T022 Add profile name validation to ConfigurationValidator in src/Dottie.Configuration/Validation/ConfigurationValidator.cs
  - Add ValidateProfileNames() method with regex pattern `^[a-zA-Z0-9_-]+$`
  - Call from Validate() method
  - **Run ProfileNameValidatorTests — verify they PASS (GREEN)**

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup and verification

- [ ] T023 [P] Run all tests and verify ≥90% line coverage, ≥80% branch coverage
- [ ] T024 [P] Update CLI help text for --profile flag
- [ ] T025 Run quickstart.md validation steps
- [ ] T026 Manual smoke test: validate --profile flag on real config

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — can start immediately
- **Phase 2 (Foundational)**: Depends on Setup — BLOCKS all user stories
- **Phase 3-6 (User Stories)**: All depend on Foundational phase completion
- **Phase 7 (Validation)**: Can run in parallel with user stories
- **Phase 8 (Polish)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies on other stories — standalone MVP
- **User Story 2 (P2)**: No dependencies on US1 — validates existing implementation
- **User Story 3 (P3)**: No dependencies on US1/US2 — modifies ProfileMerger only
- **User Story 4 (P4)**: No dependencies — adds listing capability

### Within Each User Story

1. Tests MUST be written and FAIL before implementation
2. Implementation makes tests PASS
3. Refactor while keeping tests green
4. Verify coverage before moving on

---

## Parallel Execution Examples

### Setup Phase (All Parallel)

```text
T002: Create profile-invalid-name.yaml fixture
T003: Create profile-dedup.yaml fixture
T004: Create profile-implicit-default.yaml fixture
```

### User Stories (Can Run in Parallel After Phase 2)

```text
Developer A: User Story 1 (T007-T012) — MVP
Developer B: User Story 3 (T015-T017) — Deduplication
Developer C: Phase 7 (T021-T022) — Validation
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1 (--profile flag + default)
4. **STOP and VALIDATE**: Test with `dottie validate --profile <name>`
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. User Story 1 → MVP with profile selection
3. User Story 3 → Deduplication (key for correctness)
4. User Story 2 → Validate inheritance (already implemented)
5. User Story 4 → Enhanced listing (nice to have)
6. Phase 7 → Validation polish
7. Phase 8 → Final cleanup

---

## Summary

| Phase | Tasks | Parallel Opportunities |
|-------|-------|----------------------|
| Setup | T001-T004 | T002, T003, T004 |
| Foundational | T005-T006 | None |
| US1 (P1) | T007-T012 | None (sequential TDD) |
| US2 (P2) | T013-T014 | T013 |
| US3 (P3) | T015-T017 | None (sequential TDD) |
| US4 (P4) | T018-T020 | T018 |
| Validation | T021-T022 | T021 |
| Polish | T023-T026 | T023, T024 |

**Total Tasks**: 26
**MVP Tasks** (US1 only): 12 (T001-T012)
