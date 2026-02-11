---
title: Quick Start
sidebar_position: 2
description: Create your first dottie configuration
---

# Quick Start

This guide walks you through creating your first dottie configuration and running your first command.

## Create Your Configuration

Create a `dottie.yaml` file in your dotfiles repository:

```yaml
profiles:
  default:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc
      - source: dotfiles/.gitconfig
        target: ~/.gitconfig

    install:
      apt:
        - git
        - curl
        - vim
```

## Validate Your Configuration

Before applying changes, validate your configuration to ensure it's correct:

```bash
> dottie validate default
```

If the configuration is valid, you'll see a success message. If there are issues, dottie will show you what needs to be fixed.

## Preview Changes

Use the `--dry-run` flag to preview what dottie will do without making any changes:

```bash
> dottie link --dry-run
```

This shows which symlinks will be created:

```
Dry run - no changes will be made.
Would create 2 symlink(s):
  • dotfiles/.bashrc → ~/.bashrc
  • dotfiles/.gitconfig → ~/.gitconfig
```

## Apply Your Configuration

When you're ready, run the commands without `--dry-run`:

```bash
# Create symlinks for your dotfiles
> dottie link

# Install the software packages
> dottie install
```

## What's Next?

Now that you have a working configuration, learn more about:

- [First Configuration](./first-config) - Understanding profiles, dotfiles, and install blocks
- [Configuration Overview](/docs/configuration/overview) - Detailed configuration reference
- [Commands](/docs/commands/validate) - All available CLI commands
