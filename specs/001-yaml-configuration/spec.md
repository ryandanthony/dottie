# Feature Specification: YAML Configuration System

**Feature Branch**: `001-yaml-configuration`  
**Created**: January 28, 2026  
**Status**: Draft  
**Input**: User description: "YAML configuration system for dottie - profiles, dotfiles mapping, install blocks, and inheritance"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Define Basic Dotfiles Configuration (Priority: P1)

As a user, I want to create a simple `dottie.yaml` file that maps my dotfiles from the repository to their target locations in my home directory, so that I can manage my shell and editor configurations in one place.

**Why this priority**: This is the core functionality - without the ability to define dotfile mappings, the tool has no value. Every other feature depends on this foundational capability.

**Independent Test**: Can be fully tested by creating a minimal `dottie.yaml` with one dotfile mapping and verifying the configuration is parsed correctly without errors.

**Acceptance Scenarios**:

1. **Given** a repository with a `dottie.yaml` file containing a `default` profile with one dotfile mapping, **When** the configuration is loaded, **Then** the system recognizes the source and target paths for the dotfile.
2. **Given** a `dottie.yaml` file with multiple dotfile entries, **When** the configuration is loaded, **Then** all source-to-target mappings are available in the correct order.
3. **Given** a `dottie.yaml` file with a dotfile entry missing the `source` field, **When** the configuration is loaded, **Then** the system reports a clear validation error indicating the missing field and its location.

---

### User Story 2 - Configure Package Installation Sources (Priority: P2)

As a user, I want to define packages to install from various sources (apt, snap, GitHub releases, custom scripts), so that I can automate my entire development environment setup alongside my dotfiles.

**Why this priority**: Installation automation is the second most valuable capability after dotfile management. It transforms dottie from a simple symlink tool into a complete environment provisioning solution.

**Independent Test**: Can be fully tested by creating a configuration with one `apt` package entry and verifying the package list is parsed correctly.

**Acceptance Scenarios**:

1. **Given** a profile with an `apt` install block listing packages, **When** the configuration is loaded, **Then** the system provides the list of apt packages to install.
2. **Given** a profile with a `github` install block specifying a repository, asset pattern, and binary name, **When** the configuration is loaded, **Then** the system provides complete GitHub release download information.
3. **Given** a profile with a `snap` install block, **When** the configuration is loaded, **Then** the system provides snap package names and whether each requires classic confinement.
4. **Given** a profile with an `apt-repo` install block, **When** the configuration is loaded, **Then** the system provides repository name, GPG key URL, repository line, and packages to install.
5. **Given** a profile with a `scripts` install block, **When** the configuration is loaded, **Then** the system provides the list of script paths to execute.
6. **Given** a profile with a `fonts` install block, **When** the configuration is loaded, **Then** the system provides font names and download URLs.

---

### User Story 3 - Use Multiple Profiles (Priority: P3)

As a user, I want to define multiple named profiles (e.g., "default", "work", "minimal") with different configurations, so that I can maintain separate setups for different machines or contexts.

**Why this priority**: Multiple profiles enable flexibility for users with different machines or contexts. However, a single profile is sufficient for many users, making this a nice-to-have enhancement.

**Independent Test**: Can be fully tested by creating a configuration with two independent profiles and verifying each profile's configuration is accessible by name.

**Acceptance Scenarios**:

1. **Given** a `dottie.yaml` with profiles named `default` and `work`, **When** requesting the `work` profile, **Then** the system returns only the configuration defined in the `work` profile.
2. **Given** a `dottie.yaml` with multiple profiles, **When** requesting a profile that doesn't exist, **Then** the system reports a clear error indicating the available profile names.

---

### User Story 4 - Inherit and Override Profile Settings (Priority: P4)

As a user, I want a profile to extend another profile and override specific settings, so that I can avoid duplicating common configuration across similar profiles.

**Why this priority**: Inheritance reduces maintenance burden for users with multiple similar profiles. It's valuable but not essential - users can duplicate configuration if needed.

**Independent Test**: Can be fully tested by creating a `work` profile that extends `default` and verifying the merged configuration includes both inherited and overridden values.

**Acceptance Scenarios**:

1. **Given** a `work` profile with `extends: default`, **When** the `work` profile is loaded, **Then** the system merges the `default` profile's dotfiles and install blocks with `work`'s overrides.
2. **Given** a `work` profile extending `default` with additional apt packages, **When** the `work` profile is loaded, **Then** the merged install block includes packages from both profiles.
3. **Given** a profile that extends a non-existent profile, **When** the configuration is loaded, **Then** the system reports a clear error indicating the missing parent profile.
4. **Given** a circular inheritance (profile A extends B, B extends A), **When** the configuration is loaded, **Then** the system reports a clear error about the circular dependency.

---

### Edge Cases

- What happens when `dottie.yaml` does not exist in the repository root? → Generate starter template (see FR-001a).
- What happens when `dottie.yaml` contains invalid YAML syntax?
- What happens when a dotfile source path references a file that doesn't exist in the repository?
- What happens when a target path uses environment variables or tilde expansion (e.g., `~/.bashrc`)?
- What happens when a script path in the `scripts` block points outside the repository?
- What happens when a GitHub asset pattern matches multiple assets in a release? → Auto-detect architecture and select matching asset (see FR-013a).
- What happens when the configuration file is empty?

## Requirements *(mandatory)*

### Functional Requirements

