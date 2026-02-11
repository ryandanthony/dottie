---
title: Link
sidebar_position: 2
description: Create symbolic links for your dotfiles
---

# Link Command

The `link` command creates symbolic links from your repository to their target locations.

## Usage

```bash
> dottie link [options]
```

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--profile` | `-p` | Profile to use (default: "default") |
| `--config` | `-c` | Path to configuration file |
| `--dry-run` | `-d` | Preview changes without creating symlinks |
| `--force` | `-f` | Backup existing files and overwrite conflicts |

:::note
`--dry-run` and `--force` are mutually exclusive.
:::

## Examples

### Link using default profile

```bash
> dottie link
```

### Link using a specific profile

```bash
> dottie link --profile work
> dottie link -p work
```

### Preview changes (dry-run)

```bash
> dottie link --dry-run
> dottie link -d
```

### Force linking with backups

```bash
> dottie link --force
> dottie link -f
```

### Use custom config path

```bash
> dottie link --config /path/to/dottie.yaml
> dottie link -c /path/to/dottie.yaml
```

## Output Examples

### Normal Operation

```
✓ Created 5 symlink(s).
Skipped 2 file(s) (already linked).
```

### With Conflicts (no --force)

```
Error: Conflicting files detected. Use --force to backup and overwrite.
Conflicts:
  • ~/.bashrc (file)
  • ~/.config/nvim (symlink → /other/path)
Found 2 conflict(s).
```

### Dry-Run Preview

```
Dry run - no changes will be made.
Would create 3 symlink(s):
  • dotfiles/bashrc → ~/.bashrc
  • dotfiles/vimrc → ~/.vimrc
  • dotfiles/gitconfig → ~/.gitconfig
Would skip 1 file(s) (already linked):
  • ~/.zshrc
```

## Conflict Handling

When dottie encounters existing files at target locations, it handles them based on the flags:

| Scenario | Default Behavior | With `--force` |
|----------|------------------|----------------|
| File exists | Error, stop | Backup to `.bak`, then link |
| Symlink exists (wrong target) | Error, stop | Remove, then link |
| Symlink exists (correct target) | Skip | Skip |

### Backup Location

When using `--force`, existing files are renamed with a `.bak` extension:

```
~/.bashrc → ~/.bashrc.bak
```

## Best Practices

1. **Preview first** - Always run with `--dry-run` before applying
2. **Check backups** - After using `--force`, verify your `.bak` files
3. **Use profiles** - Organize dotfiles by context (work, personal, server)
