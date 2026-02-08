# Feature Specification: OS Release Variable Substitution

**Feature Branch**: `011-os-release-variables`  
**Created**: 2026-02-07  
**Status**: Draft  
**Input**: User description: "Add /etc/os-release variable substitution support across install sources (aptrepo, dotfiles, github)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Portable APT Repository Configuration (Priority: P1)

A user maintains a single dottie configuration that targets multiple Ubuntu/Debian versions and CPU architectures. They define APT repository entries using `${VERSION_CODENAME}` and `${MS_ARCH}` variables so the same configuration file works on Ubuntu Jammy (x86_64), Ubuntu Noble (aarch64), and Debian Bookworm (x86_64) without any manual edits.

**Why this priority**: This is the core use case driving the feature. APT repository URLs are the most common place where OS version and architecture differences create portability problems. Solving this unlocks single-configuration management across machines.

**Independent Test**: Can be fully tested by running an install with an aptrepo entry containing `${VERSION_CODENAME}` and `${MS_ARCH}` on a system with `/etc/os-release` present, and verifying the expanded repository URL matches the actual OS codename and architecture.

**Acceptance Scenarios**:

1. **Given** a configuration with `repo: "deb [arch=${MS_ARCH}] https://example.com/${VERSION_CODENAME}/prod ${VERSION_CODENAME} main"` on an Ubuntu Noble x86_64 system, **When** the install command runs, **Then** the repository URL expands to `deb [arch=amd64] https://example.com/noble/prod noble main`.
2. **Given** the same configuration on an Ubuntu Jammy aarch64 system, **When** the install command runs, **Then** the repository URL expands to `deb [arch=arm64] https://example.com/jammy/prod jammy main`.
3. **Given** a configuration with aptrepo entries that contain no `${...}` variables, **When** the install command runs, **Then** the entries are processed exactly as before with no changes to behavior.

---

### User Story 2 - Architecture-Aware GitHub Release Downloads (Priority: P2)

A user configures GitHub release downloads using `${ARCH}` or `${MS_ARCH}` variables in the asset pattern so the correct binary is automatically selected for their system architecture, and `${RELEASE_VERSION}` so asset names that embed the version number are matched dynamically.

**Why this priority**: GitHub release assets commonly include architecture and version in filenames. Without variable substitution, users must hardcode architecture strings or maintain separate configurations per machine.

**Independent Test**: Can be fully tested by running an install with a GitHub release entry whose asset pattern contains `${MS_ARCH}` and `${RELEASE_VERSION}`, and verifying the resolved asset name matches the expected architecture and version.

**Acceptance Scenarios**:

1. **Given** a configuration with `asset: "tool-${RELEASE_VERSION}-linux-${MS_ARCH}.tar.gz"` and `version: "1.2.0"` on an x86_64 system, **When** the install command runs, **Then** the asset resolves to `tool-1.2.0-linux-amd64.tar.gz`.
2. **Given** a configuration with `asset: "tool-${RELEASE_VERSION}-linux-${MS_ARCH}.tar.gz"` and no explicit version on an aarch64 system, **When** the install command runs, **Then** `${RELEASE_VERSION}` resolves to the latest release tag from the GitHub API and `${MS_ARCH}` resolves to `arm64`.
3. **Given** a configuration with `asset: "tool-*-linux-${ARCH}.tar.gz"` on an x86_64 system, **When** the install command runs, **Then** `${ARCH}` resolves to `x86_64` (raw architecture string).

---

### User Story 3 - Platform-Specific Dotfile Sources (Priority: P3)

A user uses variables in dotfile source paths to select OS- or architecture-specific configuration files, allowing a single dotfile entry to resolve to different source files depending on the host system.

**Why this priority**: While less common than aptrepo and GitHub use cases, some users maintain platform-variant scripts or config files. This extends variable substitution to the dotfile source for completeness.

**Independent Test**: Can be fully tested by creating a dotfile entry with `${VERSION_CODENAME}` in the source path, placing a matching file in the dotfiles directory, and verifying the correct file is linked.

**Acceptance Scenarios**:

1. **Given** a configuration with `source: "dotfiles/setup-${VERSION_CODENAME}.sh"` on an Ubuntu Noble system, **When** the install command runs, **Then** the source resolves to `dotfiles/setup-noble.sh`.
2. **Given** a configuration with `source: "dotfiles/tool-${ARCH}.conf"` on an aarch64 system, **When** the install command runs, **Then** the source resolves to `dotfiles/tool-aarch64.conf`.

---

### User Story 4 - Graceful Handling of Missing OS Information (Priority: P4)

A user runs dottie on a minimal or non-standard Linux system where `/etc/os-release` does not exist. The system warns the user about the missing file but continues processing entries that do not depend on OS release variables, and fails clearly for entries that reference unavailable variables.

**Why this priority**: Robustness and clear error reporting are important for user trust, but this is an uncommon edge case compared to the primary happy-path scenarios.

**Independent Test**: Can be fully tested by running dottie on a system without `/etc/os-release` (or with the file removed) and verifying warning output and correct failure behavior.