#### Configuration File

- **FR-001**: System MUST read configuration from a file named `dottie.yaml` located at the repository root.
- **FR-001a**: If `dottie.yaml` does not exist, system MUST generate a starter template file with a `default` profile containing example dotfile mappings, required fields populated, and optional fields included as YAML comments.
- **FR-002**: System MUST parse valid YAML syntax and report clear errors for invalid YAML with line numbers.
- **FR-003**: System MUST validate that the `profiles` key exists at the root level.

#### Profile Structure

- **FR-004**: System MUST support multiple named profiles under the `profiles` key.
- **FR-004a**: System MUST require users to explicitly specify a profile name; there is no implicit default profile. If no profile is specified, system MUST report an error listing available profile names.
- **FR-005**: Each profile MUST support an optional `dotfiles` list and an optional `install` block.
- **FR-006**: System MUST support profile inheritance via the `extends` key, which references another profile by name.
- **FR-007**: When a profile extends another, the system MUST merge configurations as follows: (a) dotfiles lists are appended (child after parent); (b) plain install lists (apt) are appended; (c) keyed install items (github, snap, apt-repo, fonts) are merged by identifier — child entries with the same key override parent entries, new keys are added.
- **FR-008**: System MUST detect and report circular inheritance as a validation error.

#### Dotfiles Configuration

- **FR-009**: Each dotfile entry MUST have a `source` field (path relative to repository root) and a `target` field (destination path).
- **FR-010**: System MUST validate that both `source` and `target` are present for each dotfile entry.
- **FR-011**: System MUST support tilde (`~`) expansion in target paths to represent the user's home directory.

#### Install Blocks

- **FR-012**: System MUST support the following install block types: `github`, `apt`, `apt-repo`, `scripts`, `fonts`, `snap`.
- **FR-013**: For `github` entries, system MUST require: `repo` (owner/name format), `asset` (glob pattern), `binary` (name of binary to extract). Optional: `version` (defaults to latest release).
- **FR-013a**: When a `github` asset pattern matches multiple release assets, system MUST auto-detect the current system architecture (amd64, arm64) and select the asset matching that architecture. If no architecture-specific match is found, system MUST report an error listing the matched assets.
- **FR-014**: For `apt` entries, system MUST accept a list of package names.
- **FR-015**: For `apt-repo` entries, system MUST require: `name`, `key_url`, `repo` (repository line), `packages` (list of packages to install from this repo).
- **FR-016**: For `scripts` entries, system MUST accept a list of script paths relative to the repository root.
- **FR-017**: For `fonts` entries, system MUST require: `name`, `url` (download URL for font archive).
- **FR-018**: For `snap` entries, system MUST require: `name`. Optional: `classic` (boolean, defaults to false).

#### Security Constraints

- **FR-019**: System MUST validate that all script paths in the `scripts` block reference files within the repository (no absolute paths, no parent directory traversal outside repo).

#### Validation & Error Reporting

- **FR-020**: System MUST provide clear, actionable error messages that include the location (line/path) of the problem in the configuration file.
- **FR-021**: System MUST validate the complete configuration structure before any operations are performed.

### Key Entities

- **Configuration**: The root object representing the entire `dottie.yaml` file; contains one or more profiles.
- **Profile**: A named configuration set containing dotfile mappings and install specifications; may extend another profile.
- **Dotfile Entry**: A mapping from a source path (in the repository) to a target path (on the filesystem); defines one file or directory to be linked.
- **Install Block**: A categorized collection of software to install; organized by installation method (apt, snap, github, etc.).
- **GitHub Release Item**: Specification for downloading a binary from a GitHub release; includes repository, asset pattern, binary name, and optional version.
- **Apt Repository Item**: Specification for adding a third-party apt repository; includes GPG key URL, repository line, and packages.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can define a working dotfiles configuration in under 5 minutes by following documentation examples.
- **SC-002**: Configuration validation catches 100% of structural errors (missing required fields, invalid types) before any file system operations occur.
- **SC-003**: Error messages enable users to locate and fix configuration problems without external help in 90% of cases.
- **SC-004**: A configuration with 50 dotfile entries and 6 install block types loads and validates in under 2 seconds.
- **SC-005**: Profile inheritance correctly merges configurations with no unexpected data loss or duplication.

## Assumptions

- Users have basic familiarity with YAML syntax.
- The `dottie.yaml` file will be version-controlled alongside the dotfiles it references.
- Target paths will typically be in the user's home directory or subdirectories thereof.
- The repository structure is stable (source paths won't frequently change).
- Environment variable expansion (beyond tilde) is not required in the initial implementation.
- **Target OS**: Linux only (Ubuntu/Debian-based distributions) for initial implementation. Install block types (apt, snap, apt-repo) assume Linux package managers.

## Clarifications

### Session 2026-01-28

- Q: How should install block lists be merged when a child profile extends a parent? → A: Deep merge by key — keyed items (github repos, snap names) merge by identifier; plain lists (apt) append.
- Q: What should happen when `dottie.yaml` does not exist? → A: Create a starter template with non-required fields commented out.
- Q: Which operating systems should dottie support? → A: Linux only (Ubuntu/Debian-based) for initial scope.
- Q: What happens when a GitHub asset pattern matches multiple assets? → A: Auto-detect system architecture (amd64, arm64) and select matching asset.
- Q: What profile should be used when none is specified? → A: Always require explicit profile name (no default).
