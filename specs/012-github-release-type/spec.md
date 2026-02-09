# Feature Specification: GitHub Release Asset Type

**Feature Branch**: `012-github-release-type`  
**Created**: 2026-02-08  
**Status**: Draft  
**Input**: User description: "Add a type field to GitHub release items that controls how downloaded assets are installed, starting with deb package support"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Install a .deb Package from a GitHub Release (Priority: P1)

A user wants to install software that is published as a `.deb` package on GitHub Releases (e.g., Draw.io Desktop). They add a GitHub release entry to their dotfile configuration with `type: deb`, specifying the repository and asset pattern. When they run the install command, the system downloads the `.deb` asset and installs it using the system package manager, automatically resolving any missing dependencies.

**Why this priority**: This is the core value of the feature — enabling a new installation pathway that is currently impossible. Without this, users must fall back to manual shell scripts, losing version tracking, idempotency, and dry-run capabilities.

**Independent Test**: Can be fully tested by configuring a `type: deb` entry for a real GitHub release (e.g., `jgraph/drawio-desktop`), running the install command, and verifying the package is installed on the system.

**Acceptance Scenarios**:

1. **Given** a configuration with `type: deb` and a valid repo/asset pattern, **When** the user runs the install command, **Then** the `.deb` asset is downloaded from the matching GitHub release and installed via the system package manager.
2. **Given** a configuration with `type: deb` and a version specified, **When** the user runs the install command, **Then** the system downloads and installs the `.deb` from that specific release version.
3. **Given** a configuration with `type: deb` that includes variable substitutions in the asset pattern, **When** the user runs the install command, **Then** variables are resolved before matching the asset.

---

### User Story 2 - Backward-Compatible Default Behavior for Existing Configurations (Priority: P1)

A user has an existing dotfile configuration with GitHub release entries that do not include a `type` field. When they upgrade to the new version, all existing configurations continue to work identically — assets are installed as standalone binaries or extracted from archives, exactly as before.

**Why this priority**: Equal priority with Story 1 because breaking existing configurations would be a critical regression. Zero-impact backward compatibility is non-negotiable.

**Independent Test**: Can be tested by running existing configurations (without `type` field) and verifying identical behavior to the previous version.

**Acceptance Scenarios**:

1. **Given** a configuration entry without a `type` field, **When** the user runs the install command, **Then** the system treats it as `type: binary` and follows the existing binary installation flow.
2. **Given** a configuration entry with `type: binary` explicitly set, **When** the user runs the install command, **Then** the behavior is identical to omitting the `type` field.

---

### User Story 3 - Skip Already-Installed .deb Packages (Priority: P2)

A user runs the install command a second time. For entries with `type: deb`, the system checks whether the package is already installed on the system and skips re-installation if it is, providing an idempotent experience consistent with how binary-type entries behave today.

**Why this priority**: Idempotency is a core design principle of the tool. Without it, repeated runs would redundantly download and reinstall packages, wasting time and potentially requiring unnecessary privilege escalation.

**Independent Test**: Can be tested by installing a `.deb` package, running the install command again, and verifying the system reports it as already installed without re-downloading or re-installing.

**Acceptance Scenarios**:

1. **Given** a `type: deb` entry whose package is already installed, **When** the user runs the install command, **Then** the system detects the package is installed and skips the download/install step.
2. **Given** a `type: deb` entry whose package is not yet installed, **When** the user runs the install command, **Then** the system proceeds with download and installation.

---

### User Story 4 - Dry Run Preview for .deb Installation (Priority: P2)

A user runs the install command with the `--dry-run` flag. For entries with `type: deb`, the system validates that the GitHub release and matching asset exist but does not download or install anything. It reports what would happen if run for real.

**Why this priority**: Dry-run support is essential for users to preview changes safely before committing to installation, especially for actions requiring elevated privileges.

**Independent Test**: Can be tested by running the install command with `--dry-run` on a `type: deb` entry and verifying the output describes the intended action without performing it.

**Acceptance Scenarios**:

1. **Given** a `type: deb` entry and the `--dry-run` flag, **When** the user runs the install command, **Then** the system reports that the package would be installed via the package manager without downloading or installing anything.
2. **Given** a `type: deb` entry referencing a non-existent release and the `--dry-run` flag, **When** the user runs the install command, **Then** the system reports a validation error.

---

### User Story 5 - Clear Errors When Conditions Are Not Met (Priority: P3)

A user configures a `type: deb` entry but runs on a system without the required package management tools, or without elevated privileges. The system provides clear, actionable error messages explaining why installation cannot proceed.

