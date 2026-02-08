# Tasks: OS Release Variable Substitution

**Input**: Design documents from `/specs/011-os-release-variables/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: TDD is MANDATORY per constitution. Every implementation task has a preceding test task.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story (US1–US4) — only in user story phases

---

## Phase 1: Setup

**Purpose**: Create result types and supporting infrastructure needed by all subsequent phases

- [X] T001 Create `VariableResolutionResult` record in `src/Dottie.Configuration/Utilities/VariableResolutionResult.cs` (fields: `ResolvedValue`, `UnresolvedVariables`, `HasErrors` per data-model.md)
- [X] T002 Create `VariableResolutionError` record in `src/Dottie.Configuration/Utilities/VariableResolutionError.cs` (fields: `ProfileName`, `EntryIdentifier`, `FieldName`, `VariableName`, `Message` per contracts/IVariableResolver.md)
- [X] T003 Create `ConfigurationResolutionResult` record in `src/Dottie.Configuration/Utilities/ConfigurationResolutionResult.cs` (fields: `Configuration`, `Errors`, `HasErrors`)
- [X] T004 Verify project builds with `dotnet build Dottie.slnx --warnaserror`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core utilities that MUST be complete before ANY user story can proceed — `OsReleaseParser`, `ArchitectureDetector` extension, `VariableResolver` engine

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### OsReleaseParser

- [X] T005 [P] Write `OsReleaseParserTests` in `tests/Dottie.Configuration.Tests/Utilities/OsReleaseParserTests.cs` — test `Parse(content)` with: standard Ubuntu content, double-quoted values, single-quoted values, unquoted values, comment lines, blank lines, empty content, malformed lines (no `=`), duplicate keys, keys with empty values
- [X] T006 Implement `OsReleaseParser.Parse(string content)` static method in `src/Dottie.Configuration/Utilities/OsReleaseParser.cs` — splits lines, skips comments/blanks, splits on first `=`, strips quotes, returns `IReadOnlyDictionary<string, string>`
- [X] T007 Add `TryReadFromSystem` tests to `tests/Dottie.Configuration.Tests/Utilities/OsReleaseParserTests.cs` — test with existing file path, non-existent file path (returns empty + `isAvailable=false`)
- [X] T008 Implement `OsReleaseParser.TryReadFromSystem(string filePath)` in `src/Dottie.Configuration/Utilities/OsReleaseParser.cs` — reads file if exists, calls `Parse`, returns tuple `(variables, isAvailable)`

### ArchitectureDetector Extension

- [X] T009 [P] Add `RawArchitecture` tests in `tests/Dottie.Configuration.Tests/Utilities/ArchitectureDetectorTests.cs` — verify `RawArchitecture` returns a non-null, non-empty string matching one of: `x86_64`, `aarch64`, `armv7l`, `unknown`
- [X] T010 Add `RawArchitecture` static property to `src/Dottie.Configuration/Utilities/ArchitectureDetector.cs` — maps `RuntimeInformation.OSArchitecture` to uname-style strings (`X64`→`x86_64`, `Arm64`→`aarch64`, `Arm`→`armv7l`, `X86`/other→`unknown`)
- [X] T011 Update `CurrentArchitecture` mapping for `Arm` from `"arm"` to `"armhf"` in `src/Dottie.Configuration/Utilities/ArchitectureDetector.cs` and update corresponding tests

### VariableResolver Core

- [X] T012 [P] Write `VariableResolverTests` for `ResolveString` in `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs` — test cases: no variables (unchanged), single variable, multiple variables, unknown variable (error), deferred variable (left as-is, no error), mixed resolved/deferred/unresolved, empty string, null/whitespace handling
- [X] T013 Implement `VariableResolver.ResolveString` static method in `src/Dottie.Configuration/Utilities/VariableResolver.cs` — regex `\$\{([A-Za-z_][A-Za-z0-9_]*)\}` single-pass replacement with `MatchEvaluator`, returns `VariableResolutionResult`
- [X] T014 Write `VariableResolverTests` for `BuildVariableSet` in `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs` — test: OS release vars + architecture vars combined, architecture vars override conflicting OS release keys, empty OS release dict works
- [X] T015 Implement `VariableResolver.BuildVariableSet` static method in `src/Dottie.Configuration/Utilities/VariableResolver.cs` — merges OS release dictionary with `ARCH` and `MS_ARCH` from `ArchitectureDetector`

**Checkpoint**: Foundation ready — all three core utilities (`OsReleaseParser`, `ArchitectureDetector`, `VariableResolver`) are implemented and tested. User story implementation can now begin.

---

## Phase 3: User Story 1 — Portable APT Repository Configuration (Priority: P1) 🎯 MVP

**Goal**: `${VERSION_CODENAME}` and `${MS_ARCH}` variables work in `aptrepo` entry fields (`repo`, `key_url`, `packages`), resolved at config load time.

**Independent Test**: Load a YAML config with aptrepo entries containing `${VERSION_CODENAME}` and `${MS_ARCH}`, verify the resolved `AptRepoItem` has expanded values matching the injected variables.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T016 [P] [US1] Write `VariableResolverTests` for `ResolveConfiguration` — aptrepo scenario in `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs`: config with one profile containing an `AptRepoItem` whose `Repo` has `${MS_ARCH}` and `${VERSION_CODENAME}`, `KeyUrl` has `${VERSION_CODENAME}`, `Packages` has `${MS_ARCH}` — verify all fields resolve correctly
- [X] T017 [P] [US1] Write `VariableResolverTests` for backwards compatibility in `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs`: config with aptrepo entries containing NO variables — verify output equals input exactly (FR-009)
- [X] T018 [P] [US1] Create YAML test fixture `tests/Dottie.Configuration.Tests/Fixtures/variable-aptrepo.yaml` — profile with aptrepo entry using `${MS_ARCH}` and `${VERSION_CODENAME}` in repo URL
- [X] T019 [P] [US1] Write `ConfigurationLoaderTests` for variable resolution in `tests/Dottie.Configuration.Tests/Parsing/ConfigurationLoaderTests.cs` — load `variable-aptrepo.yaml` via loader, verify aptrepo `Repo` field has variables resolved

### Implementation for User Story 1

- [X] T020 [US1] Implement `VariableResolver.ResolveConfiguration` static method in `src/Dottie.Configuration/Utilities/VariableResolver.cs` — iterates profiles, resolves `AptRepoItem` fields (`Repo`, `KeyUrl`, `Packages`) with no deferred variables, creates new records with `with` expressions, collects errors with context
- [X] T021 [US1] Integrate variable resolution into `ConfigurationLoader.LoadFromString` in `src/Dottie.Configuration/Parsing/ConfigurationLoader.cs` — after YAML deserialize: call `OsReleaseParser.TryReadFromSystem`, call `VariableResolver.BuildVariableSet`, call `VariableResolver.ResolveConfiguration`, add resolution errors to `LoadResult.Errors`
- [X] T022 [US1] Verify all existing `ConfigurationLoaderTests` still pass (backwards compatibility checkpoint)

**Checkpoint**: APT repository entries with `${MS_ARCH}` and `${VERSION_CODENAME}` are fully resolved at config load time. Existing configs without variables are unchanged. User Story 1 is independently testable.

---

## Phase 4: User Story 2 — Architecture-Aware GitHub Release Downloads (Priority: P2)

**Goal**: `${ARCH}`, `${MS_ARCH}` resolved at load time in `github` asset/binary fields. `${RELEASE_VERSION}` deferred at load time, resolved per-item in `GithubReleaseInstaller` after version determination.

**Independent Test**: Load a YAML config with github entries containing `${MS_ARCH}` in asset pattern and `${RELEASE_VERSION}`, verify global vars resolve at load time and `${RELEASE_VERSION}` remains deferred then resolves at install time.

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T023 [P] [US2] Write `VariableResolverTests` for `ResolveConfiguration` — github scenario in `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs`: config with `GithubReleaseItem` whose `Asset` has `${MS_ARCH}` and `${RELEASE_VERSION}`, `Binary` has `${ARCH}` — verify `${MS_ARCH}` and `${ARCH}` resolve, `${RELEASE_VERSION}` is preserved as `${RELEASE_VERSION}` (deferred)
- [X] T024 [P] [US2] Write `GithubReleaseInstallerTests` for `${RELEASE_VERSION}` resolution in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs` — test that when version is `"1.2.0"` and asset pattern contains `${RELEASE_VERSION}`, the pattern resolves to `1.2.0` before asset matching
- [X] T025 [P] [US2] Create YAML test fixture `tests/Dottie.Configuration.Tests/Fixtures/variable-github.yaml` — profile with github entry using `${MS_ARCH}` and `${RELEASE_VERSION}` in asset pattern

