# Apply Force Test Scenario

## Purpose

Tests that the `dottie apply --force` command:
1. Backs up conflicting files before overwriting
2. Successfully creates symlinks after backup
3. Returns success (exit code 0)

## Configuration

- **Dotfiles**: bashrc is linked to home directory

## Test Setup

1. Create a conflicting file at the target location BEFORE running apply
2. Run `dottie apply --force`
3. Verify backup was created
4. Verify symlink was created

## Expected Behavior

1. Pre-existing file at target should be backed up (e.g., to `.bashrc-force-test.bak`)
2. Symlink should be created at target location
3. Command should succeed with exit code 0
4. Without `--force`, the command should fail due to conflict

## Validation Steps

1. Verify configuration validates
2. Create conflicting file at target location
3. Verify `dottie apply` (without --force) fails or warns about conflict
4. Run `dottie apply --force`
5. Verify backup file exists
6. Verify symlink exists and points to source
