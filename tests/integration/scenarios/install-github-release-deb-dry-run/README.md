# Integration Test: Install GitHub Release Deb (Dry Run)

## Purpose

Tests that `--dry-run` mode for `type: deb` GitHub release entries validates the release exists and reports what would happen, without actually downloading or installing anything.

## Configuration

The `dottie.yml` file specifies:
- **Tool**: gh (GitHub CLI)
- **Repository**: cli/cli
- **Version**: v2.86.0 (pinned)
- **Type**: deb

## Validation

The `validate.sh` script will:
1. Ensure the `gh` package is not pre-installed
2. Run `dottie install --dry-run` with the deb configuration
3. Verify the output mentions "would be installed via dpkg"
4. Verify the package was **not** actually installed
5. Verify no temporary `.deb` files were left behind
