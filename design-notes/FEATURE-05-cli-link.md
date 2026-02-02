# FEATURE-05: CLI Command `link`

**STATUS**: Done

## Command

`dottie link [--profile <name>] [--force] [--dry-run]`

## Summary

Create symlinks for dotfiles only.

## Flags

- `--profile <name>`: Use specific profile (default: `default`)
- `--force`: Overwrite existing files (backs them up first)
- `--dry-run`: Show what would happen without making changes

## Behavior

- For each dotfile mapping:
  - Ensure source exists in repo
  - Ensure parent directories for target exist
  - If target does not exist: create symlink
  - If target exists:
    - If already correct symlink: no-op
    - If different file/dir: conflict handling (see Conflict Handling spec)

## Open questions

- Whether directories are supported as targets (e.g. `~/.config/nvim`) and how symlinks are created for them
