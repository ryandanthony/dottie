---
title: Apply
sidebar_position: 4
description: Apply both dotfile links and software installation in one command
---

# Apply Command

The `apply` command combines `link` and `install` into a single operation, applying your complete configuration.

:::info Coming Soon
This command is currently in development. Documentation will be updated when the feature is released.
:::

## Planned Usage

```bash
> dottie apply [options]
```

## Planned Options

| Option | Short | Description |
|--------|-------|-------------|
| `--profile` | `-p` | Profile to use (default: "default") |
| `--config` | `-c` | Path to configuration file |
| `--dry-run` | `-d` | Preview changes without applying |
| `--force` | `-f` | Backup existing files and overwrite conflicts |

## Planned Behavior

The `apply` command will:

1. Validate the configuration
2. Create all dotfile symlinks (like `link`)
3. Install all software packages (like `install`)

This is equivalent to running:

```bash
> dottie validate <profile>
> dottie link --profile <profile>
> dottie install --profile <profile>
```

## Current Workaround

Until `apply` is available, run the individual commands:

```bash
> dottie validate default
> dottie link
> dottie install
```
