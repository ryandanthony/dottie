# Tasks: GitHub Release Asset Type

**Input**: Design documents from `/specs/012-github-release-type/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Included per constitution TDD requirement. All production code MUST have tests written first.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Exact file paths included in all task descriptions

---

## Phase 1: Setup

**Purpose**: New enum file and YAML test fixtures — foundational types needed by all stories

- [X] T001 [P] Create `GithubReleaseAssetType` enum in `src/Dottie.Configuration/Models/InstallBlocks/GithubReleaseAssetType.cs` with `Binary = 0` (default) and `Deb = 1` values, XML doc comments, copyright header per code standards
- [X] T002 [P] Create test fixture `valid-github-deb-type.yaml` in `tests/Dottie.Configuration.Tests/Fixtures/` with profiles containing `type: deb` entries (with and without binary field) and `type: binary` entries, for use across multiple test phases

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Model and validation changes that MUST be complete before any user story installer work

**⚠️ CRITICAL**: No installer work (Phases 3–7) can begin until this phase is complete

### Tests for Foundational Phase

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T003 [P] Write tests for `GithubReleaseAssetType` default on `GithubReleaseItem` in `tests/Dottie.Configuration.Tests/Models/InstallBlocks/GithubReleaseItemTests.cs`: verify `Type` defaults to `Binary` when not set; verify `Type` can be set to `Deb`; verify record equality includes `Type`
- [X] T004 [P] Write tests for conditional `binary` validation in `tests/Dottie.Configuration.Tests/Validation/InstallBlockValidatorTests.cs`: verify `binary` required when `Type` is `Binary`; verify `binary` required when `Type` is omitted (default); verify no `binary` validation error when `Type` is `Deb`; verify `binary` field ignored (no error) when `Type` is `Deb` and `binary` is provided
- [X] T005 [P] Write YAML deserialization tests in `tests/Dottie.Configuration.Tests/Parsing/ConfigurationLoaderTests.cs`: verify `type: deb` deserializes to `GithubReleaseAssetType.Deb`; verify `type: binary` deserializes to `GithubReleaseAssetType.Binary`; verify missing `type` field defaults to `Binary`; verify fixture `valid-github-deb-type.yaml` loads without errors

### Implementation for Foundational Phase

- [X] T006 Modify `GithubReleaseItem` in `src/Dottie.Configuration/Models/InstallBlocks/GithubReleaseItem.cs`: change `Binary` from `required` to optional (`string?`); add `Type` property with default `GithubReleaseAssetType.Binary`; update XML doc comments
- [X] T007 Modify `InstallBlockValidator.ValidateGithubRelease()` in `src/Dottie.Configuration/Validation/InstallBlockValidator.cs`: make `binary` field validation conditional — only require when `item.Type == GithubReleaseAssetType.Binary`
- [X] T008 Verify all T003–T005 tests pass; verify all existing tests still pass with `dotnet test`; verify build passes with `dotnet build --warnaserror`

**Checkpoint**: Model and validation updated. All existing configs still valid. New `type` field parses correctly. Ready for installer work.

---

## Phase 3: User Story 1 — Install a .deb Package from a GitHub Release (Priority: P1) 🎯 MVP

**Goal**: Users can configure `type: deb` and have `.deb` assets downloaded and installed via `dpkg -i` with automatic dependency resolution.

**Independent Test**: Configure `type: deb` for `jgraph/drawio-desktop`, run install, verify package installed.

### Tests for User Story 1

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T009 [P] [US1] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeDeb_DownloadsAndInstallsViaDpkg` — mock HttpDownloader to return .deb bytes, configure FakeProcessRunner to succeed for `dpkg-deb`, `dpkg -s` (not installed), `dpkg -i`, and `apt-get install -f`; verify `dpkg -i` was called with the downloaded file; verify `apt-get install -f -y` was called; verify result is `Success` with "installed via dpkg" message
- [X] T010 [P] [US1] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeDeb_WithVersion_DownloadsSpecificRelease` — verify that when `Version` is set, the installer fetches the specific release tag from the API
- [X] T011 [P] [US1] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeDeb_VariableSubstitution_ResolvesAssetPattern` — verify `${RELEASE_VERSION}` and `${MS_ARCH}` are substituted in the asset pattern before matching

### Implementation for User Story 1

