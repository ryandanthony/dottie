# Integration Test: Install GitHub Release (Archive Extraction)

## Purpose

Tests the binary-type GitHub release installer with a compressed `.tar.gz` asset that contains PAX extended headers. This verifies that the tar parser correctly handles modern tar formats produced by tools like GNU tar.

## Configuration

The `dottie.yml` file specifies:
- **Tool**: fzf (fuzzy finder)
- **Repository**: junegunn/fzf
- **Asset**: `fzf-0.57.0-linux_amd64.tar.gz` (via `${RELEASE_VERSION}` substitution)
- **Version**: v0.57.0 (pinned for reproducibility)
- **Binary**: fzf (extracted from the archive)

## Why This Test Matters

Many GitHub releases ship binaries inside `.tar.gz` archives created with modern tar tools that include PAX extended headers. The custom tar parser must handle these headers (type flags `x`, `g`, `L`, `K`) by skipping them rather than choking on non-standard size fields.

## Validation

The `validate.sh` script will:
1. Run `dottie install` with the archive configuration
2. Verify the fzf binary was extracted to `~/bin/`
3. Verify it's executable
4. Verify it runs (`fzf --version`)
5. Verify the version number matches 0.57.0
6. Verify idempotency (second run skips)
