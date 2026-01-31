# Feature Specification: CI/CD Build Pipeline

**Feature Branch**: `002-ci-build`  
**Created**: January 28, 2026  
**Status**: Draft  
**Input**: User description: "Build out the CI/build pipeline for this project using GitHub Actions with GitVersion for semantic versioning, code coverage requirements, and automated releases"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automated Build Verification on Pull Request (Priority: P1)

As a developer, I want my pull requests to be automatically built and tested so that I can ensure my changes don't break the codebase before merging.

**Why this priority**: This is the foundation of CI - without automated builds on PRs, code quality cannot be verified before merging. This prevents broken code from reaching the main branch.

**Independent Test**: Can be fully tested by opening a pull request and verifying that the build runs, tests execute, and status checks are reported back to the PR.

**Acceptance Scenarios**:

1. **Given** a pull request is opened against the main branch, **When** the PR is created or updated with new commits, **Then** the CI pipeline automatically triggers and runs build and tests.

2. **Given** a pull request build completes, **When** all tests pass and coverage meets the threshold, **Then** the PR status check shows as successful and ready for merge.

3. **Given** a pull request build completes, **When** any test fails or coverage is below threshold, **Then** the PR status check shows as failed with details about the failure.

4. **Given** a pull request build completes successfully, **When** the build artifacts are generated, **Then** downloadable artifacts (linux-x64, win-x64 binaries) are available in the workflow run.

---

### User Story 2 - Automated Release on Main Branch (Priority: P2)

As a maintainer, I want successful builds on the main branch to automatically create versioned releases so that users always have access to the latest stable binaries without manual intervention.

**Why this priority**: Automating releases ensures consistent delivery and removes human error from the release process. This depends on P1 being in place to ensure only passing builds are released.

**Independent Test**: Can be fully tested by merging a PR to main and verifying that a GitHub Release is created with the correct version tag and downloadable binaries.

**Acceptance Scenarios**:

1. **Given** a commit is pushed to the main branch, **When** the build and all tests pass, **Then** a git tag is created with the calculated semantic version.

2. **Given** a successful main branch build creates a tag, **When** the tag is created, **Then** a GitHub Release is automatically created with that version.

3. **Given** a GitHub Release is created, **When** the release is published, **Then** platform-specific binaries (dottie-linux-x64, dottie-win-x64.exe) are attached as downloadable assets.

4. **Given** a GitHub Release is created, **When** viewing the release notes, **Then** the notes contain auto-generated content from merged PRs/commits since the last release.

---

### User Story 3 - Semantic Version Calculation (Priority: P3)

As a developer, I want the version number to be automatically calculated based on commit history so that versioning follows semantic versioning conventions without manual tracking.

**Why this priority**: Consistent semantic versioning communicates the impact of changes to users. This builds on P1/P2 to provide meaningful version numbers.

**Independent Test**: Can be fully tested by making commits with version bump keywords and verifying the calculated version changes appropriately.

**Acceptance Scenarios**:

1. **Given** a commit message contains no version keywords, **When** the version is calculated, **Then** the patch version is incremented (e.g., 0.1.0 → 0.1.1).

2. **Given** a commit message contains `+semver: minor` or `+semver: feature`, **When** the version is calculated, **Then** the minor version is incremented and patch resets (e.g., 0.1.1 → 0.2.0).

3. **Given** a commit message contains `+semver: major` or `+semver: breaking`, **When** the version is calculated, **Then** the major version is incremented and minor/patch reset (e.g., 0.2.0 → 1.0.0).

4. **Given** the repository has no previous version tags, **When** the first build runs, **Then** the version starts at 0.1.0.

---

### User Story 4 - Code Coverage Enforcement (Priority: P4)

As a maintainer, I want code coverage to be measured and enforced so that the codebase maintains a minimum quality standard.

**Why this priority**: Coverage enforcement ensures testing discipline is maintained. Lower priority because the project can function without it, but it improves long-term maintainability.

**Independent Test**: Can be fully tested by running a build and verifying coverage is collected, reported, and enforced against the threshold.

**Acceptance Scenarios**:

1. **Given** a build runs, **When** tests complete, **Then** code coverage is collected and a coverage report is generated.

2. **Given** code coverage is below 80%, **When** the build evaluates coverage, **Then** the build fails with a clear message indicating the coverage shortfall.

3. **Given** code coverage meets or exceeds 80%, **When** the build evaluates coverage, **Then** the build passes the coverage check.

4. **Given** a pull request build completes, **When** the coverage report is available, **Then** a coverage summary is posted as a comment on the PR.

---

### Edge Cases

- **Version calculation failure**: If GitVersion cannot calculate a version (e.g., shallow clone), the build MUST fail with a clear error message.
- **Concurrent builds**: The pipeline handles concurrent builds independently; each build calculates its own version based on git state at checkout time.
- **Release creation failure**: If release creation fails after a successful build on main, the entire workflow MUST be marked as failed.
- **Dependency restore failure**: Build MUST fail fast with clear error attribution to the restore step.
- **Coverage collection failure**: If coverage collection fails but tests pass, the build MUST fail (coverage is required).

## Requirements *(mandatory)*

### Functional Requirements

#### Build & Test

