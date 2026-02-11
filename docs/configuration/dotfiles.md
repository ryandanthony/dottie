---
title: Dotfiles
sidebar_position: 3
description: Dotfile entry configuration
---

# Dotfiles

Dotfile entries define symbolic links from your repository to their target locations on your system.

## Basic Syntax

```yaml
dotfiles:
  - source: dotfiles/.bashrc
    target: ~/.bashrc
```

## Fields

| Field | Required | Description |
|-------|----------|-------------|
| `source` | Yes | Path to source file (relative to repository root) |
| `target` | Yes | Target path for symlink (supports `~` expansion) |

### Source Path

The source path is relative to your repository root:

```yaml
dotfiles:
  - source: dotfiles/.bashrc           # File
  - source: dotfiles/.config/nvim      # Directory
  - source: config/settings.json       # Any location in repo
```

:::tip
Keep your dotfiles organized in a `dotfiles/` directory for clarity.
:::

### Target Path

The target path supports `~` expansion for the home directory:

```yaml
dotfiles:
  - source: dotfiles/.bashrc
    target: ~/.bashrc                   # Expands to /home/user/.bashrc
    
  - source: dotfiles/nvim
    target: ~/.config/nvim              # Nested directories supported
```

## Examples

### Common Dotfiles

```yaml
dotfiles:
  # Shell configuration
  - source: dotfiles/.bashrc
    target: ~/.bashrc
  - source: dotfiles/.zshrc
    target: ~/.zshrc

  # Git configuration
  - source: dotfiles/.gitconfig
    target: ~/.gitconfig
  - source: dotfiles/.gitignore_global
    target: ~/.gitignore_global

  # Editor configuration
  - source: dotfiles/.vimrc
    target: ~/.vimrc
  - source: dotfiles/nvim
    target: ~/.config/nvim
```

### XDG Configuration

```yaml
dotfiles:
  - source: config/alacritty
    target: ~/.config/alacritty
  - source: config/starship.toml
    target: ~/.config/starship.toml
  - source: config/tmux
    target: ~/.config/tmux
```

## Conflict Handling

When dottie encounters an existing file at the target location:

| Scenario | Default | With `--force` |
|----------|---------|----------------|
| Regular file exists | Error | Backup to `.bak`, then link |
| Wrong symlink exists | Error | Remove, then link |
| Correct symlink exists | Skip | Skip |

### Backup Files

With `--force`, existing files are backed up:

```
~/.bashrc → ~/.bashrc.bak
```

## Best Practices

### Organize Your Repository

```
my-dotfiles/
├── dottie.yaml
└── dotfiles/
    ├── .bashrc
    ├── .gitconfig
    ├── .vimrc
    └── .config/
        ├── nvim/
        └── starship.toml
```

### Use Consistent Naming

- Keep the original dotfile names (with leading `.`)
- Or use descriptive names and let the target define the final name

### Preview Before Applying

```bash
> dottie link --dry-run
```

### Handle Machine-Specific Config

Use profiles for machine-specific dotfiles:

```yaml
profiles:
  default:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc

  work:
    extends: default
    dotfiles:
      - source: dotfiles/.work-bashrc
        target: ~/.bashrc  # Overrides default's .bashrc
```