- [X] T012 [US1] Add deb installation path to `GithubReleaseInstaller` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs`: add private method `InstallDebPackageAsync()` that downloads the .deb asset to a temp directory, runs `dpkg-deb --showformat='${Package}' -W <file>` to extract package name, runs `sudo dpkg -i <file>`, runs `sudo apt-get install -f -y` for dependency resolution, and cleans up temp files in a `finally` block
- [X] T013 [US1] Add dispatch logic in `InstallSingleItemAsync()` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs`: after download/asset-matching, branch on `item.Type` — `Binary` follows existing path, `Deb` calls `InstallDebPackageAsync()`
- [X] T014 [US1] Verify all T009–T011 tests pass; verify all existing binary-type tests still pass unchanged

**Checkpoint**: `type: deb` installs .deb packages successfully. Existing binary behavior unchanged. Story 1 is the MVP.

---

## Phase 4: User Story 2 — Backward-Compatible Default Behavior (Priority: P1)

**Goal**: All existing configurations (without `type` field) continue to work identically.

**Independent Test**: Run existing test suite and all existing YAML fixtures — zero failures, zero behavior changes.

### Tests for User Story 2

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T015 [P] [US2] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeOmitted_FollowsBinaryPath` — verify that a `GithubReleaseItem` without `Type` set follows the existing binary installation flow (archive extraction or direct copy)
- [X] T016 [P] [US2] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeBinaryExplicit_IdenticalToOmitted` — verify that `Type = GithubReleaseAssetType.Binary` produces identical behavior to omitting the type

### Implementation for User Story 2

- [X] T017 [US2] Verify the dispatch logic from T013 correctly defaults to binary path when `item.Type` is `Binary` (the default). This should already work from Phase 3 implementation — this task validates it explicitly
- [X] T018 [US2] Run full existing test suite (`dotnet test`) — confirm zero regressions. All existing `GithubReleaseInstallerTests`, `InstallBlockValidatorTests`, `GithubReleaseItemTests`, `ConfigurationLoaderTests`, and `ProfileMergerTests` must pass unchanged

**Checkpoint**: 100% backward compatibility confirmed. No existing test failures.

---

## Phase 5: User Story 3 — Skip Already-Installed .deb Packages (Priority: P2)

**Goal**: Idempotent behavior — skip download/install when the package is already present on the system.

**Independent Test**: Install a package, run again, verify "already installed" skip with no download.

### Tests for User Story 3

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T019 [P] [US3] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeDeb_PackageAlreadyInstalled_Skips` — mock `dpkg-deb -W` to return package name, mock `dpkg -s` to succeed (exit code 0), verify result is `Skipped` with "already installed" message, verify `dpkg -i` was NOT called
- [X] T020 [P] [US3] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeDeb_PackageNotInstalled_Proceeds` — mock `dpkg -s` to fail (exit code 1), verify installation proceeds with `dpkg -i`

### Implementation for User Story 3

- [X] T021 [US3] Add idempotency check to `InstallDebPackageAsync()` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs`: after downloading the .deb and extracting the package name via `dpkg-deb -W`, run `dpkg -s <package-name>`; if exit code is 0, return `InstallResult.Skipped()` with "package '{package}' already installed" and clean up temp files
- [X] T022 [US3] Verify T019–T020 tests pass; verify existing idempotency tests for binary type still pass

**Checkpoint**: Re-running install with `type: deb` skips already-installed packages. Binary idempotency unchanged.

---

## Phase 6: User Story 4 — Dry Run Preview for .deb Installation (Priority: P2)

**Goal**: `--dry-run` validates the release and asset exist without downloading or installing.

**Independent Test**: Run with `--dry-run`, verify "would be installed via dpkg" output, no download or dpkg calls.

### Tests for User Story 4

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T023 [P] [US4] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `DryRun_TypeDeb_ValidRelease_ReportsWouldInstall` — set `context.DryRun = true`, mock GitHub API to return valid release with matching asset, verify result is `Skipped` with "would be installed via dpkg" message, verify no download or dpkg calls were made
- [X] T024 [P] [US4] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `DryRun_TypeDeb_InvalidRelease_ReportsError` — set `context.DryRun = true`, mock GitHub API to return 404, verify result is `Failed` with appropriate error message

### Implementation for User Story 4

- [X] T025 [US4] Add dry-run support for deb type in `GithubReleaseInstaller` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs`: in the deb dispatch path, when `context.DryRun` is true, perform the existing release/asset validation (API check), then return `InstallResult.Skipped()` with "{repo}: would be installed via dpkg" without downloading
- [X] T026 [US4] Verify T023–T024 tests pass; verify existing binary dry-run tests still pass