### Implementation for User Story 2

- [X] T026 [US2] Extend `VariableResolver.ResolveConfiguration` in `src/Dottie.Configuration/Utilities/VariableResolver.cs` — add `GithubReleaseItem` field resolution for `Asset` and `Binary` with `RELEASE_VERSION` as deferred variable
- [X] T027 [US2] Modify `GithubReleaseInstaller` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs` — after version is determined (from `item.Version` or API latest), resolve `${RELEASE_VERSION}` in `item.Asset` and `item.Binary` using `VariableResolver.ResolveString` before asset matching
- [X] T028 [US2] Verify all existing `GithubReleaseInstallerTests` still pass (backwards compatibility checkpoint)

**Checkpoint**: GitHub release entries with `${ARCH}`, `${MS_ARCH}` resolve at load time. `${RELEASE_VERSION}` resolves per-item at install time. User Story 2 is independently testable.

---

## Phase 5: User Story 3 — Platform-Specific Dotfile Sources (Priority: P3)

**Goal**: `${VERSION_CODENAME}`, `${ARCH}`, `${MS_ARCH}` variables work in `dotfiles` entry fields (`source`, `target`), resolved at config load time.

**Independent Test**: Load a YAML config with dotfile entries containing `${VERSION_CODENAME}` in source, verify the resolved `DotfileEntry` has the expanded path.

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T029 [P] [US3] Write `VariableResolverTests` for `ResolveConfiguration` — dotfile scenario in `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs`: config with `DotfileEntry` whose `Source` has `${VERSION_CODENAME}` and `Target` has `${ARCH}` — verify both fields resolve
- [X] T030 [P] [US3] Write `VariableResolverTests` for `${RELEASE_VERSION}` in dotfile context in `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs` — config with `DotfileEntry` whose `Source` references `${RELEASE_VERSION}` — verify error reported (not deferred in dotfile context)
- [X] T031 [P] [US3] Create YAML test fixture `tests/Dottie.Configuration.Tests/Fixtures/variable-dotfiles.yaml` — profile with dotfile entry using `${VERSION_CODENAME}` in source

### Implementation for User Story 3

- [X] T032 [US3] Extend `VariableResolver.ResolveConfiguration` in `src/Dottie.Configuration/Utilities/VariableResolver.cs` — add `DotfileEntry` field resolution for `Source` and `Target` with NO deferred variables (all variables must resolve or error)
- [X] T033 [US3] Write `ConfigurationLoaderTests` for dotfile variable resolution in `tests/Dottie.Configuration.Tests/Parsing/ConfigurationLoaderTests.cs` — load `variable-dotfiles.yaml`, verify dotfile `Source` field has variables resolved

**Checkpoint**: Dotfile entries with variables resolve at load time. `${RELEASE_VERSION}` in dotfile context produces error. User Story 3 is independently testable.

---

## Phase 6: User Story 4 — Graceful Handling of Missing OS Information (Priority: P4)

**Goal**: When `/etc/os-release` is missing, warn but continue for entries without OS release variables. Error clearly for entries that reference unavailable OS release variables.

**Independent Test**: Configure `ConfigurationLoader` with a non-existent OS release path, load a config with and without OS variables, verify warning + correct error behavior.

### Tests for User Story 4

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T034 [P] [US4] Write `ConfigurationLoaderTests` for missing OS release file in `tests/Dottie.Configuration.Tests/Parsing/ConfigurationLoaderTests.cs` — loader with non-existent `/etc/os-release` path, config with NO OS release variables → loads successfully with warning
- [X] T035 [P] [US4] Write `ConfigurationLoaderTests` for missing OS release with variable reference in `tests/Dottie.Configuration.Tests/Parsing/ConfigurationLoaderTests.cs` — loader with non-existent `/etc/os-release` path, config with `${VERSION_CODENAME}` in aptrepo → returns error identifying the unresolvable variable, profile, entry, and field
- [X] T036 [P] [US4] Write `VariableResolverTests` for unsupported architecture error in `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs` — variables where `MS_ARCH` is `"unknown"`, config references `${MS_ARCH}` → verify error behavior per FR-004

### Implementation for User Story 4

- [X] T037 [US4] Add OS release availability warning to `ConfigurationLoader.LoadFromString` in `src/Dottie.Configuration/Parsing/ConfigurationLoader.cs` — when `OsReleaseParser.TryReadFromSystem` returns `isAvailable=false`, add a warning to `LoadResult` but continue processing
- [X] T038 [US4] Add unresolvable variable error message formatting in `src/Dottie.Configuration/Utilities/VariableResolver.cs` — error messages follow format: `"Unresolvable variable '${VARIABLE_NAME}' in profile 'PROFILE', ENTRY_TYPE 'ENTRY_ID', field 'FIELD'" `(per contracts/IVariableResolver.md and FR-011)
- [X] T039 [US4] Verify error message includes variable name, profile name, entry identifier, and field name in `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs`

**Checkpoint**: Missing OS release produces warning but allows variable-free configs. Unresolvable variables produce clear, actionable error messages. User Story 4 is independently testable.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, code quality, and documentation

- [X] T040 [P] Create YAML test fixture `tests/Dottie.Configuration.Tests/Fixtures/variable-mixed.yaml` — profile with all three source types (aptrepo + github + dotfiles) using variables, for end-to-end loader test
- [X] T041 Write `ConfigurationLoaderTests` end-to-end test in `tests/Dottie.Configuration.Tests/Parsing/ConfigurationLoaderTests.cs` — load `variable-mixed.yaml` with all source types, verify all variable substitutions across aptrepo, github, and dotfile entries
- [X] T042 [P] Add XML documentation comments to all new public types and methods across `OsReleaseParser.cs`, `VariableResolver.cs`, `VariableResolutionResult.cs`, `VariableResolutionError.cs`, `ConfigurationResolutionResult.cs`
- [X] T043 [P] Write a performance sanity test in `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs` — build a 30-item `DottieConfiguration` with variables in every field, run `ResolveConfiguration`, assert completes in under 1 second (SC-005). Use `Stopwatch` with a generous 1000ms threshold.
- [X] T044 Run `dotnet build Dottie.slnx --warnaserror` to verify zero analyzer warnings
- [X] T045 Run `dotnet test Dottie.slnx` to verify all tests pass (unit tests)
- [X] T046 Run `dotnet test Dottie.slnx --collect:"XPlat Code Coverage"` and verify ≥90% line coverage and ≥80% branch coverage for all new production code (`OsReleaseParser.cs`, `VariableResolver.cs`, `VariableResolutionResult.cs`, `VariableResolutionError.cs`, `ConfigurationResolutionResult.cs`, `ArchitectureDetector.cs` changes) per constitution §TDD Coverage Requirements
- [X] T047 Run `.\tests\run-integration-tests.ps1` and verify all integration tests pass per constitution §Test Execution Before Commit
- [X] T048 Run quickstart.md validation — verify build, test, and TDD steps are accurate

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — result type records only
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 — implements `ResolveConfiguration` and `ConfigurationLoader` integration
- **User Story 2 (Phase 4)**: Depends on Phase 3 (extends `ResolveConfiguration` and uses loader integration)
- **User Story 3 (Phase 5)**: Depends on Phase 4 (extends `ResolveConfiguration` in same `VariableResolver.cs`)
- **User Story 4 (Phase 6)**: Depends on Phase 5 (adds error formatting in same `VariableResolver.cs`)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Introduces `ResolveConfiguration` + `ConfigurationLoader` integration — other stories extend these
- **User Story 2 (P2)**: Can start after US1 — extends `ResolveConfiguration` for github, modifies `GithubReleaseInstaller`
- **User Story 3 (P3)**: Depends on US2 — extends `ResolveConfiguration` for dotfiles in `VariableResolver.cs` (same file as US2)
- **User Story 4 (P4)**: Depends on US3 — adds error formatting to `VariableResolver.cs` (same file as US2/US3)

**⚠️ Serialization constraint**: US2, US3, and US4 implementation tasks all modify `VariableResolver.ResolveConfiguration` in the same file (`VariableResolver.cs`). Implementation MUST be serialized: US1 → US2 → US3 → US4. **Test tasks** (T023–T025, T029–T031, T034–T036) target separate test files and CAN run in parallel after US1 tests complete.

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD)
- Test fixtures before test code
- Resolver extension before loader integration
- Verify backwards compatibility after each story

### Parallel Opportunities

**Phase 2 (Foundational)**:
```
T005 (OsReleaseParser tests)  ║  T009 (ArchDetector tests)  ║  T012 (VarResolver tests)
         ↓                    ║           ↓                  ║           ↓
