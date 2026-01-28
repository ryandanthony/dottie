# FEATURE-09: CLI Command `status`

## Command

`dottie status [--profile <name>]`

## Summary

Report current state:

- Which dotfiles are linked / missing / conflicting
- Which software is installed / missing / outdated

## Behavior (proposed)

- Resolve profile (including inheritance)
- Dotfiles:
  - Linked correctly
  - Missing target
  - Conflicting existing file/dir
- Software:
  - Installed
  - Missing
  - Outdated (when versions are knowable/pinned)

## Output

- Human-readable summary suitable for terminal
- Optional future enhancement: machine-readable output (JSON)
