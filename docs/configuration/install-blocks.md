---
title: Install Blocks
sidebar_position: 4
description: Software installation configuration
---

# Install Blocks

Install blocks define software to install from various sources. dottie supports multiple installation types that can be combined.

## Overview

```yaml
install:
  apt: [...]           # APT packages
  aptRepos: [...]      # APT repositories
  github: [...]        # GitHub releases
  scripts: [...]       # Custom scripts
  fonts: [...]         # Font files
  snaps: [...]         # Snap packages
```

## APT Packages

Install packages from the system's APT repositories:

```yaml
install:
  apt:
    - git
    - curl
    - vim
    - build-essential
```

Packages already installed are automatically skipped.

## APT Repositories

Add third-party APT repositories and install their packages:

```yaml
install:
  aptRepos:
    - name: docker
      key_url: https://download.docker.com/linux/ubuntu/gpg
      repo: "deb https://download.docker.com/linux/ubuntu jammy stable"
      packages:
        - docker-ce
        - docker-ce-cli

    - name: github-cli
      key_url: https://cli.github.com/packages/githubcli-archive-keyring.gpg
      repo: "deb https://cli.github.com/packages stable main"
      packages:
        - gh
```

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Identifier for the repository |
| `key_url` | Yes | URL to the GPG key |
| `repo` | Yes | APT repository line |
| `packages` | Yes | Packages to install from this repo |

## GitHub Releases

Download binaries from GitHub releases:

```yaml
install:
  github:
    - repo: BurntSushi/ripgrep
      asset: ripgrep-${ARCH}-unknown-linux-musl.tar.gz
      binary: rg

    - repo: sharkdp/fd
      asset: fd-v${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
      binary: fd
      version: 9.0.0  # Optional: specific version

    - repo: junegunn/fzf
      asset: fzf-${RELEASE_VERSION}-linux_amd64.tar.gz
      binary: fzf
```

| Field | Required | Description |
|-------|----------|-------------|
| `repo` | Yes | GitHub repository (owner/repo) |
| `asset` | Yes | Asset filename pattern |
| `binary` | Yes | Binary name after extraction |
| `version` | No | Specific version (default: latest) |

### Architecture Placeholders

Use `${ARCH}` in asset names for architecture-aware downloads:

| Variable | x86_64 Value | arm64 Value |
|-------------|--------------|-------------|
| `${ARCH}` | `x86_64` | `aarch64` |
| `${MS_ARCH}` | `amd64` | `arm64` |

Binaries are installed to `~/bin/`.

## Scripts

Run custom shell scripts:

```yaml
install:
  scripts:
    - scripts/setup-nvm.sh
    - scripts/configure-docker.sh
```

:::caution Security
Scripts must be located within your repository. External scripts are not allowed.
:::

Scripts are run from the repository root with the user's shell.

## Fonts

Install fonts from URL downloads:

```yaml
install:
  fonts:
    - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/FiraCode.zip
    - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/JetBrainsMono.zip
```

Fonts are extracted to `~/.local/share/fonts/` and the font cache is refreshed.

## Snap Packages

Install applications from the Snap store:

```yaml
install:
  snaps:
    - name: code
      classic: true

    - name: slack
      classic: true

    - name: htop
      # classic: false is the default
```

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Snap package name |
| `classic` | No | Install with classic confinement (default: false) |

## Installation Order

dottie installs in a specific order to handle dependencies:

1. **GitHub Releases** → `~/bin/`
2. **APT Packages** → System
3. **APT Repositories** → System (repos added, then packages installed)
4. **Scripts** → Run in order listed
5. **Fonts** → `~/.local/share/fonts/`
6. **Snap Packages** → System

## Idempotency

All installation types detect existing installations:

- **APT**: Checks `dpkg -l`
- **GitHub**: Checks for binary in `~/bin/`
- **Fonts**: Checks font directory
- **Snaps**: Checks `snap list`

Re-running `dottie install` is safe and efficient.

## Example: Complete Install Block

```yaml
install:
  apt:
    - git
    - curl
    - vim
    - tmux

  github:
    - repo: BurntSushi/ripgrep
      asset: ripgrep-${ARCH}-unknown-linux-musl.tar.gz
      binary: rg
    - repo: sharkdp/fd
      asset: fd-v${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
      binary: fd
    - repo: sharkdp/bat
      asset: bat-v${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
      binary: bat

  fonts:
    - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/FiraCode.zip

  snaps:
    - name: code
      classic: true

  aptRepos:
    - name: docker
      key_url: https://download.docker.com/linux/ubuntu/gpg
      repo: "deb https://download.docker.com/linux/ubuntu jammy stable"
      packages:
        - docker-ce

  scripts:
    - scripts/post-install.sh
```
