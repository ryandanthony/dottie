---
title: Initialize
sidebar_position: 6
description: Create a new dottie configuration file
---

# Initialize Command

The `initialize` command creates a new `dottie.yaml` configuration file with a starter template.

:::info Coming Soon
This command is currently in development. Documentation will be updated when the feature is released.
:::

## Planned Usage

```bash
> dottie init [options]
```

## Planned Options

| Option | Short | Description |
|--------|-------|-------------|
| `--output` | `-o` | Output file path (default: `dottie.yaml`) |
| `--template` | `-t` | Template to use (minimal, standard, full) |
| `--force` | `-f` | Overwrite existing file |

## Planned Templates

### Minimal

A bare-bones configuration to get started:

```yaml
profiles:
  default:
    dotfiles: []
    install:
      apt: []
```

### Standard (Default)

A typical configuration with common patterns:

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
```

### Full

A comprehensive example showing all features:

```yaml
profiles:
  default:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc
    install:
      apt: [git, curl]
      github:
        - repo: BurntSushi/ripgrep
          asset: ripgrep-{arch}-unknown-linux-musl.tar.gz
          binary: rg
      scripts: []
      fonts: []
      snaps: []
      aptRepos: []

  work:
    extends: default
    dotfiles:
      - source: dotfiles/.work-config
        target: ~/.work-config
```

## Current Workaround

Until `init` is available, manually create a `dottie.yaml` file:

```bash
touch dottie.yaml
```

Then add the starter configuration from the [Quick Start](/docs/getting-started/quick-start) guide.
