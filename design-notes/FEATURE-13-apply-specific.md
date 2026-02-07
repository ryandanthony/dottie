# FEATURE-13: CLI Command `apply --specific`

## Command

`dottie apply [--profile <name>] [--specific <item>...] [--force] [--dry-run]`

## Summary

Apply specific dotfiles or install specific software items from a profile. This allows granular control when troubleshooting configurations without applying everything at once.

## Flags

- `--profile <name>`: Use specific profile (default: `default`)
- `--specific <item>...`: One or more specific items to apply/install (e.g., `google-chrome-stable`, `config.nix`, `.bashrc`)
- `--force`: Overwrite existing files (backs them up first)
- `--dry-run`: Show what would happen without making changes

## Examples

```bash
# Install only google-chrome-stable from dev profile
dottie apply --profile dev --specific google-chrome-stable

# Link only the .bashrc and .zshrc dotfiles
dottie apply --specific .bashrc .zshrc

# Install multiple specific items with dry-run
dottie apply --profile dev --specific google-chrome-stable kubectl --dry-run

# Force apply a specific dotfile
dottie apply --specific config.nix --force
```

## Behavior (high level)

- Resolve profile (including `extends` inheritance)
- Filter dotfiles and install items by the specified names
- For dotfiles: match by filename (e.g., `.bashrc`)
- For install items: match by item name (e.g., `google-chrome-stable`)
- Link matching dotfiles (same behavior as `dottie link`)
- Install matching software (same behavior as `dottie install`)
- Respect ordering for installation sources when installing multiples

## Matching Logic

### Dotfile Matching

- Match by target filename (the part after `link:` key in config)
- Example: `--specific .bashrc` matches a dotfile with `link: ~/.bashrc`

### Install Item Matching

- Match by item identifier (the package/tool name as specified in config)
- Case-sensitive match
- Supports both short names (e.g., `kubectl`) and full package names (e.g., `google-chrome-stable`)

## Ordering

When multiple items are specified, installation sources respect their priority order:

1. GitHub Releases
2. APT Packages
3. Private APT Repositories
4. Shell Scripts
5. Fonts
6. Snap Packages

## Error handling

- Default is safe: fail on conflicts unless `--force` is provided
- If a specified item is not found in the profile, warn and skip (continue with others)
- `--dry-run` should not mutate filesystem or install anything
- Return non-zero exit code if no specified items were found

## Use Cases

### Troubleshooting Installation

Test a single tool installation before applying everything:
```bash
dottie apply --profile dev --specific google-chrome-stable --dry-run
```

### Partial Configuration Recovery

Apply only critical dotfiles when recovering from a failed configuration:
```bash
dottie apply --specific .bashrc .profile --force
```

### Testing New Additions

When adding a new package to a profile, test it in isolation:
```bash
dottie apply --profile dev --specific new-tool-name --dry-run
```

### Configuration Verification

Verify which files would be linked without applying:
```bash
dottie apply --specific config.json settings.yaml --dry-run
```

## Open questions

- Should partial failures in a multi-item apply stop execution or continue?
- How detailed should the output be for each specific item?
- Should we provide a way to list available items in a profile for easier discovery?

