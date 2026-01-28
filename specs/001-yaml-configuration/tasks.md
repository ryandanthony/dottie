# Tasks: YAML Configuration System

**Input**: Design documents from `/specs/001-yaml-configuration/`  
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**⚠️ TDD COMPLIANCE**: All tasks follow Red→Green→Refactor. Within each user story:
1. Create test fixtures FIRST
2. Write failing tests (RED)
3. Implement minimum code to pass (GREEN)
4. Refactor while keeping tests green

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, solution structure, and dependencies

- [ ] T001 Create solution and project structure per plan.md layout
- [ ] T002 [P] Configure Dottie.Cli.csproj with AOT/trimming settings and Spectre.Console packages
- [ ] T003 [P] Configure Dottie.Configuration.csproj with YamlDotNet and trimming settings
- [ ] T004 [P] Configure test projects with xUnit and FluentAssertions
- [ ] T005 [P] Add .editorconfig and Directory.Build.props for consistent formatting
- [ ] T006 Create test fixtures directory structure at tests/Dottie.Configuration.Tests/Fixtures/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### 2.1 Test Fixtures (Create First)

- [ ] T007 [P] Create valid-minimal.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/
- [ ] T008 [P] Create valid-full.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/

### 2.2 Model Types (Stubs for Test Compilation)

- [ ] T009 [P] Create DottieConfiguration record in src/Dottie.Configuration/Models/DottieConfiguration.cs
- [ ] T010 [P] Create Profile record in src/Dottie.Configuration/Models/Profile.cs
- [ ] T011 [P] Create DotfileEntry record in src/Dottie.Configuration/Models/DotfileEntry.cs
- [ ] T012 [P] Create InstallBlock record in src/Dottie.Configuration/Models/InstallBlocks/InstallBlock.cs
- [ ] T013 [P] Create GithubReleaseItem record in src/Dottie.Configuration/Models/InstallBlocks/GithubReleaseItem.cs
- [ ] T014 [P] Create AptRepoItem record in src/Dottie.Configuration/Models/InstallBlocks/AptRepoItem.cs
- [ ] T015 [P] Create SnapItem record in src/Dottie.Configuration/Models/InstallBlocks/SnapItem.cs
- [ ] T016 [P] Create FontItem record in src/Dottie.Configuration/Models/InstallBlocks/FontItem.cs
- [ ] T017 Create ConfigurationYamlContext (source generator) in src/Dottie.Configuration/Parsing/ConfigurationYamlContext.cs
- [ ] T018 Create ValidationError and ValidationResult records in src/Dottie.Configuration/Validation/ValidationResult.cs

**Checkpoint**: Foundation ready — all model types exist, YAML context configured, test fixtures in place

---

## Phase 3: User Story 1 — Define Basic Dotfiles Configuration (Priority: P1) 🎯 MVP

**Goal**: Parse dottie.yaml with profile and dotfile mappings; validate required fields; report errors with line numbers

**Independent Test**: Create minimal dottie.yaml with one dotfile mapping and verify it parses correctly

### 3.1 Test Fixtures (RED - Create First)

- [ ] T019 [P] [US1] Create invalid-missing-source.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/
- [ ] T020 [P] [US1] Create invalid-missing-target.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/
- [ ] T021 [P] [US1] Create invalid-yaml-syntax.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/

### 3.2 Write Failing Tests (RED - Before Implementation)

- [ ] T022 [US1] Write ConfigurationLoaderTests in tests/Dottie.Configuration.Tests/Parsing/ConfigurationLoaderTests.cs
  - Test: Load_ValidMinimalConfig_ReturnsConfiguration (uses valid-minimal.yaml)
  - Test: Load_InvalidYamlSyntax_ReturnsErrorWithLineNumber (uses invalid-yaml-syntax.yaml)
  - Test: Load_MissingProfilesKey_ReturnsValidationError
  - **Run tests — verify they FAIL (RED)**
- [ ] T023 [US1] Write DotfileEntryValidatorTests in tests/Dottie.Configuration.Tests/Validation/DotfileEntryValidatorTests.cs
  - Test: Validate_MissingSource_ReturnsError (uses invalid-missing-source.yaml)
  - Test: Validate_MissingTarget_ReturnsError (uses invalid-missing-target.yaml)
  - Test: Validate_ValidEntry_ReturnsSuccess
  - **Run tests — verify they FAIL (RED)**
