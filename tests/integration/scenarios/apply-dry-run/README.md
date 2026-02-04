# Apply Dry-Run Test Scenario

## Purpose

Tests that the `dottie apply --dry-run` command:
1. Shows what would be done (links and installs)
2. Does NOT create any symlinks
3. Does NOT install any software
4. Returns success (exit code 0)

## Configuration

- **Dotfiles**: bashrc and vimrc would be linked
- **Install**: APT package `jq` would be installed

## Expected Behavior

1. `dottie apply --dry-run` should succeed with exit code 0
2. No symlinks should be created
3. No packages should be installed
4. Output should show "Would link" or similar preview text
5. Output should mention "Dry Run" or "Preview"

## Validation Steps

1. Verify configuration validates
2. Run `dottie apply --dry-run`
3. Verify NO symlinks exist after command
4. Verify output indicates dry-run mode
