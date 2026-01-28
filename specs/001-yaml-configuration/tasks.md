# Tasks: YAML Configuration System

**Input**: Design documents from `/specs/001-yaml-configuration/`  
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

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

- [ ] T007 [P] Create DottieConfiguration record in src/Dottie.Configuration/Models/DottieConfiguration.cs
- [ ] T008 [P] Create Profile record in src/Dottie.Configuration/Models/Profile.cs
- [ ] T009 [P] Create DotfileEntry record in src/Dottie.Configuration/Models/DotfileEntry.cs
- [ ] T010 [P] Create InstallBlock record in src/Dottie.Configuration/Models/InstallBlocks/InstallBlock.cs
- [ ] T011 [P] Create GithubReleaseItem record in src/Dottie.Configuration/Models/InstallBlocks/GithubReleaseItem.cs
- [ ] T012 [P] Create AptRepoItem record in src/Dottie.Configuration/Models/InstallBlocks/AptRepoItem.cs
- [ ] T013 [P] Create SnapItem record in src/Dottie.Configuration/Models/InstallBlocks/SnapItem.cs
- [ ] T014 [P] Create FontItem record in src/Dottie.Configuration/Models/InstallBlocks/FontItem.cs
- [ ] T015 Create ConfigurationYamlContext (source generator) in src/Dottie.Configuration/Parsing/ConfigurationYamlContext.cs
- [ ] T016 Create ValidationError and ValidationResult records in src/Dottie.Configuration/Validation/ValidationResult.cs
- [ ] T017 [P] Create valid-minimal.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/
- [ ] T018 [P] Create valid-full.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/

**Checkpoint**: Foundation ready — all model types exist, YAML context configured, test fixtures in place

---

## Phase 3: User Story 1 — Define Basic Dotfiles Configuration (Priority: P1) 🎯 MVP

**Goal**: Parse dottie.yaml with profile and dotfile mappings; validate required fields; report errors with line numbers

**Independent Test**: Create minimal dottie.yaml with one dotfile mapping and verify it parses correctly

### Implementation for User Story 1

- [ ] T019 [US1] Implement YamlDeserializer with error handling in src/Dottie.Configuration/Parsing/YamlDeserializer.cs
- [ ] T020 [US1] Implement ConfigurationLoader.Load() in src/Dottie.Configuration/Parsing/ConfigurationLoader.cs
- [ ] T021 [US1] Implement DotfileEntryValidator in src/Dottie.Configuration/Validation/DotfileEntryValidator.cs (FR-009, FR-010)
- [ ] T022 [US1] Implement ProfileValidator in src/Dottie.Configuration/Validation/ProfileValidator.cs (FR-003, FR-005)
- [ ] T023 [US1] Implement ConfigurationValidator orchestration in src/Dottie.Configuration/Validation/ConfigurationValidator.cs (FR-021)
- [ ] T024 [US1] Implement tilde expansion utility in src/Dottie.Configuration/Utilities/PathExpander.cs (FR-011)
- [ ] T025 [P] [US1] Create invalid-missing-source.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/
- [ ] T026 [P] [US1] Create invalid-missing-target.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/
- [ ] T027 [P] [US1] Create invalid-yaml-syntax.yaml test fixture in tests/Dottie.Configuration.Tests/Fixtures/
- [ ] T028 [US1] Write ConfigurationLoaderTests in tests/Dottie.Configuration.Tests/Parsing/ConfigurationLoaderTests.cs
- [ ] T029 [US1] Write DotfileEntryValidatorTests in tests/Dottie.Configuration.Tests/Validation/DotfileEntryValidatorTests.cs

**Checkpoint**: User Story 1 complete — can parse valid configs, reject invalid ones with line numbers

---

## Phase 4: User Story 2 — Configure Package Installation Sources (Priority: P2)

**Goal**: Parse all install block types (apt, github, snap, apt-repo, scripts, fonts) with validation

**Independent Test**: Create config with one apt package and verify it parses correctly

### Implementation for User Story 2

