# FEATURE-10: CLI Command `initialize`

## Command

`dottie initialize <git-repo-url> [--location <location-to-clone>]`

## Summary

Clone a dotfiles repo locally and bootstrap `dottie.yaml` by discovering common existing dotfiles.

## Behavior

1. Clone the provided git repo to `~/.dottie` (or configurable location)
2. Generate initial `dottie.yaml` with a `default` profile
3. Scan for common dotfiles in home directory (`.bashrc`, `.vimrc`, `.gitconfig`, etc.)
4. Add discovered dotfiles to the `default` profile in `dottie.yaml`
5. Move found dotfiles into the repo’s `dotfiles/` directory
6. Create symlinks back to original locations

## Notes / questions

- Define the list of “common dotfiles” and whether it’s configurable
- Define how to handle existing repo content vs discovered files (conflicts/merges)
- Define behavior when target already a symlink
