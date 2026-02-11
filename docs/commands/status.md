---
title: Status
sidebar_position: 5
description: Check the current state of your dotfiles and installed software
---

# Status Command

The `status` command shows the current state of your dotfiles and installed software.

:::info Coming Soon
This command is currently in development. Documentation will be updated when the feature is released.
:::

## Planned Usage

```bash
> dottie status [options]
```

## Planned Options

| Option | Short | Description |
|--------|-------|-------------|
| `--profile` | `-p` | Profile to check (default: "default") |
| `--config` | `-c` | Path to configuration file |

## Planned Output

### Dotfiles Status

```
Dotfiles:
  ✓ ~/.bashrc → dotfiles/.bashrc (linked)
  ✓ ~/.gitconfig → dotfiles/.gitconfig (linked)
  ✗ ~/.vimrc (not linked)
  ⚠ ~/.zshrc → /other/path (wrong target)
```

### Software Status

```
Software:
  ✓ git (apt) - installed
  ✓ rg (github) - installed at ~/bin/rg
  ✗ fd (github) - not installed
```

## Use Cases

- **Pre-flight check** — See what needs to be applied before running `link` or `install`
- **Verify state** — Confirm your configuration is fully applied
- **Troubleshooting** — Identify misconfigurations or missing components
