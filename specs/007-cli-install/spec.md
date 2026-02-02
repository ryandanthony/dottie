# Feature Specification: CLI Command `install`

**Feature Branch**: `007-cli-install`  
**Created**: February 1, 2026  
**Status**: Draft  
**Input**: User description: "CLI command 'install' to install software only (no dotfile linking). Supports --profile flag for profile selection and --dry-run flag to preview changes. Should resolve profile inheritance and execute install sources in priority order while avoiding re-installation of already-present tools."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic Software Installation (Priority: P1)

As a user setting up a new machine, I want to run a single command to install all software tools defined in my dottie configuration, so that I can quickly provision my development environment without manually installing each tool.

**Why this priority**: This is the core functionality of the install command. Without basic installation capability, the command provides no value.

**Independent Test**: Can be fully tested by running `dottie install` with a valid configuration containing install sources and verifying that all defined software is installed on the system.

**Acceptance Scenarios**:

1. **Given** a valid dottie configuration with install sources defined, **When** user runs `dottie install`, **Then** all software from all installation sources is installed in priority order (GitHub releases → APT packages → APT repositories → Scripts → Fonts → Snap packages)
2. **Given** a valid dottie configuration with install sources, **When** user runs `dottie install` and a tool is already installed, **Then** that tool is skipped and marked as "already installed" in the output
3. **Given** a valid dottie configuration with install sources, **When** user runs `dottie install`, **Then** a progress indicator shows the current installation status for each source type

---

### User Story 2 - Profile-Specific Installation (Priority: P2)

As a user with different software requirements for different machines (work laptop vs personal desktop), I want to specify which profile to use when installing, so that I only install the software relevant to my current environment.

**Why this priority**: Profile support enables users to manage different toolsets for different machines, a common use case for development environment management.

**Independent Test**: Can be fully tested by running `dottie install --profile work` with a configuration containing multiple profiles and verifying only the specified profile's software is installed.

**Acceptance Scenarios**:

1. **Given** a configuration with multiple profiles including a profile named "work", **When** user runs `dottie install --profile work`, **Then** only install sources defined in the "work" profile (including inherited sources) are processed
2. **Given** a profile that inherits from another profile, **When** user runs `dottie install --profile child`, **Then** install sources from both the child profile and all parent profiles are installed
3. **Given** no profile flag is provided, **When** user runs `dottie install`, **Then** the "default" profile is used
4. **Given** user specifies a profile name that does not exist, **When** user runs `dottie install --profile nonexistent`, **Then** an error message is displayed indicating the profile was not found

---

### User Story 3 - Preview Changes with Dry Run (Priority: P2)

As a user who wants to understand what software will be installed before committing, I want to preview the installation operations without actually installing anything, so that I can verify the expected behavior and review the software list.

**Why this priority**: Dry run provides safety and transparency, allowing users to validate operations before execution. Essential for building user trust and avoiding unwanted software installation.

**Independent Test**: Can be fully tested by running `dottie install --dry-run` and verifying no software is installed while a summary of planned installations is displayed.

**Acceptance Scenarios**:

1. **Given** a valid configuration with install sources, **When** user runs `dottie install --dry-run`, **Then** no software is installed and no system changes are made
2. **Given** a valid configuration with install sources, **When** user runs `dottie install --dry-run`, **Then** the system checks which tools are already installed and displays a summary showing source type, package/tool name, version (if applicable), and whether each would be installed or skipped
3. **Given** a configuration with some tools already installed, **When** user runs `dottie install --dry-run`, **Then** the output indicates which tools would be skipped as already installed

---

### User Story 4 - Skip Already Installed Tools (Priority: P1)

As a user who has some tools already installed on my machine, I want the install command to detect and skip tools that are already present, so that I don't waste time or cause conflicts by re-installing existing software.

**Why this priority**: Idempotency is critical for a reliable installation tool. Users expect to run the command multiple times safely without side effects.

**Independent Test**: Can be fully tested by running `dottie install` on a machine with some tools pre-installed and verifying those tools are skipped with appropriate messages.

**Acceptance Scenarios**:

1. **Given** a configuration that includes a tool already installed on the system, **When** user runs `dottie install`, **Then** that tool is skipped and the output indicates "already installed"
2. **Given** multiple tools to install where some are present and some are not, **When** user runs `dottie install`, **Then** only missing tools are installed and already-present tools are skipped
3. **Given** a tool installed at a different version than specified in configuration, **When** user runs `dottie install`, **Then** the tool is skipped (version matching is not enforced for presence detection)

---

### Edge Cases

