---
title: APT Repositories
sidebar_position: 3
description: How to add third-party APT repositories
---

# APT Repositories Guide

This guide explains how to add third-party APT repositories to install packages that aren't in the default Ubuntu repositories.

## When to Use APT Repos

Use `aptRepos` when you need software from:

- Vendor-provided repositories (Docker, GitHub CLI, VS Code)
- PPAs (Personal Package Archives)
- Third-party package sources

## Basic Configuration

```yaml
install:
  aptRepos:
    - name: docker
      key_url: https://download.docker.com/linux/ubuntu/gpg
      repo: "deb https://download.docker.com/linux/ubuntu jammy stable"
      packages:
        - docker-ce
        - docker-ce-cli
        - containerd.io
```

## Required Fields

| Field | Description |
|-------|-------------|
| `name` | Identifier for the repository (used in filenames) |
| `key_url` | URL to the GPG signing key |
| `repo` | APT repository line |
| `packages` | List of packages to install from this repo |

## Finding Repository Information

Most software provides APT repository setup instructions. Look for:

1. **GPG Key URL** - Usually ends in `.gpg` or `.asc`
2. **Repository Line** - Starts with `deb` or `deb-src`
3. **Package Names** - What to install from the repo

### Example: Docker's Instructions

Docker's documentation shows:
```bash
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu jammy stable" | sudo tee /etc/apt/sources.list.d/docker.list
sudo apt-get update && sudo apt-get install docker-ce docker-ce-cli containerd.io
```

Translated to dottie:
```yaml
aptRepos:
  - name: docker
    key_url: https://download.docker.com/linux/ubuntu/gpg
    repo: "deb https://download.docker.com/linux/ubuntu jammy stable"
    packages:
      - docker-ce
      - docker-ce-cli
      - containerd.io
```

## Common Repositories

### Docker

```yaml
- name: docker
  key_url: https://download.docker.com/linux/ubuntu/gpg
  repo: "deb https://download.docker.com/linux/ubuntu jammy stable"
  packages:
    - docker-ce
    - docker-ce-cli
    - containerd.io
    - docker-buildx-plugin
    - docker-compose-plugin
```

### GitHub CLI

```yaml
- name: github-cli
  key_url: https://cli.github.com/packages/githubcli-archive-keyring.gpg
  repo: "deb https://cli.github.com/packages stable main"
  packages:
    - gh
```

### Visual Studio Code

```yaml
- name: vscode
  key_url: https://packages.microsoft.com/keys/microsoft.asc
  repo: "deb https://packages.microsoft.com/repos/code stable main"
  packages:
    - code
```

### Node.js (via NodeSource)

```yaml
- name: nodejs
  key_url: https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key
  repo: "deb https://deb.nodesource.com/node_20.x nodistro main"
  packages:
    - nodejs
```

### Kubernetes (kubectl)

```yaml
- name: kubernetes
  key_url: https://pkgs.k8s.io/core:/stable:/v1.29/deb/Release.key
  repo: "deb https://pkgs.k8s.io/core:/stable:/v1.29/deb/ /"
  packages:
    - kubectl
```

## Installation Order

dottie ensures repositories are added before their packages are installed:

1. APT repositories are configured
2. `apt-get update` runs
3. Packages from the repo are installed

This happens automatically for each `aptRepos` entry.

## Ubuntu Version Codenames

Many repos require the Ubuntu codename in the repo line. Instead of hardcoding it, use the `${VERSION_CODENAME}` variable so your config works across Ubuntu versions:

```yaml
repo: "deb [arch=${MS_ARCH}] https://download.docker.com/linux/${ID} ${VERSION_CODENAME} stable"
```

This automatically resolves to the correct values for your system (e.g., `noble` on 24.04, `jammy` on 22.04).

## Supported Variables

All architecture and OS release variables are available in `repo`, `key_url`, and `packages` fields:

| Variable | Example Value | Description |
|----------|--------------|-------------|
| `${VERSION_CODENAME}` | `noble` | Ubuntu/Debian release codename |
| `${MS_ARCH}` | `amd64` | Debian-style architecture |
| `${ID}` | `ubuntu` | OS identifier |
| `${ARCH}` | `x86_64` | Raw architecture |
| `${VERSION_ID}` | `24.04` | OS version number |
| `${ID_LIKE}` | `debian` | Parent distribution family |
| `${SIGNING_FILE}` | `/etc/apt/trusted.gpg.d/<name>.gpg` | GPG key path (deferred, resolved at install time) |

The `${SIGNING_FILE}` variable is a special **deferred variable** — it resolves at install time to the path where dottie stores the GPG key for the repository. Use it when a repository requires a `signed-by` clause:

```yaml
aptRepos:
  - name: typora
    key_url: https://typora.io/linux/public-key.asc
    repo: "deb [signed-by=${SIGNING_FILE}] https://downloads.typora.io/linux ./"
    packages:
      - typora
```

See the [Variables reference](../configuration/variables.md) for the full list of available variables.

## Idempotency

dottie checks if packages are already installed before adding repositories:

```
⊘ docker-ce Skipped (AptRepo) - Already installed
```

The repository is still configured, but packages aren't reinstalled.

## Troubleshooting

### GPG Key Issues

```
✗ docker Failed (AptRepo) - GPG key download failed
```

**Solution**: Verify the `key_url` is correct and accessible.

### Repository Not Found

```
✗ docker Failed (AptRepo) - Repository configuration failed
```

**Solution**: Check the `repo` line format and Ubuntu codename.

### Package Not Found

```
✗ docker-ce Failed (AptRepo) - Package not found
```

**Solution**: Verify the package name and that it's available in the repository.

## Complete Example

```yaml
install:
  aptRepos:
    # Docker - uses variables for portable config across Ubuntu versions and architectures
    - name: docker
      key_url: https://download.docker.com/linux/${ID}/gpg
      repo: "deb [arch=${MS_ARCH}] https://download.docker.com/linux/${ID} ${VERSION_CODENAME} stable"
      packages:
        - docker-ce
        - docker-ce-cli

    # GitHub CLI
    - name: github-cli
      key_url: https://cli.github.com/packages/githubcli-archive-keyring.gpg
      repo: "deb [arch=${MS_ARCH}] https://cli.github.com/packages stable main"
      packages:
        - gh

    # VS Code
    - name: vscode
      key_url: https://packages.microsoft.com/keys/microsoft.asc
      repo: "deb [arch=${MS_ARCH}] https://packages.microsoft.com/repos/code stable main"
      packages:
        - code
```
