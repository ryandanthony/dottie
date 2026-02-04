# Apply Basic Test Scenario

## Purpose

Tests that the `dottie apply` command successfully:
1. Creates symlinks for dotfiles
2. Installs software packages
3. Does both in a single command (links before installs)

## Configuration

- **Dotfiles**: bashrc and vimrc are linked to home directory
- **Install**: APT package `jq` is installed

## Expected Behavior

1. `dottie apply` should succeed with exit code 0
2. Symlinks `~/.bashrc-apply-test` and `~/.vimrc-apply-test` should be created
3. Symlinks should point to the source files in the repo
4. Install phase should run (may fail in container without apt privileges)

## Validation Steps

1. Verify configuration validates
2. Run `dottie apply`
3. Verify symlinks exist and point to correct targets
4. Verify command output shows both link and install phases
