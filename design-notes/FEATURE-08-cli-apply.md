# FEATURE-08: CLI Command `apply`

## Command

`dottie apply [--profile <name>] [--force] [--dry-run]`

## Summary

Apply everything: create symlinks and install all software for a selected profile.

## Flags

- `--profile <name>`: Use specific profile (default: `default`)
- `--force`: Overwrite existing files (backs them up first)
- `--dry-run`: Show what would happen without making changes

## Behavior (high level)

- Resolve profile (including `extends` inheritance)
- Link dotfiles (same behavior as `dottie link`)
- Install software (same behavior as `dottie install`)

## Ordering

Installation sources have a priority order:

1. GitHub Releases
2. APT Packages
3. Private APT Repositories
4. Shell Scripts
5. Fonts
6. Snap Packages

## Error handling

- Default is safe: fail on conflicts unless `--force` is provided
- `--dry-run` should not mutate filesystem or install anything