- **FR-001**: The pipeline MUST automatically trigger on pull request creation, update, or reopen against the main branch.
- **FR-002**: The pipeline MUST automatically trigger on any push to the main branch.
- **FR-003**: The pipeline MUST restore all dependencies before building.
- **FR-004**: The pipeline MUST compile the solution with the calculated semantic version.
- **FR-005**: The pipeline MUST run all unit tests after a successful build.
- **FR-006**: The pipeline MUST run integration tests (tests/integration/) in a Docker container after unit tests pass, validating dottie CLI behavior on Ubuntu with real configuration scenarios.
- **FR-007**: The pipeline MUST fail fast if any build or test step fails.

#### Code Coverage

- **FR-008**: The pipeline MUST collect code coverage during test execution.
- **FR-009**: The pipeline MUST generate a coverage report in a human-readable format.
- **FR-010**: The pipeline MUST enforce a minimum code coverage threshold of 80%.
- **FR-011**: The pipeline MUST fail the build if coverage drops below the threshold.
- **FR-012**: The pipeline MUST upload the coverage report as a workflow artifact.
- **FR-013**: The pipeline MUST post a coverage summary as a comment on pull requests, deleting any previous coverage comments to show only the latest results.

#### Versioning

- **FR-014**: The pipeline MUST calculate semantic version from git history using mainline mode.
- **FR-015**: The pipeline MUST clone the repository with full history to enable version calculation.
- **FR-016**: The pipeline MUST increment patch version by default when no version keyword is present.
- **FR-017**: The pipeline MUST increment minor version when commit message contains `+semver: minor` or `+semver: feature`.
- **FR-018**: The pipeline MUST increment major version when commit message contains `+semver: major` or `+semver: breaking`.
- **FR-019**: The pipeline MUST start versioning at 0.1.0 for repositories with no prior version tags.

#### Artifacts & Publishing

- **FR-020**: The pipeline MUST produce self-contained, single-file, trimmed executables.
- **FR-021**: The pipeline MUST publish artifacts for linux-x64 platform.
- **FR-022**: The pipeline MUST publish artifacts for win-x64 platform.
- **FR-023**: The pipeline MUST upload artifacts to the workflow run for download with a 7-day retention period.

#### Release (Main Branch Only)

- **FR-024**: The pipeline MUST create a git tag with format `v{major}.{minor}.{patch}` on successful main branch builds.
- **FR-025**: The pipeline MUST create a GitHub Release when a version tag is created.
- **FR-026**: The pipeline MUST name releases with the version only (e.g., `v1.2.3`).
- **FR-027**: The pipeline MUST attach platform-specific binaries to the release as downloadable assets.
- **FR-028**: The release MUST include auto-generated release notes from merged PRs and commits.

#### Performance

- **FR-029**: The pipeline MUST cache dependencies to improve restore performance.

#### Branch Protection

- **FR-030**: A script MUST be provided to configure branch protection rules for the main branch (this is NOT run as part of the pipeline)
- **FR-031**: The script MUST require CI status checks to pass before merging.
- **FR-032**: The script MUST require branches to be up-to-date before merging.
- **FR-033**: The script MUST be idempotent (safe to run multiple times).

### Key Entities

- **Build Run**: A single execution of the CI pipeline, identified by run number and commit SHA.
- **Version**: A semantic version (major.minor.patch) calculated from git history.
- **Tag**: A git tag marking a specific version in the repository history.
- **Release**: A GitHub Release with version-specific downloadable artifacts.
- **Artifact**: A build output (binary) for a specific platform.
- **Coverage Report**: A document showing code coverage metrics from test execution.
- **Branch Protection Script**: A utility script to configure GitHub branch protection rules via the GitHub API.

## Clarifications

### Session 2026-01-28

- Q: What should happen when the release creation fails after a successful build on main? → A: Fail the entire workflow as failed (even though build/tests passed)
- Q: Should integration tests run in a separate step with potential external dependencies, or are they simply slower tests in the same test run? → A: Separate step using Docker for cross-platform consistency (works on Windows and Linux)
- Q: How should multiple coverage comments be handled on the same PR? → A: Delete old coverage comments and post fresh (only latest visible)
- Q: How long should build artifacts be retained for PR builds? → A: 7 days
- Q: Should the spec include requirements for branch protection rules? → A: In scope - provide a script to establish branch protection in GitHub

## Assumptions

- The repository uses git as the version control system.
- The CI/CD platform is available and enabled for the repository.
- The SDK version is pinned in the repository root for CI consistency.
- The solution file and test projects follow standard conventions.
- Sufficient CI/CD minutes are available for the workflow runs.
- The repository has appropriate permissions to create tags and releases.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All pull requests receive build status feedback within 10 minutes of creation or update.
- **SC-002**: Code coverage remains at or above 80% across all builds.
- **SC-003**: Every successful main branch build produces a tagged release within 5 minutes of build completion.
- **SC-004**: Developers can download platform-specific binaries from any PR build or release.
- **SC-005**: Version numbers follow semantic versioning and increment correctly based on commit messages.
- **SC-006**: Zero manual steps required for the release process after code is merged to main.
- **SC-007**: Build failures are clearly attributed to the specific failing step (build, test, coverage, publish).
