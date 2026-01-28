# FEATURE-04: Conflict Handling (Dotfile Linking)

## Summary

When a target file already exists and is not already a symlink to the repo, Dottie must handle conflicts safely.

## Default behavior

- Fail with error and show which files conflict.

## With `--force`

- Back up existing file to `<filename>.backup.<timestamp>`
- Then create the symlink

## Scope

- Applies to `dottie link` and the linking portion of `dottie apply`

## Open questions

- How to back up directories vs files
- Whether to preserve ownership/permissions
- Whether to support a centralized backup directory