**Why this priority**: Good error handling improves user trust and debuggability, but is less critical than the core installation and idempotency paths.

**Independent Test**: Can be tested by simulating missing prerequisites (no package manager available, no elevated privileges) and verifying the error messages are clear and specific.

**Acceptance Scenarios**:

1. **Given** a `type: deb` entry on a system where `dpkg` is not available, **When** the user runs the install command, **Then** the system reports an error: "dpkg is not available on this system."
2. **Given** a `type: deb` entry on a system where elevated privileges are not available, **When** the user runs the install command, **Then** the system reports a warning: "Sudo required to install .deb packages."
3. **Given** a `type: deb` entry where the downloaded asset is not a valid `.deb` file, **When** the install command runs, **Then** the system reports an error: "Asset does not appear to be a .deb package."
4. **Given** a configuration with an unrecognized `type` value, **When** the user runs the install command, **Then** the system reports an error: "Unsupported asset type: \<value\>."
5. **Given** a `type: binary` entry without a `binary` field, **When** configuration is validated, **Then** the system reports a validation error (existing behavior preserved).

---

### Edge Cases

- What happens when `type: deb` is specified and a `binary` field is also provided? The `binary` field is ignored — deb installation does not use it.
- What happens when the `.deb` installation succeeds but dependency resolution fails? The system reports the dependency resolution failure with actionable details.
- What happens when the `.deb` asset pattern matches multiple files in a release? The system follows existing asset-matching behavior (first match or error on ambiguity, consistent with binary type).
- What happens when `type: deb` is used with a version that does not publish a `.deb` asset? The system reports that no matching asset was found for the given release.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support an optional `type` field on GitHub release configuration entries, with allowed values of `binary` and `deb`.
- **FR-002**: System MUST default to `binary` behavior when `type` is omitted, preserving full backward compatibility with existing configurations.
- **FR-003**: System MUST install `.deb` assets via the system package manager when `type: deb` is specified, including automatic dependency resolution.
- **FR-004**: System MUST validate that the downloaded asset is a `.deb` file when `type: deb` is specified, and fail with a clear error if it is not.
- **FR-005**: System MUST check whether the package is already installed before attempting re-installation for `type: deb` entries (idempotency).
- **FR-006**: System MUST NOT require the `binary` field when `type: deb` is specified; the `binary` field is only required for `type: binary`.
- **FR-007**: System MUST ignore the `binary` field if it is provided alongside `type: deb`.
- **FR-008**: System MUST report a clear warning when `type: deb` is used without elevated privileges.
- **FR-009**: System MUST report a clear error when `type: deb` is used on a system that does not have `dpkg` available.
- **FR-010**: System MUST support dry-run mode for `type: deb` entries, validating the release and asset exist without downloading or installing.
- **FR-011**: System MUST report a clear error for unrecognized `type` values.
- **FR-012**: System MUST support variable substitution in the `asset` field for `type: deb` entries, consistent with existing variable support.
- **FR-013**: System MUST clean up any temporary files after `.deb` installation completes, whether successful or failed.

### Key Entities

- **GitHub Release Item**: A configuration entry describing a software asset to install from a GitHub release. Key attributes: repository, asset pattern, type (binary or deb), version, and optionally binary name.
- **Asset Type**: An enumeration controlling the installation pathway for a downloaded asset. Currently supports `binary` (default) and `deb`. Designed to be extended with future types (rpm, appimage, etc.).

### Assumptions

- The system already supports downloading assets from GitHub releases, archive extraction, binary installation to `~/bin/`, and idempotency checks for binaries.
- The system already has a `HasSudo` context flag and a pattern for handling sudo-required operations (used by the apt repository installer).
- Variable substitution in asset patterns is an existing capability that will work with the new `type` field without changes.
- The `.deb` package name needed for idempotency checks can be derived from the downloaded file (standard `.deb` metadata inspection).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can install `.deb` packages from GitHub releases using a single configuration entry, without resorting to shell scripts.
- **SC-002**: 100% of existing configurations (without a `type` field) continue to work identically after the feature is added.
- **SC-003**: Repeated runs with `type: deb` entries skip already-installed packages, completing in under 5 seconds for cached/installed items.
- **SC-004**: Dry-run mode for `type: deb` entries reports intended actions without downloading or installing anything.
- **SC-005**: All error conditions (missing dpkg, no sudo, invalid asset, unknown type) produce user-friendly messages that describe both the problem and what to do about it.
- **SC-006**: The `type` field design accommodates future expansion to additional asset types without requiring changes to existing configuration entries or breaking the user-facing contract.