- [ ] T024 [US1] Write PathExpanderTests in tests/Dottie.Configuration.Tests/Utilities/PathExpanderTests.cs
  - Test: Expand_TildePath_ReturnsHomeDirectory
  - Test: Expand_AbsolutePath_ReturnsUnchanged
  - **Run tests — verify they FAIL (RED)**

### 3.3 Implementation (GREEN - Minimum to Pass)

- [ ] T025 [US1] Implement YamlDeserializer with error handling in src/Dottie.Configuration/Parsing/YamlDeserializer.cs
- [ ] T026 [US1] Implement ConfigurationLoader.Load() in src/Dottie.Configuration/Parsing/ConfigurationLoader.cs
  - **Run ConfigurationLoaderTests — verify they PASS (GREEN)**
- [ ] T027 [US1] Implement DotfileEntryValidator in src/Dottie.Configuration/Validation/DotfileEntryValidator.cs (FR-009, FR-010)
  - **Run DotfileEntryValidatorTests — verify they PASS (GREEN)**
- [ ] T028 [US1] Implement ProfileValidator in src/Dottie.Configuration/Validation/ProfileValidator.cs (FR-003, FR-005)
- [ ] T029 [US1] Implement ConfigurationValidator orchestration in src/Dottie.Configuration/Validation/ConfigurationValidator.cs (FR-021)
- [ ] T030 [US1] Implement tilde expansion utility in src/Dottie.Configuration/Utilities/PathExpander.cs (FR-011)
  - **Run PathExpanderTests — verify they PASS (GREEN)**

### 3.4 Refactor & Verify Coverage

- [ ] T031 [US1] Refactor US1 code for clarity; verify ≥90% line coverage, ≥80% branch coverage

**Checkpoint**: User Story 1 complete — can parse valid configs, reject invalid ones with line numbers

---

## Phase 4: User Story 2 — Configure Package Installation Sources (Priority: P2)

**Goal**: Parse all install block types (apt, github, snap, apt-repo, scripts, fonts) with validation

**Independent Test**: Create config with one apt package and verify it parses correctly

### 4.1 Test Fixtures (RED - Create First)

- [ ] T032 [P] [US2] Create invalid-github-missing-repo.yaml test fixture
- [ ] T033 [P] [US2] Create invalid-script-traversal.yaml test fixture
- [ ] T034 [P] [US2] Create valid-all-install-types.yaml test fixture

### 4.2 Write Failing Tests (RED - Before Implementation)

- [ ] T035 [US2] Write InstallBlockValidatorTests in tests/Dottie.Configuration.Tests/Validation/InstallBlockValidatorTests.cs
  - Test: Validate_GithubMissingRepo_ReturnsError
  - Test: Validate_GithubMissingAsset_ReturnsError
  - Test: Validate_GithubMissingBinary_ReturnsError
  - Test: Validate_AptRepoMissingKeyUrl_ReturnsError
  - Test: Validate_SnapMissingName_ReturnsError
  - Test: Validate_FontMissingUrl_ReturnsError
  - Test: Validate_AllTypesValid_ReturnsSuccess (uses valid-all-install-types.yaml)
  - **Run tests — verify they FAIL (RED)**
- [ ] T036 [US2] Write ScriptPathValidatorTests in tests/Dottie.Configuration.Tests/Validation/ScriptPathValidatorTests.cs
  - Test: Validate_PathWithinRepo_ReturnsSuccess
  - Test: Validate_AbsolutePath_ReturnsError
  - Test: Validate_ParentTraversal_ReturnsError (uses invalid-script-traversal.yaml)
  - **Run tests — verify they FAIL (RED)**
- [ ] T037 [US2] Write ArchitectureDetectorTests in tests/Dottie.Configuration.Tests/Utilities/ArchitectureDetectorTests.cs
  - Test: GetArchitecture_ReturnsCurrentSystemArchitecture
  - Test: MatchesArchitecture_Amd64Pattern_ReturnsTrue
  - **Run tests — verify they FAIL (RED)**

### 4.3 Implementation (GREEN - Minimum to Pass)