**Checkpoint**: Dry-run for `type: deb` works correctly. Binary dry-run unchanged.

---

## Phase 7: User Story 5 — Clear Errors When Conditions Are Not Met (Priority: P3)

**Goal**: Actionable error messages for all failure conditions: no sudo, no dpkg, invalid asset, unknown type.

**Independent Test**: Simulate each failure condition, verify specific error messages.

### Tests for User Story 5

> **Write these tests FIRST, ensure they FAIL before implementation**

- [X] T027 [P] [US5] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeDeb_NoSudo_ReturnsWarning` — set `context.HasSudo = false`, verify result is `Warning` with "Sudo required to install .deb packages"
- [X] T028 [P] [US5] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeDeb_NoDpkg_ReturnsFailed` — mock `which dpkg` to return non-zero exit code, verify result is `Failed` with "dpkg is not available on this system"
- [X] T029 [P] [US5] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeDeb_AssetNotDeb_ReturnsFailed` — configure asset pattern not ending in `.deb`, verify result is `Failed` with "Asset does not appear to be a .deb package"
- [X] T030 [P] [US5] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeDeb_DpkgInstallFails_ReturnsFailed` — mock `dpkg -i` to fail, verify result is `Failed` with "dpkg installation failed" message including stderr
- [X] T031 [P] [US5] Write test in `tests/Dottie.Configuration.Tests/Installing/GithubReleaseInstallerTests.cs`: `InstallSingleItem_TypeDeb_DependencyResolutionFails_ReturnsFailed` — mock `dpkg -i` to succeed but `apt-get install -f` to fail, verify result is `Failed` with "Dependency resolution failed" message
- [X] T032 [P] [US5] Write test in `tests/Dottie.Configuration.Tests/Validation/InstallBlockValidatorTests.cs`: `ValidateGithubRelease_TypeBinary_NoBinaryField_ReturnsError` — verify existing validation error for `type: binary` without `binary` field is preserved
- [X] T032a [P] [US5] Write test in `tests/Dottie.Configuration.Tests/Parsing/ConfigurationLoaderTests.cs`: `LoadConfiguration_UnrecognizedGithubType_ReturnsUserFriendlyError` — provide YAML with `type: foo`, verify the error message is "Unsupported asset type: foo" (not a raw YamlDotNet deserialization exception)

### Implementation for User Story 5

