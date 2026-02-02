# Basic Symlinks Scenario

## Purpose
Test that dottie correctly creates symlinks for dotfiles with the `link` command.

## What is tested
1. **Configuration validation** - Verifies dottie.yml is valid
2. **Dry-run mode** - Tests that `dottie link --dry-run` works without making changes
3. **Symlink creation** - Verifies that `dottie link` actually creates symlinks
4. **Symlink targeting** - Ensures symlinks point to the correct source files
5. **Symlink readability** - Verifies created symlinks are readable
6. **Profile resolution** - Tests that the default profile is properly resolved

## Expected behavior
1. `dottie validate --config dottie.yml` should pass
2. `dottie link --dry-run` should output what would happen without creating symlinks
3. `dottie link` should create symlinks from sources to targets
4. Symlinks should be readable and point to correct source files
5. Multiple invocations of `dottie link` should be idempotent
