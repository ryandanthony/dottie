# Feature Specification: CLI Status Command

**Feature Branch**: `010-cli-status`  
**Created**: February 3, 2026  
**Status**: Draft  
**Input**: User description: "CLI Command `dottie status [--profile <name>]` - Report current state of dotfiles (linked/missing/conflicting) and software (installed/missing/outdated)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Dotfile Link Status (Priority: P1)

As a user managing dotfiles, I want to see at a glance which dotfiles are properly linked, which are missing, and which have conflicts, so I can quickly identify and resolve issues with my configuration.

**Why this priority**: This is the core value of the status command - users need to know the current state of their dotfile links before taking any corrective action. Without this, users would need to manually check each symlink.

**Independent Test**: Can be fully tested by creating a test configuration with mixed link states and verifying the status output correctly categorizes each dotfile.

**Acceptance Scenarios**:

1. **Given** a configuration with properly linked dotfiles, **When** user runs `dottie status`, **Then** the command displays all linked dotfiles with a "linked" indicator
2. **Given** a configuration with dotfiles that have not been linked yet, **When** user runs `dottie status`, **Then** the command displays those dotfiles with a "missing" indicator
3. **Given** a configuration with dotfiles that conflict with existing files, **When** user runs `dottie status`, **Then** the command displays those dotfiles with a "conflicting" indicator showing the conflicting path

---

### User Story 2 - View Software Installation Status (Priority: P2)

As a user with a software installation configuration, I want to see which software packages are installed, missing, or outdated, so I can understand what needs to be installed or updated on my system.

**Why this priority**: Software installation status complements dotfile status and provides a complete picture of system configuration state. It's secondary because dotfile management is the primary use case.

**Independent Test**: Can be fully tested by configuring software packages with known states (installed, not installed) and verifying the status output correctly identifies each.

**Acceptance Scenarios**:

1. **Given** a configuration with software that is currently installed, **When** user runs `dottie status`, **Then** the command displays those items with an "installed" indicator
2. **Given** a configuration with software that is not installed, **When** user runs `dottie status`, **Then** the command displays those items with a "missing" indicator
3. **Given** a configuration with software that has a pinned version and an older version is installed, **When** user runs `dottie status`, **Then** the command displays those items with an "outdated" indicator showing current and target versions

---

### User Story 3 - Status for Specific Profile (Priority: P2)

As a user with multiple profiles defined, I want to check the status of a specific profile, so I can understand the state of configuration for different environments or machines.

**Why this priority**: Profile filtering is essential for users managing multiple configurations but is only useful after the core status display works correctly.

**Independent Test**: Can be fully tested by creating multiple profiles and verifying `dottie status --profile <name>` only shows items from that profile (including inherited profiles).

**Acceptance Scenarios**:

1. **Given** multiple profiles are defined in configuration, **When** user runs `dottie status --profile work`, **Then** only dotfiles and software from the "work" profile (and its inherited profiles) are displayed
2. **Given** a profile inherits from another profile, **When** user runs `dottie status --profile child`, **Then** the status includes items from both the child profile and all parent profiles
3. **Given** user specifies a non-existent profile, **When** user runs `dottie status --profile nonexistent`, **Then** the command displays an appropriate error message

---

### User Story 4 - Default Profile Resolution (Priority: P3)

As a user who typically uses one profile, I want the status command to automatically use my default profile when no profile is specified, so I don't need to type the profile name every time.

**Why this priority**: This is a convenience feature that improves user experience but is not essential for core functionality.

**Independent Test**: Can be fully tested by setting a default profile and running `dottie status` without arguments.

**Acceptance Scenarios**:

1. **Given** a default profile is configured, **When** user runs `dottie status` without `--profile` flag, **Then** the command displays status for the default profile
2. **Given** no default profile is configured and multiple profiles exist, **When** user runs `dottie status` without `--profile` flag, **Then** the command prompts user to specify a profile or shows all profiles

---

### Edge Cases

- What happens when the configuration file does not exist or is invalid?
- How does the system handle symlinks that point to non-existent targets (broken links)?
- What happens when a dotfile source file no longer exists but is defined in configuration?
- How does the system handle when software version detection fails (e.g., command not in PATH)?
- What happens when the user does not have permission to read certain target directories?
- How does the system handle circular profile inheritance?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display the status of all dotfile entries showing linked, missing, broken, or conflicting states
- **FR-002**: System MUST display the status of all software installation items showing installed, missing, or outdated states
- **FR-003**: System MUST resolve profile inheritance before computing status (child profile includes parent items)
- **FR-004**: System MUST support `--profile <name>` option to filter status to a specific profile
- **FR-005**: System MUST use the default profile when no profile is specified and a default is configured
- **FR-006**: System MUST display human-readable output formatted for terminal viewing
- **FR-007**: System MUST indicate outdated software when version information is available and a version mismatch exists
- **FR-008**: System MUST display an error message when a specified profile does not exist
- **FR-009**: System MUST gracefully handle configuration files that cannot be loaded
- **FR-010**: System MUST report symlinks pointing to non-existent source files as "broken" (distinct from missing or conflicting)
- **FR-011**: System MUST display a friendly "no items configured" message when a section (dotfiles or software) has no entries
- **FR-012**: System MUST organize output by category (Dotfiles section first, then Software section)
- **FR-013**: System MUST display entries with "unknown" state and brief error reason when status cannot be determined due to permission or access errors
- **FR-014**: System MUST return exit code 0 when the command executes successfully, regardless of item states (status is informational)

### Key Entities

- **Dotfile Entry**: Represents a single dotfile configuration with source path, target path, and current link state (linked/missing/broken/conflicting/unknown)
- **Software Item**: Represents a software package with name, optional version constraint, and current installation state (installed/missing/outdated)
- **Profile**: A named configuration scope that can inherit from other profiles and contains dotfile entries and software items
- **Status Report**: The aggregated result containing categorized lists of dotfiles and software with their respective states

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can assess their complete dotfile and software configuration state within 5 seconds of running the command
- **SC-002**: Status output correctly identifies 100% of linked, missing, and conflicting dotfiles
- **SC-003**: Status output correctly identifies 100% of installed and missing software items
- **SC-004**: Users can filter status by profile and receive accurate results including inherited profile items
- **SC-005**: The command provides clear, actionable information that enables users to determine their next steps (link, install, resolve conflicts)

## Assumptions

- The existing profile resolution and inheritance system from previous features is available and functioning
- Dotfile linking follows the existing symlink conventions established in the `link` command
- Software installation status detection leverages existing install source implementations (APT, GitHub releases, fonts, etc.)
- Version detection for "outdated" status is only available for software sources that support version queries (e.g., apt packages with version info)
- The configuration file format and loader from previous features is available

## Clarifications

### Session 2026-02-03

- Q: How should broken symlinks (pointing to non-existent source) be categorized? → A: Report as distinct "broken" state (separate from missing/conflicting)
- Q: What should be displayed when a profile has no dotfiles or software configured? → A: Display a friendly "no items configured" message per section
- Q: How should the status output be organized? → A: Group by category first (Dotfiles section, then Software section)
- Q: How should permission errors be handled when checking dotfile targets? → A: Show entry with "unknown" state and brief error reason
- Q: What exit code should the status command return when problems exist? → A: Always return 0 if command runs successfully (status is informational only)
