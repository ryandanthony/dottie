# Integration Test: Install GitHub Release (Deb Package)

## Purpose

Tests the `type: deb` GitHub release installation functionality by downloading and installing a real `.deb` package from GitHub releases using `dpkg`.

## Configuration

The `dottie.yml` file specifies:
- **Tool**: gh (GitHub CLI)
- **Repository**: cli/cli
- **Asset**: `gh_2.86.0_linux_amd64.deb`
- **Version**: v2.86.0 (pinned for reproducibility)
- **Type**: deb (triggers dpkg installation path)

## Installation Steps

1. Run `dottie install` with this configuration
2. The installer will:
   - Query GitHub API for the v2.86.0 release of cli/cli
   - Find the asset matching `gh_2.86.0_linux_amd64.deb`
   - Download the `.deb` package to a temp file
   - Extract the package name via `dpkg-deb -W`
   - Check idempotency via `dpkg -s`
   - Install the package via `sudo dpkg -i`
   - Fix dependencies via `sudo apt-get install -f`
   - Clean up the temp file

## Validation

The `validate.sh` script will:
1. Run `dottie install` with the deb configuration
2. Verify the `gh` package is installed via `dpkg -s gh`
3. Verify the `gh` binary is on PATH and executable
4. Test execution: `gh --version` should return version info
5. Verify idempotency: running install again should succeed without errors
