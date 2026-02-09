# Integration Test: Install GitHub Release Mixed Types

## Purpose

Tests that `type: binary` (default) and `type: deb` GitHub release entries work correctly together in the same profile, verifying both installation pathways execute without conflicts.

## Configuration

The `dottie.yml` file specifies two GitHub release entries:
- **direnv** (`direnv/direnv`): Binary type (default) — downloaded to `~/bin/`
- **gh** (`cli/cli`): Deb type — installed via `dpkg`

## Validation

The `validate.sh` script will:
1. Run `dottie install` with both entries in the same profile
2. Verify direnv binary exists in `~/bin/`, is executable, and runs
3. Verify gh is installed via `dpkg`, is on PATH, and runs
4. Verify the install output mentions both entries
5. Verify idempotency: second run skips both entries
6. Clean up both installations