- [ ] T030 [US2] Implement InstallBlockValidator in src/Dottie.Configuration/Validation/InstallBlockValidator.cs (FR-012)
- [ ] T031 [US2] Implement GithubReleaseValidator in src/Dottie.Configuration/Validation/GithubReleaseValidator.cs (FR-013)
- [ ] T032 [US2] Implement AptRepoValidator in src/Dottie.Configuration/Validation/AptRepoValidator.cs (FR-015)
- [ ] T033 [US2] Implement SnapValidator in src/Dottie.Configuration/Validation/SnapValidator.cs (FR-018)
- [ ] T034 [US2] Implement FontValidator in src/Dottie.Configuration/Validation/FontValidator.cs (FR-017)
- [ ] T035 [US2] Implement ScriptPathValidator in src/Dottie.Configuration/Validation/ScriptPathValidator.cs (FR-016, FR-019)
- [ ] T036 [US2] Implement ArchitectureDetector in src/Dottie.Configuration/Utilities/ArchitectureDetector.cs (FR-013a)
- [ ] T037 [P] [US2] Create invalid-github-missing-repo.yaml test fixture
- [ ] T038 [P] [US2] Create invalid-script-traversal.yaml test fixture
- [ ] T039 [P] [US2] Create valid-all-install-types.yaml test fixture
- [ ] T040 [US2] Write InstallBlockValidatorTests in tests/Dottie.Configuration.Tests/Validation/InstallBlockValidatorTests.cs
- [ ] T041 [US2] Write ScriptPathValidatorTests in tests/Dottie.Configuration.Tests/Validation/ScriptPathValidatorTests.cs

**Checkpoint**: User Story 2 complete — all install block types parse and validate correctly

---

## Phase 5: User Story 3 — Use Multiple Profiles (Priority: P3)

**Goal**: Support multiple named profiles; require explicit profile selection; list available profiles on error

**Independent Test**: Create config with two profiles, request each by name, verify correct data returned

### Implementation for User Story 3

- [ ] T042 [US3] Implement ProfileResolver.GetProfile() in src/Dottie.Configuration/ProfileResolver.cs (FR-004, FR-004a)
- [ ] T043 [US3] Implement profile listing for error messages in ProfileResolver (FR-004a)
- [ ] T044 [P] [US3] Create valid-multiple-profiles.yaml test fixture
- [ ] T045 [US3] Write ProfileResolverTests in tests/Dottie.Configuration.Tests/ProfileResolverTests.cs

**Checkpoint**: User Story 3 complete — multiple profiles work, unknown profile shows available list

---

## Phase 6: User Story 4 — Inherit and Override Profile Settings (Priority: P4)

**Goal**: Support extends keyword; merge dotfiles and install blocks per clarification rules; detect cycles

**Independent Test**: Create work profile extending default, verify merged config contains both

### Implementation for User Story 4

- [ ] T046 [US4] Create ResolvedProfile record in src/Dottie.Configuration/Models/ResolvedProfile.cs
- [ ] T047 [US4] Implement ProfileMerger.MergeDotfiles() in src/Dottie.Configuration/Inheritance/ProfileMerger.cs (FR-007a)
- [ ] T048 [US4] Implement ProfileMerger.MergeInstallBlocks() in src/Dottie.Configuration/Inheritance/ProfileMerger.cs (FR-007b,c)
- [ ] T049 [US4] Implement cycle detection in ProfileMerger (FR-008)
- [ ] T050 [US4] Integrate ProfileMerger into ProfileResolver for automatic inheritance resolution (FR-006)
- [ ] T051 [P] [US4] Create valid-inheritance.yaml test fixture
- [ ] T052 [P] [US4] Create invalid-circular-inheritance.yaml test fixture
- [ ] T053 [P] [US4] Create invalid-extends-missing.yaml test fixture
- [ ] T054 [US4] Write ProfileMergerTests in tests/Dottie.Configuration.Tests/Inheritance/ProfileMergerTests.cs

**Checkpoint**: User Story 4 complete — inheritance merges correctly, cycles detected

---