- [ ] T038 [US2] Implement InstallBlockValidator in src/Dottie.Configuration/Validation/InstallBlockValidator.cs (FR-012)
- [ ] T039 [US2] Implement GithubReleaseValidator in src/Dottie.Configuration/Validation/GithubReleaseValidator.cs (FR-013)
- [ ] T040 [US2] Implement AptRepoValidator in src/Dottie.Configuration/Validation/AptRepoValidator.cs (FR-015)
- [ ] T041 [US2] Implement SnapValidator in src/Dottie.Configuration/Validation/SnapValidator.cs (FR-018)
- [ ] T042 [US2] Implement FontValidator in src/Dottie.Configuration/Validation/FontValidator.cs (FR-017)
  - **Run InstallBlockValidatorTests — verify they PASS (GREEN)**
- [ ] T043 [US2] Implement ScriptPathValidator in src/Dottie.Configuration/Validation/ScriptPathValidator.cs (FR-016, FR-019)
  - **Run ScriptPathValidatorTests — verify they PASS (GREEN)**
- [ ] T044 [US2] Implement ArchitectureDetector in src/Dottie.Configuration/Utilities/ArchitectureDetector.cs (FR-013a)
  - **Run ArchitectureDetectorTests — verify they PASS (GREEN)**

### 4.4 Refactor & Verify Coverage

- [ ] T045 [US2] Refactor US2 code for clarity; verify ≥90% line coverage, ≥80% branch coverage

**Checkpoint**: User Story 2 complete — all install block types parse and validate correctly

---

## Phase 5: User Story 3 — Use Multiple Profiles (Priority: P3)

**Goal**: Support multiple named profiles; require explicit profile selection; list available profiles on error

**Independent Test**: Create config with two profiles, request each by name, verify correct data returned

### 5.1 Test Fixtures (RED - Create First)

- [ ] T046 [P] [US3] Create valid-multiple-profiles.yaml test fixture

### 5.2 Write Failing Tests (RED - Before Implementation)

- [ ] T047 [US3] Write ProfileResolverTests in tests/Dottie.Configuration.Tests/ProfileResolverTests.cs
  - Test: GetProfile_ExistingProfile_ReturnsProfile
  - Test: GetProfile_NonExistentProfile_ReturnsErrorWithAvailableList
  - Test: GetProfile_NoProfileSpecified_ReturnsErrorWithAvailableList
  - Test: ListProfiles_ReturnsAllProfileNames
  - **Run tests — verify they FAIL (RED)**

### 5.3 Implementation (GREEN - Minimum to Pass)

- [ ] T048 [US3] Implement ProfileResolver.GetProfile() in src/Dottie.Configuration/ProfileResolver.cs (FR-004, FR-004a)
- [ ] T049 [US3] Implement profile listing for error messages in ProfileResolver (FR-004a)
  - **Run ProfileResolverTests — verify they PASS (GREEN)**

### 5.4 Refactor & Verify Coverage

- [ ] T050 [US3] Refactor US3 code for clarity; verify ≥90% line coverage, ≥80% branch coverage

**Checkpoint**: User Story 3 complete — multiple profiles work, unknown profile shows available list

---

## Phase 6: User Story 4 — Inherit and Override Profile Settings (Priority: P4)

**Goal**: Support extends keyword; merge dotfiles and install blocks per clarification rules; detect cycles

**Independent Test**: Create work profile extending default, verify merged config contains both

### 6.1 Test Fixtures (RED - Create First)

- [ ] T051 [P] [US4] Create valid-inheritance.yaml test fixture
- [ ] T052 [P] [US4] Create invalid-circular-inheritance.yaml test fixture
- [ ] T053 [P] [US4] Create invalid-extends-missing.yaml test fixture

### 6.2 Write Failing Tests (RED - Before Implementation)

- [ ] T054 [US4] Write ProfileMergerTests in tests/Dottie.Configuration.Tests/Inheritance/ProfileMergerTests.cs
  - Test: Merge_DotfilesAppendChildAfterParent
  - Test: Merge_AptPackagesAppend
  - Test: Merge_GithubItemsByRepoKey_ChildOverrides
  - Test: Merge_SnapItemsByNameKey_ChildOverrides
  - Test: Merge_CircularInheritance_ReturnsError (uses invalid-circular-inheritance.yaml)
  - Test: Merge_ExtendsMissingProfile_ReturnsError (uses invalid-extends-missing.yaml)
  - Test: Merge_ValidInheritance_ReturnsMergedProfile (uses valid-inheritance.yaml)
  - **Run tests — verify they FAIL (RED)**

