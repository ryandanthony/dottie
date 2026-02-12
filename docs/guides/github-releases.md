---
title: GitHub Releases
sidebar_position: 2
description: How to download and install binaries from GitHub releases
---

# GitHub Releases Guide

This guide explains how to configure dottie to download binaries from GitHub releases, including architecture-aware downloads and version pinning.

## Basic Configuration

```yaml
install:
  github:
    - repo: BurntSushi/ripgrep
      asset: ripgrep-${ARCH}-unknown-linux-musl.tar.gz
      binary: rg
```

This downloads the latest release of ripgrep, extracts it, and installs the `rg` binary to `~/bin/`.

## Required Fields

| Field | Description |
|-------|-------------|
| `repo` | GitHub repository in `owner/repo` format |
| `asset` | Asset filename to download (supports placeholders) |
| `binary` | Name of the binary to install |

## Architecture Placeholders

Use `${ARCH}` to automatically select the right architecture:

```yaml
github:
  - repo: sharkdp/fd
    asset: fd-v${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
    binary: fd
```

| Variable | x86_64 | arm64 |
|-------------|--------|-------|
| `${ARCH}` | `x86_64` | `aarch64` |
| `${MS_ARCH}` | `amd64` | `arm64` |

## Version Pinning

By default, dottie downloads the latest release. Pin to a specific version:

```yaml
github:
  - repo: junegunn/fzf
    asset: fzf-${RELEASE_VERSION}-linux_amd64.tar.gz
    binary: fzf
    version: 0.44.0  # Pinned version
```

## Finding Asset Names

1. Go to the repository's **Releases** page
2. Find the release you want
3. Look at the **Assets** section
4. Copy the filename for your platform

Example for ripgrep:
```
ripgrep-14.1.0-x86_64-unknown-linux-musl.tar.gz
ripgrep-14.1.0-aarch64-unknown-linux-musl.tar.gz
```

Replace the version and architecture with placeholders:
```
ripgrep-${ARCH}-unknown-linux-musl.tar.gz
```

## Common Tools

Here are configurations for popular tools:

### ripgrep (rg)

```yaml
- repo: BurntSushi/ripgrep
  asset: ripgrep-${ARCH}-unknown-linux-musl.tar.gz
  binary: rg
```

### fd

```yaml
- repo: sharkdp/fd
  asset: fd-v${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
  binary: fd
```

### bat

```yaml
- repo: sharkdp/bat
  asset: bat-v${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
  binary: bat
```

### fzf

```yaml
- repo: junegunn/fzf
  asset: fzf-${RELEASE_VERSION}-linux_amd64.tar.gz
  binary: fzf
```

### lazygit

```yaml
- repo: jesseduffield/lazygit
  asset: lazygit_${RELEASE_VERSION}_Linux_${ARCH}.tar.gz
  binary: lazygit
```

### delta

```yaml
- repo: dandavison/delta
  asset: delta-${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
  binary: delta
```

## Archive Formats

dottie handles these archive formats automatically:

- `.tar.gz` / `.tgz`
- `.tar.xz`
- `.tar.bz2`
- `.zip`

The binary is extracted and placed in `~/bin/`.

## Idempotency

dottie checks if the binary already exists in `~/bin/` before downloading:

```
⊘ rg Skipped (GithubRelease) - Already installed in ~/bin/
```

To reinstall, manually remove the binary first:

```bash
rm ~/bin/rg
> dottie install
```

## Troubleshooting

### Asset Not Found

```
✗ mytool Failed (GithubRelease) - Asset 'mytool-linux.tar.gz' not found
```

**Solution**: Check the exact asset name on the GitHub releases page.

### Version Not Found

```
✗ mytool Failed (GithubRelease) - Version 99.0.0 not found
```

**Solution**: Verify the version exists or use `latest` (default).

### Binary Not in PATH

Make sure `~/bin` is in your PATH:

```bash
export PATH="$HOME/bin:$PATH"
```

Add this to your `~/.bashrc` or `~/.zshrc`.

## Complete Example

```yaml
install:
  github:
    # Essential CLI tools
    - repo: BurntSushi/ripgrep
      asset: ripgrep-${ARCH}-unknown-linux-musl.tar.gz
      binary: rg

    - repo: sharkdp/fd
      asset: fd-v${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
      binary: fd

    - repo: sharkdp/bat
      asset: bat-v${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
      binary: bat

    # Git tools
    - repo: jesseduffield/lazygit
      asset: lazygit_${RELEASE_VERSION}_Linux_${ARCH}.tar.gz
      binary: lazygit

    - repo: dandavison/delta
      asset: delta-${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
      binary: delta

    # Pinned version example
    - repo: junegunn/fzf
      asset: fzf-${RELEASE_VERSION}-linux_amd64.tar.gz
      binary: fzf
      version: 0.44.0
```
