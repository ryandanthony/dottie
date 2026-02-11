---
title: Overview
sidebar_position: 1
description: Introduction to dottie configuration
---

# Configuration Overview

dottie uses a single YAML configuration file (`dottie.yaml`) to define your dotfiles and software installation.

## Configuration File

By default, dottie looks for `dottie.yaml` in the current directory. You can specify a different path with the `--config` flag.

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

## Structure

A configuration file contains one or more **profiles**. Each profile defines:

- **dotfiles** - Symlink mappings from your repository to target locations
- **install** - Software packages to install from various sources

```yaml
profiles:
  <profile-name>:
    extends: <parent-profile>  # Optional
    dotfiles:
      - source: <path>
        target: <path>
    install:
      apt: [...]
      github: [...]
      scripts: [...]
      fonts: [...]
      snaps: [...]
      aptRepos: [...]
```

## Profiles

Profiles let you organize configurations for different contexts:

```yaml
profiles:
  default:
    # Base configuration for all machines
    
  work:
    extends: default
    # Work-specific additions
    
  server:
    # Minimal server setup
```

See [Profiles](./profiles) for details on profile inheritance.

## Dotfiles

Dotfile entries define symbolic links:

```yaml
dotfiles:
  - source: dotfiles/.bashrc
    target: ~/.bashrc
```

See [Dotfiles](./dotfiles) for field reference and best practices.

## Install Blocks

Install blocks define software to install:

```yaml
install:
  apt:
    - git
    - curl
  github:
    - repo: BurntSushi/ripgrep
      asset: ripgrep-{arch}.tar.gz
      binary: rg
```

See [Install Blocks](./install-blocks) for all available install types.

## Validation

Always validate your configuration before applying:

```bash
> dottie validate default
```

This catches common errors like:
- Invalid YAML syntax
- Missing required fields
- Non-existent source files
- Circular profile inheritance

## Best Practices

1. **Start simple** - Begin with a minimal configuration and expand
2. **Use profiles** - Separate configurations by context (work, home, server)
3. **Validate often** - Run `validate` after every change
4. **Preview first** - Use `--dry-run` before applying
5. **Version control** - Keep your `dottie.yaml` in git
