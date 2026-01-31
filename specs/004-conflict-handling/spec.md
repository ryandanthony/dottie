# Feature Specification: Conflict Handling for Dotfile Linking

**Feature Branch**: `004-conflict-handling`  
**Created**: January 30, 2026  
**Status**: Draft  
**Input**: User description: "Conflict handling for dotfile linking - fail with error on conflicts, support --force flag for backup and overwrite"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Safe Conflict Detection (Priority: P1)

As a user running `dottie link` or `dottie apply`, when a target file already exists and is not a symlink pointing to my dotfiles repository, I want the operation to fail safely with a clear error message showing which files conflict, so that I don't accidentally lose my existing configurations.

**Why this priority**: Data safety is paramount. Users must never lose their existing configuration files without explicit consent. This is the core protective behavior that prevents data loss.

**Independent Test**: Can be fully tested by attempting to link dotfiles where target files already exist, verifying the operation fails and lists all conflicting files without modifying anything.

**Acceptance Scenarios**:

1. **Given** a dotfile entry configured to link `~/.bashrc`, **When** `~/.bashrc` already exists as a regular file, **Then** the operation fails with an error message identifying `~/.bashrc` as a conflicting file
2. **Given** multiple dotfile entries where 3 target files already exist, **When** running `dottie link`, **Then** the operation fails and displays all 3 conflicting files in the error output
3. **Given** a target file that is already a symlink pointing to the correct source in the repo, **When** running `dottie link`, **Then** no conflict is reported and the operation succeeds

---

### User Story 2 - Force Link with Automatic Backup (Priority: P2)

As a user who wants to overwrite existing files with my managed dotfiles, I want to use a `--force` flag that automatically backs up conflicting files before creating symlinks, so that I can proceed with linking while preserving my original files for recovery.

**Why this priority**: After safe detection (P1), users need a way to proceed when conflicts exist. Automatic backup ensures the force operation remains safe while enabling workflow progress.

**Independent Test**: Can be fully tested by running `dottie link --force` with existing target files, verifying backups are created and symlinks are established.

**Acceptance Scenarios**:

1. **Given** a regular file exists at `~/.bashrc`, **When** running `dottie link --force`, **Then** the existing file is renamed to `~/.bashrc.backup.<timestamp>` and a symlink is created at `~/.bashrc`
2. **Given** multiple conflicting files exist, **When** running `dottie link --force`, **Then** each conflicting file is backed up with a unique timestamp and all symlinks are created
3. **Given** a backup operation succeeds, **When** the user checks the backup file, **Then** the backup contains the exact original content and the timestamp format is consistent (e.g., `YYYYMMDD-HHMMSS`)

---

### User Story 3 - Conflict Reporting in Apply Command (Priority: P3)

As a user running `dottie apply` (which includes linking as part of its workflow), I want the same conflict handling behavior to apply to the linking portion, so that I have consistent and safe behavior regardless of which command I use.

**Why this priority**: Ensures consistent user experience across commands. Once link conflict handling works, apply should inherit the same behavior for its linking operations.

**Independent Test**: Can be fully tested by running `dottie apply` with existing target files and verifying the same conflict detection and `--force` behavior as `dottie link`.

**Acceptance Scenarios**:

1. **Given** a dotfile entry with a conflicting target file, **When** running `dottie apply`, **Then** the operation fails with the same conflict error as `dottie link`
2. **Given** conflicting files exist, **When** running `dottie apply --force`, **Then** backups are created and symlinks established the same way as `dottie link --force`

---

### Edge Cases

- What happens when the backup filename already exists? (Create a unique backup name to avoid overwriting previous backups)
- What happens when the conflicting path is a directory instead of a file? (Back up the entire directory with the same naming convention)
- What happens when the user lacks write permission to create the backup? (Fail with a clear permission error before attempting any modifications)
- What happens when disk space is insufficient for the backup? (Fail gracefully and report the issue without partial modifications)
- What happens when the target is a symlink pointing to a different location (not the repo)? (Treat as a conflict requiring backup)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST detect conflicts when a target path exists and is not already a symlink to the expected source in the dotfiles repository
- **FR-002**: System MUST fail the operation and display all conflicting file paths when conflicts are detected without the `--force` flag; output MUST be a structured list showing each conflict's target path and type (file, directory, or mismatched symlink) on separate lines
- **FR-003**: System MUST NOT modify any files when conflicts are detected without the `--force` flag
- **FR-004**: System MUST support a `--force` flag on `dottie link` command to enable backup-and-overwrite behavior
- **FR-005**: System MUST support a `--force` flag on `dottie apply` command with the same behavior for its linking operations
- **FR-006**: When `--force` is specified, system MUST create a backup of each conflicting file before creating the symlink
- **FR-006a**: After successful `--force` operation, system MUST display per-file output showing each original path and its corresponding backup path
- **FR-007**: Backup files MUST be named using the pattern `<original-filename>.backup.<timestamp>` where timestamp is in `YYYYMMDD-HHMMSS` format; if a backup with that name already exists, append a numeric suffix (`.1`, `.2`, etc.) to ensure uniqueness
- **FR-008**: Backup files MUST be created in the same directory as the original file
- **FR-009**: System MUST handle directory conflicts by backing up the entire directory using the same naming convention
- **FR-010**: System MUST treat symlinks pointing to locations other than the expected repo source as conflicts
- **FR-011**: System MUST skip (no conflict) when the target is already a symlink pointing to the correct source
- **FR-012**: System MUST ensure backup creation succeeds before removing/replacing the original file
- **FR-013**: System MUST report clear error messages when backup operations fail (permission denied, disk full, etc.)

### Key Entities

- **Conflict**: A situation where the target path for a dotfile entry exists and is not the expected symlink; includes the source path, target path, and type (file, directory, or mismatched symlink)
- **Backup**: A preserved copy of a conflicting file or directory; includes the original path, backup path with timestamp, and creation timestamp

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can identify all conflicting files in a single command run without needing to run multiple times
- **SC-002**: Zero data loss occurs when running link operations without `--force` flag on systems with existing configurations
- **SC-003**: 100% of conflicting files have recoverable backups when using `--force` flag
- **SC-004**: Users can restore original configurations from backups without requiring external tools or documentation
- **SC-005**: Conflict detection and backup operations complete within 5 seconds for typical dotfile configurations (up to 50 files)
- **SC-006**: Error messages clearly identify the conflicting path and required action for resolution

## Clarifications

### Session 2026-01-30

- Q: When using `--force`, should the system process all conflicts automatically, or should users have an option for more granular control? → A: All-or-nothing approach - `--force` processes all conflicts automatically with no per-file prompting
- Q: How should conflicting files be displayed to the user? → A: Structured list showing path + conflict type (file/directory/mismatched symlink) per line
- Q: After `--force` successfully backs up and links files, what information should be shown to the user? → A: Per-file detail listing each original path → backup path created
- Q: When a backup file with the same timestamp already exists, how should the system resolve this? → A: Append numeric suffix (e.g., `.backup.20260130-143022.1`, `.backup.20260130-143022.2`)

## Assumptions

- Timestamps use local system time for backup naming
- Backup files remain in place indefinitely (no automatic cleanup)
- The `--force` flag applies to all conflicts in a single run (no per-file prompting) — **confirmed via clarification**
- File permissions of original files are not explicitly preserved in backups (standard copy behavior)
- Ownership of backup files follows standard file creation rules (current user)