### 6.3 Implementation (GREEN - Minimum to Pass)

- [ ] T055 [US4] Create ResolvedProfile record in src/Dottie.Configuration/Models/ResolvedProfile.cs
- [ ] T056 [US4] Implement ProfileMerger.MergeDotfiles() in src/Dottie.Configuration/Inheritance/ProfileMerger.cs (FR-007a)
- [ ] T057 [US4] Implement ProfileMerger.MergeInstallBlocks() in src/Dottie.Configuration/Inheritance/ProfileMerger.cs (FR-007b,c)
- [ ] T058 [US4] Implement cycle detection in ProfileMerger (FR-008)
  - **Run ProfileMergerTests — verify they PASS (GREEN)**
- [ ] T059 [US4] Integrate ProfileMerger into ProfileResolver for automatic inheritance resolution (FR-006)

### 6.4 Refactor & Verify Coverage

- [ ] T060 [US4] Refactor US4 code for clarity; verify ≥90% line coverage, ≥80% branch coverage

**Checkpoint**: User Story 4 complete — inheritance merges correctly, cycles detected

---

## Phase 7: CLI Integration

**Goal**: Wire configuration library to CLI; implement validate command

### 7.1 Write Failing Tests (RED - Before Implementation)

- [ ] T061 [CLI] Write ValidateCommandTests in tests/Dottie.Cli.Tests/Commands/ValidateCommandTests.cs
  - Test: Execute_ValidConfig_ReturnsZero
  - Test: Execute_InvalidConfig_ReturnsNonZeroAndShowsErrors
  - Test: Execute_MissingProfile_ShowsAvailableProfiles
  - **Run tests — verify they FAIL (RED)**

### 7.2 Implementation (GREEN - Minimum to Pass)

- [ ] T062 [CLI] Implement Program.cs entry point with Spectre.Console.Cli in src/Dottie.Cli/Program.cs
- [ ] T063 [CLI] Implement ValidateCommand in src/Dottie.Cli/Commands/ValidateCommand.cs
- [ ] T064 [CLI] Implement error output formatting with Spectre.Console markup in src/Dottie.Cli/Output/ErrorFormatter.cs
- [ ] T065 [CLI] Implement repo root discovery (manual .git traversal) in src/Dottie.Cli/Utilities/RepoRootFinder.cs
  - **Run ValidateCommandTests — verify they PASS (GREEN)**

### 7.3 Refactor & Verify Coverage

- [ ] T066 [CLI] Refactor CLI code for clarity; verify ≥90% line coverage, ≥80% branch coverage

**Checkpoint**: CLI works — `dottie validate <profile>` parses and validates config

---

## Phase 8: Template Generation (FR-001a)

**Goal**: Generate starter template when dottie.yaml doesn't exist

### 8.1 Write Failing Tests (RED - Before Implementation)

- [ ] T067 [TPL] Write StarterTemplateTests in tests/Dottie.Configuration.Tests/Templates/StarterTemplateTests.cs
  - Test: Generate_CreatesValidYamlWithDefaultProfile
  - Test: Generate_IncludesCommentedOptionalFields
  - Test: ConfigurationLoader_MissingFile_GeneratesTemplate
  - **Run tests — verify they FAIL (RED)**

### 8.2 Implementation (GREEN - Minimum to Pass)

- [ ] T068 [TPL] Create embedded starter template resource in src/Dottie.Configuration/Templates/starter-template.yaml
- [ ] T069 [TPL] Implement StarterTemplate.Generate() in src/Dottie.Configuration/Templates/StarterTemplate.cs
- [ ] T070 [TPL] Integrate template generation into ConfigurationLoader when file missing
  - **Run StarterTemplateTests — verify they PASS (GREEN)**

### 8.3 Refactor & Verify Coverage

- [ ] T071 [TPL] Refactor template code for clarity; verify ≥90% line coverage, ≥80% branch coverage

**Checkpoint**: Missing config triggers template generation with commented optional fields

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, cleanup, and final validation