- What happens when a GitHub release asset pattern matches no files? → Error is reported for that item, other installations continue
- What happens when a GitHub release has a pinned version that doesn't exist? → Error is reported indicating the version was not found; no fallback to latest (pinned versions are explicit user choices)
- What happens when GitHub API rate limit is exceeded? → Error is reported with suggestion to set GITHUB_TOKEN environment variable for higher limits
- What happens when the user lacks sudo permissions for APT package installation? → Error is reported indicating sudo is required, installation continues for non-sudo sources
- What happens when network connectivity is unavailable for downloads? → Error is reported with clear message about connectivity, offline installations are skipped
- What happens when an installation script exits with non-zero code? → Error is reported for that script, other installations continue
- What happens when a font archive contains unexpected structure? → Error is reported, other fonts continue to be processed
- What happens when configuration specifies duplicate packages across different sources? → Both are attempted; second occurrence will be skipped as "already installed"
- What happens when disk space is insufficient? → Error is reported by the underlying package manager, message is surfaced to user
- How are installation failures in middle of process handled? → Failures are collected and reported at the end; process continues with remaining installations

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST read install sources from the dottie configuration file for the resolved profile
- **FR-002**: System MUST resolve profile inheritance before determining which install sources to process
- **FR-003**: System MUST execute install sources in priority order: GitHub releases (1), APT packages (2), APT repositories (3), Scripts (4), Fonts (5), Snap packages (6)
- **FR-004**: System MUST detect already-installed tools and skip them without error
- **FR-005**: System MUST use the "default" profile when no --profile flag is specified
- **FR-006**: System MUST validate that the specified profile exists in the configuration
- **FR-007**: System MUST support the --dry-run flag to preview installations without making system changes
- **FR-008**: System MUST display planned installations when --dry-run is specified, grouped by source type
- **FR-009**: System MUST display progress during installation operations showing current source type and item
- **FR-010**: System MUST display a summary of results upon completion (count of installed, skipped, failed) with all failures grouped together showing source type, item name, and error message
- **FR-011**: System MUST continue processing remaining installations when individual items fail
- **FR-012**: System MUST exit with non-zero exit code if any installation operation fails
- **FR-013**: System MUST report clear error messages for each type of failure (network error, permission denied, missing asset, script failure)
- **FR-014**: System MUST NOT perform any dotfile linking operations (install command is software-only)
- **FR-015**: System MUST create required directories (~/bin/, ~/.local/share/fonts/) if they do not exist
- **FR-016**: System MUST make downloaded binaries executable after extraction
- **FR-017**: System MUST run font cache refresh after installing fonts
- **FR-018**: System MUST verify scripts exist in the repository before attempting execution (no external script URLs)
- **FR-019**: System MUST run only one `apt-get update` per install session (before APT and APT-repo installations)
- **FR-020**: System MUST support cancellation via Ctrl+C, cleaning up any partial installations where possible
- **FR-021**: System MUST support optional `GITHUB_TOKEN` environment variable for authenticated GitHub API requests (higher rate limits)
- **FR-022**: System MUST detect already-installed GitHub release binaries by checking ~/bin/ first, then falling back to PATH lookup if not found in ~/bin/

### Key Entities

- **Install Source**: A configuration item defining software to install, with a type (GitHub, APT, APT-repo, Script, Font, Snap) and source-specific properties
- **Profile**: A named collection of install sources and dotfile mappings that can be selectively applied; supports inheritance from parent profiles
- **Install Result**: The outcome of an individual installation attempt, indicating success, skipped (already installed), or failure with error details

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can install all software for a profile with a single command execution
- **SC-002**: 100% of install operations are idempotent - running the command multiple times produces the same result without errors or re-installations
- **SC-003**: Users can preview all planned installations before execution using dry-run mode
- **SC-004**: Users receive actionable error messages for all failure scenarios within 1 second of the failure occurring
- **SC-005**: Install command processes up to 50 installation items within 5 minutes (excluding actual download/install time)
- **SC-006**: Profile inheritance is correctly resolved with all parent profile install sources included
- **SC-007**: Installation priority order is respected - all GitHub releases complete before APT packages begin, etc.

## Assumptions

- Users have appropriate system permissions for software installation (sudo access for APT/Snap, write access to ~/bin/)
- The dottie configuration file format and location are defined by other specifications (FEATURE-01)
- Profile inheritance and resolution logic is defined by other specifications (FEATURE-03)
- Installation source types and their configuration schema are defined by other specifications (FEATURE-06)
- Network connectivity is available for downloading remote assets (GitHub releases, fonts)
- Target system is Linux-based (Ubuntu/Debian) for APT and Snap package managers
- ~/bin/ is in the user's PATH for binary installations to be usable

## Clarifications

### Session 2026-02-01

- Q: Should --dry-run check which tools are already installed or just list all configured items? → A: Check system state (shows accurate "would install" vs "would skip")
- Q: When a GitHub release has a pinned version that doesn't exist, what should happen? → A: Fail with error (no fallback to latest)
- Q: How should the install command handle GitHub API rate limiting? → A: Support optional GITHUB_TOKEN environment variable for higher rate limits
- Q: How should multiple installation failures be presented in the output? → A: Grouped summary at end (source type, item name, error message)
- Q: How should "already installed" detection work for GitHub release binaries? → A: Check ~/bin/ first, fall back to PATH check if not found