## Phase 7: CLI Integration

**Goal**: Wire configuration library to CLI; implement validate command

### Implementation

- [ ] T055 Implement Program.cs entry point with Spectre.Console.Cli in src/Dottie.Cli/Program.cs
- [ ] T056 Implement ValidateCommand in src/Dottie.Cli/Commands/ValidateCommand.cs
- [ ] T057 Implement error output formatting with Spectre.Console markup in src/Dottie.Cli/Output/ErrorFormatter.cs
- [ ] T058 Implement repo root discovery (manual .git traversal) in src/Dottie.Cli/Utilities/RepoRootFinder.cs
- [ ] T059 Write ValidateCommandTests in tests/Dottie.Cli.Tests/Commands/ValidateCommandTests.cs

**Checkpoint**: CLI works — `dottie validate <profile>` parses and validates config

---

## Phase 8: Template Generation (FR-001a)

**Goal**: Generate starter template when dottie.yaml doesn't exist

### Implementation

- [ ] T060 Create embedded starter template resource in src/Dottie.Configuration/Templates/starter-template.yaml
- [ ] T061 Implement StarterTemplate.Generate() in src/Dottie.Configuration/Templates/StarterTemplate.cs
- [ ] T062 Integrate template generation into ConfigurationLoader when file missing
- [ ] T063 Write StarterTemplateTests in tests/Dottie.Configuration.Tests/Templates/StarterTemplateTests.cs

**Checkpoint**: Missing config triggers template generation with commented optional fields

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, cleanup, and final validation

- [ ] T064 [P] Add XML documentation comments to all public types
- [ ] T065 [P] Update README.md with usage examples
- [ ] T066 Verify AOT publish works: `dotnet publish -c Release -r linux-x64`
- [ ] T067 Verify binary size < 50MB
- [ ] T068 Run quickstart.md validation checklist
- [ ] T069 Performance test: 50-entry config loads in < 2 seconds (SC-004)

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

### User Story Dependencies

| Story | Depends On | Can Parallel With |
|-------|------------|-------------------|
| US1 (Phase 3) | Phase 2 | — |
| US2 (Phase 4) | Phase 2 | US1, US3, US4 |
| US3 (Phase 5) | Phase 2 | US1, US2, US4 |
| US4 (Phase 6) | Phase 2, US3 (ProfileResolver) | US1, US2 |

### Within-Phase Parallel Opportunities

**Phase 2**: T007-T014 (all models), T017-T018 (fixtures) — all parallelizable  
**Phase 3**: T025-T027 (fixtures) — parallelizable  
**Phase 4**: T037-T039 (fixtures) — parallelizable  
**Phase 6**: T051-T053 (fixtures) — parallelizable

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. ✅ Phase 1: Setup
2. ✅ Phase 2: Foundational
3. ✅ Phase 3: User Story 1
4. **VALIDATE**: Test basic config parsing works
5. Optionally add Phase 7 (CLI) for demo-ready MVP

### Incremental Delivery

| Increment | Contains | Value Delivered |
|-----------|----------|-----------------|
| MVP | Setup + Foundation + US1 + CLI | Parse dotfiles config |
| +1 | US2 | Parse all install block types |
| +2 | US3 | Multiple profiles |
| +3 | US4 | Profile inheritance |
| +4 | Template | Auto-generate starter config |

---

## Summary

| Metric | Value |
|--------|-------|
| Total Tasks | 69 |
| Phase 1 (Setup) | 6 tasks |
| Phase 2 (Foundational) | 12 tasks |
| Phase 3 (US1 - P1) | 11 tasks |
| Phase 4 (US2 - P2) | 12 tasks |
| Phase 5 (US3 - P3) | 4 tasks |
| Phase 6 (US4 - P4) | 9 tasks |
| Phase 7 (CLI) | 5 tasks |
| Phase 8 (Template) | 4 tasks |
| Phase 9 (Polish) | 6 tasks |
| Parallelizable [P] | 24 tasks |
| MVP Scope | T001-T029 + T055-T057 (35 tasks) |
