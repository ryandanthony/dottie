# Variable Install Types Integration Test

## Purpose

Tests that `${VARIABLE_NAME}` substitution works in all install type fields:
- **Scripts**: Variable substitution in script paths
- **APT packages**: Variable substitution in package names
- **APT repos**: `${SIGNING_FILE}` deferred variable in repo lines

This complements the `variable-substitution` scenario which focuses on dotfile paths and basic apt repo variable usage.

## Variables Tested

| Variable | Expected Value (Ubuntu 24.04) | Used In |
|----------|-------------------------------|---------|
| `${ID}` | `ubuntu` | Script path |
| `${VERSION_CODENAME}` | `noble` | Script path |
| `${MS_ARCH}` | `amd64` or `arm64` | APT package name |
| `${SIGNING_FILE}` | `/etc/apt/trusted.gpg.d/<name>.gpg` | APT repo `repo` field (deferred) |

## Validation

The `validate.sh` script:
1. Runs `dottie validate` to verify variables resolve at config load time
2. Runs `dottie install --dry-run` and verifies no `${...}` tokens remain in output
3. Runs `dottie install` with a script that uses `${ID}` in its path
4. Verifies the script at the resolved path was actually executed