- [ ] T072 [P] Add XML documentation comments to all public types
- [ ] T073 [P] Update README.md with usage examples
- [ ] T074 Verify AOT publish works: `dotnet publish -c Release -r linux-x64`
- [ ] T075 Verify binary size < 50MB
- [ ] T076 Run quickstart.md validation checklist
- [ ] T077 Performance test: 50-entry config loads in < 2 seconds (SC-004)
- [ ] T078 Generate final coverage report; verify ≥90% line, ≥80% branch overall

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational) ─── BLOCKS ALL USER STORIES
    ↓
┌───────────────────────────────────────────────────┐
│  User Stories can proceed in parallel or by priority  │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐   │
│  │  US1    │ │  US2    │ │  US3    │ │  US4    │   │
│  │ Phase 3 │ │ Phase 4 │ │ Phase 5 │ │ Phase 6 │   │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘   │
└───────────────────────────────────────────────────┘
    ↓
Phase 7 (CLI) ─── Depends on US1-US4
    ↓
Phase 8 (Template) ─── Depends on Phase 7
    ↓
Phase 9 (Polish)
```

### TDD Workflow Within Each User Story

```
┌─────────────────────────────────────────────────────────┐
│  For each User Story phase:                             │
│                                                         │
│  ┌──────────────┐                                       │
│  │ 1. Fixtures  │  Create test data files               │
│  └──────┬───────┘                                       │
│         ↓                                               │
│  ┌──────────────┐                                       │
│  │ 2. Tests     │  Write tests → Run → Verify FAIL     │
│  │    (RED)     │                                       │
│  └──────┬───────┘                                       │
│         ↓                                               │
│  ┌──────────────┐                                       │
│  │ 3. Implement │  Write minimum code → Run → PASS     │
│  │    (GREEN)   │                                       │
│  └──────┬───────┘                                       │
│         ↓                                               │
│  ┌──────────────┐                                       │
│  │ 4. Refactor  │  Improve quality → Tests stay GREEN  │
│  │  + Coverage  │  Verify ≥90% line, ≥80% branch       │
│  └──────────────┘                                       │
└─────────────────────────────────────────────────────────┘
```

### User Story Dependencies

| Story | Depends On | Can Parallel With |
|-------|------------|-------------------|
| US1 (Phase 3) | Phase 2 | — |
| US2 (Phase 4) | Phase 2 | US1, US3, US4 |
| US3 (Phase 5) | Phase 2 | US1, US2, US4 |
| US4 (Phase 6) | Phase 2, US3 (ProfileResolver) | US1, US2 |

### Within-Phase Parallel Opportunities

**Phase 2**: T007-T008 (fixtures), T009-T016 (models) — all parallelizable  
**Phase 3**: T019-T021 (fixtures) — parallelizable  
**Phase 4**: T032-T034 (fixtures) — parallelizable  
**Phase 6**: T051-T053 (fixtures) — parallelizable

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. ✅ Phase 1: Setup
2. ✅ Phase 2: Foundational
3. ✅ Phase 3: User Story 1 (TDD: fixtures → tests → implement → refactor)
4. **VALIDATE**: All tests pass, coverage met
5. Optionally add Phase 7 (CLI) for demo-ready MVP

### Incremental Delivery

| Increment | Contains | Value Delivered | Coverage Gate |
|-----------|----------|-----------------|---------------|
| MVP | Setup + Foundation + US1 + CLI | Parse dotfiles config | ≥90% line |
| +1 | US2 | Parse all install block types | ≥90% line |
| +2 | US3 | Multiple profiles | ≥90% line |
| +3 | US4 | Profile inheritance | ≥90% line |
| +4 | Template | Auto-generate starter config | ≥90% line |

---

## Summary

| Metric | Value |
|--------|-------|
| Total Tasks | 78 |
| Phase 1 (Setup) | 6 tasks |
| Phase 2 (Foundational) | 12 tasks |
| Phase 3 (US1 - P1) | 13 tasks |
| Phase 4 (US2 - P2) | 14 tasks |
| Phase 5 (US3 - P3) | 5 tasks |
| Phase 6 (US4 - P4) | 10 tasks |
| Phase 7 (CLI) | 6 tasks |
| Phase 8 (Template) | 5 tasks |
| Phase 9 (Polish) | 7 tasks |
| Parallelizable [P] | 18 tasks |
| Coverage Verification Tasks | 6 tasks (one per phase) |
| MVP Scope | T001-T031 + T061-T066 (43 tasks) |
