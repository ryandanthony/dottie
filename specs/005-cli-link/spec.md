# Feature Specification: CLI Link Command

**Feature Branch**: `005-cli-link`  
**Created**: January 31, 2026  
**Status**: Draft  
**Input**: User description: "CLI Command link - Create symlinks for dotfiles only with profile, force, and dry-run options"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic Dotfile Linking (Priority: P1)

As a user setting up a new machine, I want to run a single command to create symlinks for all my dotfiles from my dotfiles repository to their expected locations in my home directory, so that my configuration is immediately active without manual copying.

**Why this priority**: This is the core functionality of the link command. Without basic linking, the command provides no value.

**Independent Test**: Can be fully tested by running `dottie link` with a valid configuration containing dotfile mappings and verifying symlinks are created at target locations pointing to source files.

**Acceptance Scenarios**:

1. **Given** a valid dottie configuration with dotfile mappings and source files exist in the repository, **When** user runs `dottie link`, **Then** symlinks are created at each target location pointing to the corresponding source file
2. **Given** a valid dottie configuration with dotfile mappings, **When** user runs `dottie link` and a target symlink already points to the correct source, **Then** no action is taken for that mapping (idempotent operation)
3. **Given** a valid dottie configuration with dotfile mappings, **When** user runs `dottie link` and the target's parent directory does not exist, **Then** the parent directory is created before creating the symlink

---

### User Story 2 - Profile-Specific Linking (Priority: P2)

As a user with different configurations for different machines (work laptop vs personal desktop), I want to specify which profile to use when linking, so that I only link the dotfiles relevant to my current environment.

**Why this priority**: Profile support enables users to manage multiple configurations, a common use case for dotfile management across different machines.

**Independent Test**: Can be fully tested by running `dottie link --profile work` with a configuration containing multiple profiles and verifying only the specified profile's dotfiles are linked.

**Acceptance Scenarios**:

1. **Given** a configuration with multiple profiles including a profile named "work", **When** user runs `dottie link --profile work`, **Then** only dotfile mappings defined in the "work" profile are processed
2. **Given** no profile flag is provided, **When** user runs `dottie link`, **Then** the "default" profile is used
3. **Given** user specifies a profile name that does not exist, **When** user runs `dottie link --profile nonexistent`, **Then** an error message is displayed indicating the profile was not found

---

### User Story 3 - Preview Changes with Dry Run (Priority: P2)

As a user who wants to understand what changes will be made before committing, I want to preview the linking operations without actually creating symlinks, so that I can verify the expected behavior and catch potential issues.

**Why this priority**: Dry run provides safety and transparency, allowing users to validate operations before execution. Essential for building user trust.

**Independent Test**: Can be fully tested by running `dottie link --dry-run` and verifying no filesystem changes occur while a summary of planned operations is displayed.

**Acceptance Scenarios**:

1. **Given** a valid configuration with dotfile mappings, **When** user runs `dottie link --dry-run`, **Then** no symlinks are created and no files are modified
2. **Given** a valid configuration with dotfile mappings, **When** user runs `dottie link --dry-run`, **Then** a summary of all planned linking operations is displayed showing source and target paths
3. **Given** a configuration with conflicts (existing files at target locations), **When** user runs `dottie link --dry-run`, **Then** the output indicates which targets have conflicts and what would happen to them

---

### User Story 4 - Force Overwrite with Backup (Priority: P3)

As a user who wants to replace existing configuration files with my managed dotfiles, I want to force overwrite existing files while keeping backups, so that I can adopt my dotfile management without losing my current configuration.

**Why this priority**: Force mode handles the common scenario of adopting dotfile management on an existing system. Lower priority because basic linking must work first.

**Independent Test**: Can be fully tested by running `dottie link --force` with existing files at target locations and verifying backups are created before symlinks replace the files.

**Acceptance Scenarios**:

1. **Given** a target location contains an existing file (not a symlink), **When** user runs `dottie link --force`, **Then** the existing file is backed up and replaced with a symlink to the source
2. **Given** a target location contains an existing symlink pointing to a different location, **When** user runs `dottie link --force`, **Then** the existing symlink is backed up and replaced with the correct symlink
3. **Given** `--force` is not specified and a target location contains an existing file, **When** user runs `dottie link`, **Then** a conflict error is reported and the existing file is not modified

---

### Edge Cases

