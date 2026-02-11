---
title: Install
sidebar_position: 3
description: Install software packages from your configuration
---

# Install Command

The `install` command installs software packages defined in your configuration's install block.

## Usage

```bash
> dottie install [options]
```

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--profile` | `-p` | Profile to use (default: "default") |
| `--config` | `-c` | Path to configuration file |
| `--dry-run` | | Preview installation without making changes |

## Examples

### Install using default profile

```bash
> dottie install
```

### Install using a specific profile

```bash
> dottie install --profile work
> dottie install -p work
```

### Preview what would be installed

```bash
> dottie install --dry-run
```

### Use custom config path

```bash
> dottie install --config /path/to/dottie.yaml
```

## Installation Priority Order

dottie installs components in a specific order to handle dependencies correctly:

1. **GitHub Releases** — Binaries downloaded to `~/bin/`
2. **APT Packages** — Standard Debian/Ubuntu packages
3. **APT Repositories** — Added before installing their packages
4. **Scripts** — Custom setup scripts
5. **Fonts** — Installed to `~/.local/share/fonts/`
6. **Snap Packages** — Snap store applications

## Idempotency

Already-installed tools are automatically detected and skipped. Running `dottie install` multiple times is safe and efficient.

## Output Examples

### Successful Installation

```
✓ rg Success (GithubRelease) - Installed to ~/bin/rg
⊘ git Skipped (AptPackage) - Already installed
✓ curl Success (AptPackage)

Installation Summary:
  ✓ Succeeded: 2
  ⊘ Skipped: 1
```

### Dry-Run Preview

```
Dry Run Mode: Previewing installation without making changes
✓ rg Success (GithubRelease) - GitHub release BurntSushi/ripgrep@latest would be installed
⊘ fd Skipped (GithubRelease) - Already installed in ~/bin/

Installation Summary:
  ✓ Succeeded: 1
  ⊘ Skipped: 1
```

### With Failures

```
✓ rg Success (GithubRelease)
✗ nonexistent Failed (GithubRelease) - Version 99.0.0 not found

Installation Summary:
  ✓ Succeeded: 1
  ✗ Failed: 1

Failed Installations:
  [GithubRelease] nonexistent: Version 99.0.0 not found
```

## Install Block Types

| Type | Description | Install Location |
|------|-------------|------------------|
| `apt` | APT packages (Debian/Ubuntu) | System |
| `scripts` | Shell scripts to run | N/A |
| `github` | Download binaries from GitHub releases | `~/bin/` |
| `snaps` | Snap packages | System |
| `fonts` | Nerd Fonts or other font downloads | `~/.local/share/fonts/` |
| `aptRepos` | Add APT repositories | System |

## Best Practices

1. **Use dry-run first** - Preview changes before installing
2. **Check failures** - Review failed installations and fix configuration
3. **Keep ~/bin in PATH** - Ensure GitHub release binaries are accessible