T006 (OsReleaseParser impl)   ║  T010 (RawArch impl)        ║  T013 (ResolveString impl)
         ↓                    ║  T011 (armhf fix)            ║           ↓
T007 (TryRead tests)          ║                              ║  T014 (BuildVarSet tests)
         ↓                    ║                              ║           ↓
T008 (TryRead impl)           ║                              ║  T015 (BuildVarSet impl)
```

**Phase 3–6 (User Stories 1–4)** — after Phase 2:
```
US1 tests (T016-T019)  →  US1 impl (T020-T022)
                              ↓
            ┌─────────────────┼──────────────────┐
            ↓                 ↓                  ↓
US2 tests (T023-T025)   US3 tests (T029-T031)   US4 tests (T034-T036)   [P] tests parallel
            ↓
US2 impl (T026-T028)                                                    serial: VariableResolver.cs
            ↓
US3 impl (T032-T033)                                                    serial: VariableResolver.cs
            ↓
US4 impl (T037-T039)                                                    serial: VariableResolver.cs
```

⚠️ Implementation tasks are serialized because US2/US3/US4 all extend `ResolveConfiguration` in `VariableResolver.cs`.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T004)
2. Complete Phase 2: Foundational (T005–T015)
3. Complete Phase 3: User Story 1 (T016–T022)
4. **STOP and VALIDATE**: Test US1 independently — load a YAML config with aptrepo variables, verify resolution
5. Deploy/demo if ready — MVP delivers portable APT repo configurations

### Incremental Delivery

1. Setup + Foundational → Core variable engine ready
2. Add User Story 1 → Test independently → **MVP** (portable aptrepo configs)
3. Add User Story 2 → Test independently → GitHub releases with arch-aware + version variables
4. Add User Story 3 → Test independently → Dotfile path variables
5. Add User Story 4 → Test independently → Graceful degradation on missing OS info
6. Polish → End-to-end validation, docs, cleanup

### Suggested MVP Scope

**User Story 1 only** — delivers the highest-value use case (portable APT repository URLs) with the smallest implementation surface. Phases 1 + 2 + 3 = **22 tasks**.

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [US#] label maps task to specific user story for traceability
- TDD is MANDATORY: tests MUST be written and FAIL before implementation
- All model records use `init` properties — resolution creates new instances via `with`
- `${RELEASE_VERSION}` is the only deferred variable; it is scoped to `github` entries only
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