- What happens when a source file specified in the configuration does not exist? → Error is reported for that mapping, other mappings continue to be processed
- What happens when user lacks write permissions to create the symlink at the target location? → Error is reported with a clear message about permission denial
- What happens when a circular symlink situation is detected? → Error is reported indicating the circular reference
- What happens when the target path traverses outside the user's home directory? → Operation proceeds (no restriction on target paths, user is trusted)
- What happens when the configuration file itself is malformed? → Error is reported before any linking operations begin
- How are directory symlinks handled? → Directories are supported as both source and target; a single symlink is created to the directory (not individual file symlinks within)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST read dotfile mappings from the dottie configuration file
- **FR-002**: System MUST validate that each source path exists before attempting to create a symlink
- **FR-003**: System MUST create parent directories for target paths if they do not exist
- **FR-004**: System MUST create symlinks at target locations pointing to source locations for each dotfile mapping
- **FR-005**: System MUST skip symlink creation when target already points to the correct source (idempotent)
- **FR-006**: System MUST report conflicts when target exists and is not the correct symlink (without --force flag)
- **FR-007**: System MUST use the "default" profile when no --profile flag is specified
- **FR-008**: System MUST validate that the specified profile exists in the configuration
- **FR-009**: System MUST support the --dry-run flag to preview operations without making filesystem changes
- **FR-010**: System MUST display planned operations when --dry-run is specified
- **FR-011**: System MUST support the --force flag to overwrite existing files/symlinks
- **FR-012**: System MUST create backups of existing files before overwriting when --force is used
- **FR-013**: System MUST support directory paths as both source and target for symlink creation
- **FR-014**: System MUST expand path variables (such as ~ for home directory) in target paths
- **FR-015**: System MUST report clear error messages for each type of failure (missing source, permission denied, invalid configuration)
- **FR-016**: System MUST continue processing remaining mappings when individual mappings fail (unless configuration is invalid)
- **FR-017**: System MUST exit with non-zero exit code if any mapping operation fails
- **FR-018**: System MUST display a progress bar during linking operations
- **FR-019**: System MUST display a summary of results (count of linked, skipped, failed) upon completion
- **FR-020**: System MUST use symbolic links only (no junctions or hardlinks as fallback)
- **FR-021**: On Windows, when symlink creation fails due to insufficient permissions, system MUST display an error message explaining the user can either run as Administrator or enable Developer Mode
- **FR-022**: When creating backups with --force, system MUST store backup in the same directory as the original file with a timestamp suffix (format: .dottie-backup-YYYYMMDD-HHMMSS)
- **FR-023**: The --dry-run and --force flags MUST be mutually exclusive; system MUST display an error if both are specified

### Key Entities

- **Dotfile Mapping**: A source-to-target relationship defining where a configuration file lives in the repository (source) and where it should be linked in the filesystem (target)
- **Profile**: A named collection of dotfile mappings that can be selectively applied based on user choice or machine context
- **Backup**: A copy of an existing file made before it is overwritten by a symlink, preserving the user's original configuration

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can link all dotfiles for a profile with a single command execution
- **SC-002**: 100% of link operations are idempotent - running the command multiple times produces the same result without errors
- **SC-003**: Users can preview all planned changes before execution using dry-run mode
- **SC-004**: Zero data loss when using force mode - all overwritten files have corresponding backups
- **SC-005**: Users receive actionable error messages for all failure scenarios within 1 second of the failure occurring
- **SC-006**: Link command completes within 5 seconds for configurations with up to 100 dotfile mappings

## Clarifications

### Session 2026-01-31

- Q: When some mappings succeed while others fail, what should be the command exit code? → A: Exit non-zero (failure) if any mapping failed
- Q: What level of output verbosity should dottie link display by default? → A: Summary only (count of linked/skipped/failed) with a progress bar during execution
- Q: On Windows, how should dottie link handle symlink creation when permissions are insufficient? → A: Symlinks only - fail with clear error message explaining how to enable symlinks (run as Administrator or enable Developer Mode)
- Q: When --force creates a backup of an existing file, where should the backup be stored? → A: Same directory with timestamp suffix (e.g., .bashrc.dottie-backup-20260131-143052)
- Q: Should --dry-run and --force be allowed to be used together? → A: No - mutually exclusive flags, error if both specified

## Assumptions

- Users have appropriate filesystem permissions to create symlinks in target directories
- The dottie configuration file format and location are defined by other specifications (FEATURE-01)
- Profile inheritance and resolution logic is defined by other specifications (FEATURE-02)
- Conflict handling behavior (backup location, naming convention) is defined by other specifications (FEATURE-04)
- Source paths are relative to the dotfiles repository root
- Target paths support ~ expansion for home directory
