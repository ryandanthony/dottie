# FEATURE-07: CLI Command `install`

## Command

`dottie install [--profile <name>] [--dry-run]`

## Summary

Install software only (no dotfile linking).

## Flags

- `--profile <name>`: Use specific profile (default: `default`)
- `--dry-run`: Show what would happen without making changes

## Behavior

- Resolve profile (including inheritance)
- Execute install sources in priority order (see Installation Sources spec)
- Avoid re-installing already-present tools when possible

## Open questions

- How to detect installed state for GitHub binaries (presence in `~/bin`, version checks)
- How to handle partial failures (continue vs stop)
