# APT Package Installation Integration Test

## Purpose

Tests the `dottie install` command with APT package installation source.

## Configuration

The `dottie.yml` file specifies two packages to install via apt-get:
- `jq` - JSON processor
- `tree` - Directory listing utility

## Validation

The `validate.sh` script:
1. Initializes a git repository (required for dottie)
2. Checks if each package is installed via dpkg
3. Returns 0 if all packages are found, 1 if any are missing

## Requirements

- Must be run with sudo privileges (for apt-get install)
- Requires a system with apt-get (Ubuntu/Debian)
- Internet connectivity for package downloads

## Notes

- This test uses lightweight, commonly available packages
- APT installation requires the --no-break-system flag or sudo access
- The test validates that `dottie install` correctly invokes apt-get
