# Link and Install Combined Scenario

## Purpose
Test that dottie handles both linking and installing in the same profile/configuration.

## What is tested
1. **Dotfile linking** - Links dotfiles to home directory
2. **Package installation** - Installs packages via APT
3. **Combined execution** - Both operations work in same profile
4. **Ordering** - Operations execute in correct order
5. **Independence** - Link success doesn't depend on install and vice versa

## Expected behavior
1. Configuration with both link and install sections validates
2. `dottie link` creates symlinks
3. `dottie install` installs packages
4. Both commands can be run in either order
5. State after both is consistent and correct

## Rationale
Users often want to both link dotfiles and install tools in one step.
This tests that both operations work correctly together in a real workflow.
