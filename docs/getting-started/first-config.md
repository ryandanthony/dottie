---
title: First Configuration
sidebar_position: 3
description: Understanding profiles, dotfiles, and install blocks
---

# First Configuration

This guide explains the core concepts of dottie: profiles, dotfile entries, and install blocks. Understanding these will help you customize your setup effectively.

## Profiles

Profiles group related configuration together. You can have multiple profiles for different use cases:

```yaml
profiles:
  default:
    # Your main configuration
    
  work:
    # Work-specific settings
    
  minimal:
    # Lightweight setup for servers
```

### Using Profiles

Specify a profile when running commands:

```bash
> dottie link --profile work
> dottie install -p minimal
```

If you don't specify a profile, dottie uses `default`.

### Profile Inheritance

Profiles can extend other profiles to avoid duplication:

```yaml
profiles:
  default:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc
    install:
      apt:
        - git
        - curl

  work:
    extends: default  # Inherits everything from default
    dotfiles:
      - source: dotfiles/.work-gitconfig
        target: ~/.gitconfig
    install:
      apt:
        - docker.io
```

The `work` profile includes everything from `default` plus its own additions.

## Dotfile Entries

Dotfile entries define symlinks from your repository to their target locations:

```yaml
dotfiles:
  - source: dotfiles/.bashrc    # Path in your repository
    target: ~/.bashrc           # Where the symlink will be created
    
  - source: dotfiles/.config/nvim
    target: ~/.config/nvim
```

| Field | Required | Description |
|-------|----------|-------------|
| `source` | Yes | Path to source file (relative to repo root) |
| `target` | Yes | Target path for symlink (supports `~` expansion) |

### Best Practices

- Keep your dotfiles organized in a `dotfiles/` directory
- Use meaningful names and directory structure
- Test with `--dry-run` before applying

## Install Blocks

Install blocks define software to install. dottie supports multiple installation sources:

```yaml
install:
  apt:
    - git
    - curl
    - vim
    
  scripts:
    - scripts/setup-custom.sh
    
  github:
    - repo: BurntSushi/ripgrep
      asset: ripgrep-{arch}-unknown-linux-musl.tar.gz
      binary: rg
      
  snaps:
    - name: code
      classic: true
      
  fonts:
    - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/FiraCode.zip
    
  aptRepos:
    - name: docker
      key_url: https://download.docker.com/linux/ubuntu/gpg
      repo: "deb https://download.docker.com/linux/ubuntu jammy stable"
      packages:
        - docker-ce
```

### Install Block Types

| Type | Description |
|------|-------------|
| `apt` | APT packages (Debian/Ubuntu) |
| `scripts` | Shell scripts to run (must be within repository) |
| `github` | Download binaries from GitHub releases |
| `snaps` | Snap packages |
| `fonts` | Nerd Fonts or other font downloads |
| `aptRepos` | Add APT repositories before installing packages |

### Installation Order

dottie installs in a specific order to handle dependencies:

1. GitHub Releases (binaries to `~/bin/`)
2. APT Packages
3. APT Repositories (added before their packages)
4. Scripts
5. Fonts (to `~/.local/share/fonts/`)
6. Snap Packages

### Idempotency

dottie automatically detects already-installed software and skips it. Running `dottie install` multiple times is safe and efficient.

## Next Steps

- Explore the [Configuration Reference](/docs/configuration/overview) for all available options
- Learn about specific [CLI Commands](/docs/commands/validate)
- Check out [Guides](/docs/guides/profile-inheritance) for advanced use cases
