# Variable Substitution Integration Test

## Purpose

Tests that `${VARIABLE_NAME}` substitution works end-to-end when loading and applying a dottie configuration. Verifies that OS release variables from `/etc/os-release` and architecture variables are resolved correctly in dotfile paths, apt repository URLs, and package names.

## Variables Tested

| Variable | Expected Value (Ubuntu 24.04) | Used In |
|----------|-------------------------------|---------|
| `${VERSION_CODENAME}` | `noble` | dotfile source path, apt repo URL |
| `${ID}` | `ubuntu` | dotfile source path |
| `${MS_ARCH}` | `amd64` or `arm64` | apt repo URL, package name |
| `${ARCH}` | `x86_64` or `aarch64` | (available but not directly tested) |

## Configuration

The `dottie.yml` file uses variables in:
- **Dotfile source paths**: `dotfiles/${VERSION_CODENAME}/bashrc` → `dotfiles/noble/bashrc`
- **Dotfile source paths**: `dotfiles/${ID}/profile` → `dotfiles/ubuntu/profile`
- **Apt repo URL**: `deb [arch=${MS_ARCH}] https://...` → `deb [arch=amd64] https://...`
- **Package name**: `doesnotexist-${MS_ARCH}` → `doesnotexist-amd64`

## Validation

The `validate.sh` script:
1. Runs `dottie validate` to verify the config loads and variables resolve
2. Runs `dottie link --dry-run` to verify resolved dotfile paths point to correct sources
3. Checks that the dry-run output references the resolved paths (not raw `${...}` tokens)
4. Verifies the actual symlink creation with `dottie link` resolves to the correct files

## Requirements

- Must run on a Linux system with `/etc/os-release` present (Ubuntu 24.04 container)
- Source files must exist at the resolved paths (`dotfiles/noble/`, `dotfiles/ubuntu/`)