- [X] T033 [US5] Add sudo check for deb type in `GithubReleaseInstaller` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs`: at the start of the deb dispatch path, check `context.HasSudo`; if false, return `InstallResult.Warning()` with "Sudo required to install .deb packages" — check before any download
- [X] T034 [US5] Add dpkg availability check in `GithubReleaseInstaller` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs`: after sudo check, run `which dpkg` via `IProcessRunner`; if exit code is non-zero, return `InstallResult.Failed()` with "dpkg is not available on this system"
- [X] T035 [US5] Add .deb asset validation in `GithubReleaseInstaller` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs`: after asset matching, validate the matched asset name ends with `.deb`; if not, return `InstallResult.Failed()` with "Asset does not appear to be a .deb package"
- [X] T036 [US5] Add error handling for `dpkg -i` failure in `InstallDebPackageAsync()` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs`: if `dpkg -i` returns non-zero, return `InstallResult.Failed()` with "dpkg installation failed: {stderr}"
- [X] T037 [US5] Add error handling for `apt-get install -f` failure in `InstallDebPackageAsync()` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs`: if `apt-get install -f -y` returns non-zero after dpkg succeeds, return `InstallResult.Failed()` with "Dependency resolution failed: {stderr}"
- [X] T037a [US5] Add error wrapping for unrecognized `type` values in `src/Dottie.Configuration/Parsing/ConfigurationLoader.cs` (or `ConfigurationValidator`): catch YamlDotNet enum deserialization failures for the `Type` field and transform into user-friendly error "Unsupported asset type: \<value\>"
- [X] T038 [US5] Verify all T027–T032a tests pass; verify all previous phase tests still pass

**Checkpoint**: All error conditions produce clear, actionable messages. No regressions.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Cleanup, documentation, and final validation across all stories

- [X] T039 [P] Ensure temp file cleanup works in all error paths — review `InstallDebPackageAsync()` in `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs` for proper `try/finally` coverage, add a test for cleanup-on-failure if not already covered
- [X] T040 [P] Verify profile inheritance works with `type` field — add a test in `tests/Dottie.Configuration.Tests/Inheritance/ProfileMergerTests.cs` that a child profile can override a parent's GitHub entry including changing the `type` value
- [X] T041 Run full test suite: `dotnet test` for all unit tests + `.\tests\run-integration-tests.ps1` for integration tests; verify zero failures
- [X] T042 Run build with `dotnet build --warnaserror` — verify no analyzer warnings or code standards violations
- [X] T043 Run quickstart.md validation — manually verify the configuration examples from `specs/012-github-release-type/quickstart.md` are accurate against the implementation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phases 3–7 (User Stories)**: All depend on Phase 2 completion
  - US1 (Phase 3) and US2 (Phase 4) are both P1 — US2 validates backward compat of US1 changes
  - US3 (Phase 5) and US4 (Phase 6) are both P2 — can run in parallel after US1
  - US5 (Phase 7) is P3 — can run in parallel with US3/US4 but error handling tasks depend on the basic deb path existing (T012–T013)
- **Phase 8 (Polish)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational (Phase 2) only — creates the deb install path
- **US2 (P1)**: Depends on Foundational (Phase 2) — validates backward compat; should run after US1 to verify no regressions
- **US3 (P2)**: Depends on US1 (needs deb install path to add idempotency to)
- **US4 (P2)**: Depends on US1 (needs deb dispatch to add dry-run branch to)
- **US5 (P3)**: Depends on US1 (needs deb dispatch to add error guards to); sudo/dpkg checks are prepended before the install logic

### Within Each User Story

1. Tests MUST be written and FAIL before implementation
2. Implementation follows test guidance
3. Verify all tests (new + existing) pass before moving to next story

### Parallel Opportunities

Within **Phase 1**: T001 and T002 can run in parallel (different files)
Within **Phase 2 tests**: T003, T004, T005 can run in parallel (different test files)
Within **Phase 3 tests**: T009, T010, T011 can run in parallel (same file, but independent test methods)
Within **Phase 5–6**: US3 and US4 can run in parallel (different code paths in the same installer)
Within **Phase 7 tests**: T027–T032 can all run in parallel (independent test methods)
Within **Phase 8**: T039 and T040 can run in parallel (different test files)

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests together (write first, must fail):
Task T009: "InstallSingleItem_TypeDeb_DownloadsAndInstallsViaDpkg"
Task T010: "InstallSingleItem_TypeDeb_WithVersion_DownloadsSpecificRelease"
Task T011: "InstallSingleItem_TypeDeb_VariableSubstitution_ResolvesAssetPattern"

# Then implement sequentially:
Task T012: "Add InstallDebPackageAsync() method"
Task T013: "Add dispatch logic in InstallSingleItemAsync()"
Task T014: "Verify all tests pass"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T002)
2. Complete Phase 2: Foundational (T003–T008)
3. Complete Phase 3: User Story 1 (T009–T014)
4. **STOP and VALIDATE**: `type: deb` installs .deb packages; all existing tests pass
5. This is a shippable MVP — users can install .deb packages from GitHub releases

### Incremental Delivery

1. Setup + Foundational → Model and validation ready
2. Add US1 → deb installation works → **MVP!**
3. Add US2 → backward compatibility confirmed
4. Add US3 → idempotency for deb type
5. Add US4 → dry-run for deb type
6. Add US5 → all error handling
7. Polish → cleanup, docs, final validation
8. Each story adds value without breaking previous stories

### Suggested Commit Points

- After Phase 2: "feat: add GithubReleaseAssetType enum and conditional binary validation"
- After Phase 3: "feat: implement deb package installation from GitHub releases"
- After Phase 4: "test: verify backward compatibility for type field default"
- After Phase 5: "feat: add idempotency check for deb package installation"
- After Phase 6: "feat: add dry-run support for deb package installation"
- After Phase 7: "feat: add error handling for deb installation prerequisites"
- After Phase 8: "chore: polish and cross-cutting validation"

---

## Notes

- [P] tasks = different files, no dependencies — safe to parallelize
- [Story] label maps task to specific user story for traceability
- Constitution requires TDD: all tests written and failing before any production code
- All tasks reference exact file paths from plan.md project structure
- Commit after each phase or logical checkpoint
- Stop at any checkpoint to validate independently
