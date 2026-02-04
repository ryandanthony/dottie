# Feature Specification: CLI Command `apply`

**Feature Branch**: `009-cli-apply`  
**Created**: February 3, 2026  
**Status**: Draft  
**Input**: User description: "Apply everything: create symlinks and install all software for a selected profile. Command: dottie apply with --profile, --force, and --dry-run flags."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Apply Full Profile Configuration (Priority: P1)

As a user setting up a new machine, I want to run a single command that applies my entire dotfile configuration and installs all software, so that I can quickly replicate my development environment without running multiple commands.

**Why this priority**: This is the core value proposition of the `apply` command - a single unified command that performs both linking and installation. Without this, users must run `dottie link` and `dottie install` separately.

**Independent Test**: Can be fully tested by running `dottie apply` on a fresh system with a complete configuration file and verifying all symlinks are created and software is installed.

**Acceptance Scenarios**:

1. **Given** a valid configuration with dotfiles and install blocks for the default profile, **When** the user runs `dottie apply`, **Then** all dotfiles are symlinked to their destinations AND all software is installed in the correct priority order.
2. **Given** a valid configuration file, **When** the user runs `dottie apply --profile work`, **Then** the specified profile is resolved (including inherited profiles via `extends`) and applied.
3. **Given** the apply command completes, **When** inspecting the system, **Then** the dotfiles are linked first, followed by software installations in priority order: GitHub Releases → APT Packages → Private APT Repositories → Shell Scripts → Fonts → Snap Packages.

---

### User Story 2 - Preview Changes with Dry Run (Priority: P2)

As a cautious user, I want to preview what the apply command will do before actually making changes, so that I can verify the operations are correct and avoid unintended modifications.

**Why this priority**: The dry-run capability is critical for user confidence and safety. It allows users to verify their configuration without risking their system, especially on production or shared machines.

**Independent Test**: Can be fully tested by running `dottie apply --dry-run` and verifying no filesystem changes occur while a complete plan is displayed.

**Acceptance Scenarios**:

1. **Given** a valid configuration, **When** the user runs `dottie apply --dry-run`, **Then** a complete list of operations is displayed showing what would happen.
2. **Given** a dry-run is executed, **When** checking the filesystem, **Then** no symlinks are created, no files are modified, and no software is installed.
3. **Given** a dry-run is executed, **When** viewing the output, **Then** the user sees the resolved profile, dotfile links that would be created, and software that would be installed in order.

---

### User Story 3 - Force Apply with Conflict Resolution (Priority: P3)

As a user with existing files that conflict with my dotfile configuration, I want to force the apply command to overwrite conflicts while safely backing up existing files, so that I can apply my configuration without manual cleanup.

**Why this priority**: Users may have existing configurations or partial setups. The force option enables them to proceed while the backup ensures they can recover if needed.

**Independent Test**: Can be fully tested by creating conflicting files, running `dottie apply --force`, and verifying conflicts are backed up and replaced.

**Acceptance Scenarios**:

1. **Given** an existing file at a dotfile destination path, **When** the user runs `dottie apply --force`, **Then** the existing file is backed up and the symlink is created.
2. **Given** conflicts exist and `--force` is NOT provided, **When** the user runs `dottie apply`, **Then** the command fails with a clear error message listing the conflicts.
3. **Given** `--force` is used and backups are created, **When** the apply completes, **Then** the user is informed where backup files are located.

---

### User Story 4 - Apply with Profile Inheritance (Priority: P3)

As a user with a modular profile setup, I want the apply command to correctly resolve profile inheritance chains, so that my base configurations are included when applying specialized profiles.

**Why this priority**: Profile inheritance allows users to build modular, reusable configurations. This is important for maintaining DRY configurations across multiple machines or contexts.

**Independent Test**: Can be fully tested by creating a profile that extends another and running `dottie apply --profile child`, verifying both parent and child configurations are applied.

**Acceptance Scenarios**:

1. **Given** profile "work" extends profile "default", **When** the user runs `dottie apply --profile work`, **Then** dotfiles and software from both "default" and "work" profiles are applied.
2. **Given** a multi-level inheritance chain (e.g., "gaming" extends "personal" extends "default"), **When** applying the "gaming" profile, **Then** all ancestor profile configurations are included.
3. **Given** conflicting entries between parent and child profiles, **When** applying the child profile, **Then** child profile settings take precedence.

---

### Edge Cases

- What happens when the configuration file is missing or invalid? → Command fails with a descriptive error before any operations.
- What happens when a profile specified via `--profile` doesn't exist? → Command fails with an error listing available profiles.
- What happens when network is unavailable during software installation? → Installation fails gracefully with clear error, already-completed operations remain.
- What happens when an installation step fails partway through? → Command continues with remaining items (fail-soft), collects all failures, and reports a summary at the end listing what succeeded and what failed.
- What happens when running apply on an already-applied configuration? → Idempotent behavior - existing symlinks are verified, already-installed software is skipped.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an `apply` command that combines the functionality of `link` and `install` commands.
- **FR-002**: System MUST support a `--profile <name>` flag to specify which profile to apply (default: `default`).
- **FR-003**: System MUST support a `--force` flag that backs up conflicting files before overwriting.
- **FR-004**: System MUST support a `--dry-run` flag that shows planned operations without executing them.
- **FR-005**: System MUST resolve profile inheritance (via `extends`) before applying configuration.
- **FR-006**: System MUST execute dotfile linking before software installation.
- **FR-007**: System MUST install software sources in priority order: GitHub Releases → APT Packages → Private APT Repositories → Shell Scripts → Fonts → Snap Packages.
- **FR-008**: System MUST fail safely on conflicts when `--force` is not provided, listing all conflicting paths.
- **FR-009**: System MUST display progress information during apply operations.
- **FR-010**: System MUST report a verbose summary upon completion showing every operation performed with its status (success/failure/skipped).
- **FR-011**: System MUST behave idempotently - running apply multiple times produces the same result.
- **FR-012**: System MUST NOT modify the filesystem or install software when `--dry-run` is specified.

### Key Entities

- **Profile**: A named configuration section containing dotfile entries and install blocks; may inherit from other profiles via `extends`.
- **Dotfile Entry**: A source-destination pair defining a symlink to create.
- **Install Block**: A collection of software to install from various sources (GitHub releases, APT packages, shell scripts, fonts, snaps).
- **Conflict**: A situation where a destination path already exists and is not managed by dottie.
- **Backup**: A copy of a conflicting file preserved before overwriting (when `--force` is used).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can set up a complete development environment (dotfiles + software) using a single command.
- **SC-002**: Users can preview all operations before execution using dry-run mode.
- **SC-003**: Users can safely apply configurations to machines with existing files by using the force option with automatic backups.
- **SC-004**: Apply operations complete with clear progress indication and summary reporting.
- **SC-005**: Running apply multiple times on the same configuration produces consistent results (idempotent behavior).
- **SC-006**: Users with complex profile hierarchies see correct inheritance resolution when applying child profiles.

## Assumptions

- The `link` and `install` command implementations are already available and can be reused or orchestrated by the `apply` command.
- Profile inheritance resolution logic exists and can be leveraged by the apply command.
- Backup functionality from the `link` command can be reused for conflict resolution.
- Installation priority ordering is fixed as specified and does not need to be user-configurable.

## Clarifications

### Session 2026-02-03

- Q: When a software installation step fails partway through, what should happen to the overall operation? → A: Continue with remaining items and report all failures at the end (fail-soft behavior).
- Q: What level of detail should the summary report provide upon completion? → A: Verbose - full list of every operation performed with status.
