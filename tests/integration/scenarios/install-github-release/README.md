# Integration Test: Install GitHub Release

## Purpose

Tests the basic GitHub release installation functionality by downloading and installing a real binary from GitHub releases.

## Configuration

The `dottie.yml` file specifies:
- **Tool**: jq (JSON processor)
- **Repository**: jqlang/jq
- **Asset Pattern**: `jq-*-linux64` (matches jq-1.7.1-linux64 and similar releases)
- **Binary Name**: jq

## Installation Steps

1. Run `dottie install` with this configuration
2. The installer will:
   - Query GitHub API for the latest jq release
   - Find the asset matching `jq-*-linux64` pattern
   - Download the binary
   - Place it in `~/.dottie-test/bin/`
   - Make it executable

## Validation

The `validate.sh` script will:
1. Check if jq binary exists in `~/.dottie-test/bin/`
2. Verify it's executable
3. Test execution: `jq --version` should return version info

## Platform Support

This test uses `jq-*-linux64` which is platform-specific (Linux only). For cross-platform testing, consider:
- Using `uname` to detect platform and select appropriate asset pattern
- Or using multi-platform binaries like those from `direnv/direnv` or `mgechev/revup`

## Troubleshooting

If the test fails:
1. Verify network connectivity to GitHub API
2. Check that the asset pattern still matches a current release
3. Look for authentication errors if rate-limited (set `GITHUB_TOKEN` environment variable)
4. Ensure `~/.dottie-test/bin/` has write permissions
