# Quickstart: CLI Command `install`

**Feature**: 007-cli-install
**Date**: February 1, 2026

## Overview

The `dottie install` command installs software tools defined in your dottie configuration. It supports profile selection, dry-run preview, and automatically skips already-installed tools.

## Basic Usage

### Install All Tools (Default Profile)

```bash
dottie install
```

Installs all software defined in the "default" profile.

### Install with Specific Profile

```bash
dottie install --profile work
```

Installs software defined in the "work" profile, including any inherited from parent profiles.

### Preview Without Installing

```bash
dottie install --dry-run
```

Shows what would be installed without making changes. Checks system state to show accurate "would install" vs "would skip" status.

### Specify Config File

```bash
dottie install --config ~/dotfiles/dottie.yaml
```

## Configuration Example

```yaml
# dottie.yaml
profiles:
  default:
    install:
      # Priority 1: GitHub Releases
      github:
        - repo: BurntSushi/ripgrep
          asset: ripgrep-*-x86_64-unknown-linux-musl.tar.gz
          binary: rg
        - repo: sharkdp/fd
          asset: fd-*-x86_64-unknown-linux-musl.tar.gz
          binary: fd
          version: "9.0.0"  # Pin specific version
      
      # Priority 2: APT Packages
      apt:
        - git
        - curl
        - jq
      
      # Priority 3: Private APT Repos
      aptRepos:
        - name: docker
          key_url: https://download.docker.com/linux/ubuntu/gpg
          repo: "deb [arch=amd64] https://download.docker.com/linux/ubuntu focal stable"
          packages:
            - docker-ce
            - docker-ce-cli
      
      # Priority 4: Scripts
      scripts:
        - scripts/install-nvm.sh
      
      # Priority 5: Fonts
      fonts:
        - name: JetBrainsMono
          url: https://github.com/JetBrains/JetBrainsMono/releases/download/v2.304/JetBrainsMono-2.304.zip
      
      # Priority 6: Snap Packages
      snap:
        - name: code
          classic: true
  
  work:
    extends: default  # Inherits all install sources from default
    install:
      apt:
        - awscli
        - terraform
```

## Output Examples

### Successful Installation

```
✓ rg Success (GithubRelease) - Installed to ~/bin/rg
✓ fd Success (GithubRelease) - Installed to ~/bin/fd
⊘ git Skipped (AptPackage) - Already installed
✓ curl Success (AptPackage)
✓ jq Success (AptPackage)

Installation Summary:
  ✓ Succeeded: 4
  ⊘ Skipped: 1
```

### Dry-Run Output

```
Dry Run Mode: Previewing installation without making changes

✓ rg Success (GithubRelease) - GitHub release BurntSushi/ripgrep@latest would be installed
⊘ fd Skipped (GithubRelease) - Already installed in ~/bin/
⊘ git Skipped (AptPackage) - Already installed
✓ curl Success (AptPackage) - Would be installed via apt
✓ jq Success (AptPackage) - Would be installed via apt

Installation Summary:
  Would install: 3
  Would skip: 2
```

### With Failures

```
✓ rg Success (GithubRelease) - Installed to ~/bin/rg
✗ nonexistent Failed (GithubRelease) - Version 99.0.0 not found
✓ git Success (AptPackage)
✗ fake-package Failed (AptPackage) - Package not found

Installation Summary:
  ✓ Succeeded: 2
  ✗ Failed: 2

Failed Installations:
  [GitHub] nonexistent: Version 99.0.0 not found
  [APT] fake-package: Package not found in repositories
```

## Environment Variables

### GITHUB_TOKEN

Set to increase GitHub API rate limits (60/hour → 5000/hour):

```bash
export GITHUB_TOKEN=ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
dottie install
```

If rate limited without a token, you'll see:

```
✗ ripgrep Failed (GithubRelease) - GitHub API rate limit exceeded. Set GITHUB_TOKEN environment variable for higher limits.
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All installations succeeded (or were skipped) |
| 1 | One or more installations failed |

## CLI Reference

```
USAGE:
    dottie install [OPTIONS]

OPTIONS:
    -p, --profile <NAME>    Profile to use (default: 'default')
    -c, --config <PATH>     Path to configuration file (default: dottie.yaml in repo root)
    --dry-run               Preview the installation without executing changes
    -h, --help              Show help information
```

## Troubleshooting

### "Profile 'xyz' not found"

Check your dottie.yaml has the profile defined:

```yaml
profiles:
  xyz:  # Must match the --profile argument
    install:
      ...
```

### "GitHub API rate limit exceeded"

Set the GITHUB_TOKEN environment variable with a GitHub personal access token.

### "sudo required for APT installation"

APT and Snap packages require sudo. Run dottie from a terminal with sudo access, or ensure your user has passwordless sudo configured.

### "Binary already installed" but want to reinstall

Currently, dottie doesn't support forced reinstallation. Remove the binary from ~/bin/ first, then run install again.