**Acceptance Scenarios**:

1. **Given** a system where `/etc/os-release` does not exist and a configuration has no `${...}` OS release variables, **When** the install command runs, **Then** the system warns about the missing file but all entries process successfully.
2. **Given** a system where `/etc/os-release` does not exist and a configuration references `${VERSION_CODENAME}`, **When** the install command runs, **Then** the system reports a clear error identifying the unresolvable variable and the affected entry.

---

### Edge Cases

- What happens when `/etc/os-release` contains unexpected or non-standard keys?
  - Only keys present in the file are available; unrecognized keys are still loaded and usable as variables.
- What happens when a variable like `${VERSION_CODENAME}` is referenced but the OS release file does not define that key (e.g., some minimal distributions)?
  - The system treats it as an unresolvable variable and reports an error for the affected entry.
- What happens when the system architecture is not in the known mapping table for `${MS_ARCH}` (e.g., `riscv64`)?
  - The system reports an error and fails the affected entry, since the Microsoft-style mapping is undefined.
- What happens when a configuration contains a literal `${...}` string that is not intended as a variable (e.g., a shell variable in a script)?
  - Any `${...}` pattern is treated as a variable reference. Users should avoid this pattern in literal strings or escape it if an escape mechanism is supported.
- What happens when the GitHub API is unreachable and `${RELEASE_VERSION}` needs to be resolved from the latest release?
  - The system fails the affected entry with a clear error indicating the API could not be reached.
- What happens when `${RELEASE_VERSION}` is used outside of a `github` install source?
  - The variable is not available and treated as unresolvable, producing an error for the affected entry.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST parse the `/etc/os-release` file and make all key-value pairs available as substitution variables in the format `${KEY_NAME}`.
- **FR-002**: System MUST provide an `${ARCH}` variable containing the raw system architecture string (e.g., `x86_64`, `aarch64`, `armv7l`).
- **FR-003**: System MUST provide an `${MS_ARCH}` variable that maps the raw architecture to Microsoft-style naming: `x86_64` → `amd64`, `aarch64` → `arm64`, `armv7l` → `armhf`.
- **FR-004**: System MUST fail with a clear error when the system architecture is not in the known `${MS_ARCH}` mapping and `${MS_ARCH}` is referenced.
- **FR-005**: System MUST perform variable substitution on `aptrepo` entry fields: `repo`, `key_url`, and `packages`.
- **FR-006**: System MUST perform variable substitution on `dotfiles` entry fields: `source` and `target`.
- **FR-007**: System MUST perform variable substitution on `github` entry fields: `asset` and `binary`.
- **FR-008**: System MUST provide a `${RELEASE_VERSION}` variable for `github` entries that resolves to the explicitly specified version or the latest release tag from the GitHub API.
- **FR-009**: System MUST process entries without any `${...}` patterns identically to current behavior (full backwards compatibility).
- **FR-010**: System MUST warn the user when `/etc/os-release` is not found but continue processing entries that do not reference OS release variables.
- **FR-011**: System MUST report a clear error identifying the specific unresolvable variable and the affected configuration entry when a referenced variable cannot be resolved.
- **FR-012**: System MUST resolve all variables before performing any installation actions (variable resolution happens during configuration loading, not during execution).

### Key Entities

- **Variable Source**: A provider of substitution variables (OS release file, architecture detection, GitHub release API). Each source has a set of key-value pairs it contributes.
- **Variable Reference**: A `${KEY_NAME}` pattern found in a configuration field value. It has a name and a location (which entry and field it appears in).
- **Architecture Mapping**: A lookup table that translates raw architecture identifiers to Microsoft-style identifiers. Contains source architecture, target identifier, and whether the mapping is known/supported.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A single configuration file with variable references works correctly across at least 3 different OS version and architecture combinations without modification.
- **SC-002**: Users can set up a new machine's APT repositories, GitHub tools, and dotfiles using a shared configuration in under 5 minutes, regardless of OS version or architecture.
- **SC-003**: All existing configurations without variables continue to work with zero changes (100% backwards compatibility).
- **SC-004**: When a variable cannot be resolved, the error message identifies the variable name, the configuration entry, and the field — enabling the user to fix the issue without guessing.
- **SC-005**: Variable substitution adds no perceptible delay to configuration processing (under 1 second additional time for reading OS information and resolving variables).

## Assumptions

- The target systems are Linux-based and use `/etc/os-release` as the standard OS identification file (per the freedesktop.org specification).
- The `uname -m` command is available and returns the system's CPU architecture.
- The three architecture mappings (`x86_64` → `amd64`, `aarch64` → `arm64`, `armv7l` → `armhf`) cover the vast majority of user systems. Additional mappings can be added in future iterations.
- `${RELEASE_VERSION}` is scoped to `github` install source entries only and is not available in `aptrepo` or `dotfiles` contexts.
- Variable substitution uses the `${...}` syntax exclusively. No escape mechanism is provided in this iteration; users should avoid literal `${...}` patterns in configuration values.
