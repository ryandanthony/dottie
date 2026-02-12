---
title: Variables
sidebar_position: 5
description: Dynamic variable substitution in configuration values
---

# Variables

dottie supports dynamic variable substitution using the `${VARIABLE_NAME}` syntax. Variables are resolved at configuration load time, allowing your `dottie.yml` to adapt to different systems without manual editing.

## Syntax

```
${VARIABLE_NAME}
```

Variable names must start with a letter or underscore and contain only letters, digits, and underscores.

## Architecture Variables

These built-in variables detect the current system architecture:

| Variable | x86_64 | arm64 | arm32 | Description |
|----------|--------|-------|-------|-------------|
| `${ARCH}` | `x86_64` | `aarch64` | `armv7l` | Raw architecture (matches `uname -m` output) |
| `${MS_ARCH}` | `amd64` | `arm64` | `armhf` | Debian/Microsoft-style architecture label |

### When to use which

- **`${ARCH}`** — Use for GitHub release assets that follow Linux naming conventions (e.g., `x86_64`, `aarch64`)
- **`${MS_ARCH}`** — Use for APT repository lines and `.deb` packages that use Debian architecture names (e.g., `amd64`, `arm64`)

### Examples

```yaml
install:
  github:
    - repo: BurntSushi/ripgrep
      asset: ripgrep-${ARCH}-unknown-linux-musl.tar.gz
      binary: rg

  aptRepos:
    - name: docker
      key_url: https://download.docker.com/linux/ubuntu/gpg
      repo: "deb [arch=${MS_ARCH}] https://download.docker.com/linux/ubuntu ${VERSION_CODENAME} stable"
      packages:
        - docker-ce
```

## OS Release Variables

All key-value pairs from `/etc/os-release` are automatically available as variables. Common ones include:

| Variable | Example Value | Description |
|----------|--------------|-------------|
| `${VERSION_CODENAME}` | `noble` | Ubuntu/Debian release codename |
| `${VERSION_ID}` | `24.04` | OS version number |
| `${ID}` | `ubuntu` | OS identifier (e.g., `ubuntu`, `debian`, `fedora`) |
| `${ID_LIKE}` | `debian` | Parent distribution family |
| `${NAME}` | `Ubuntu` | OS name |
| `${PRETTY_NAME}` | `Ubuntu 24.04.1 LTS` | Human-readable OS name |
| `${UBUNTU_CODENAME}` | `noble` | Ubuntu-specific codename |

:::tip
Run `cat /etc/os-release` on your system to see all available variables.
:::

### Example

```yaml
install:
  aptRepos:
    - name: vscode
      key_url: https://packages.microsoft.com/keys/microsoft.asc
      repo: "deb [arch=${MS_ARCH}] https://packages.microsoft.com/repos/vscode stable main"
      packages:
        - code

    - name: docker
      key_url: https://download.docker.com/linux/${ID}/gpg
      repo: "deb [arch=${MS_ARCH}] https://download.docker.com/linux/${ID} ${VERSION_CODENAME} stable"
      packages:
        - docker-ce
```

## Deferred Variables

Some variables are resolved later during installation rather than at configuration load time:

| Variable | Resolved during | Description |
|----------|----------------|-------------|
| `${RELEASE_VERSION}` | GitHub release install | The resolved release version (latest or pinned), with leading `v` stripped |

### Example

```yaml
install:
  github:
    - repo: sharkdp/fd
      asset: fd-v${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
      binary: fd

    - repo: junegunn/fzf
      asset: fzf-${RELEASE_VERSION}-linux_amd64.tar.gz
      binary: fzf
      version: 0.44.0  # RELEASE_VERSION will be "0.44.0"
```

When `version` is specified, `${RELEASE_VERSION}` resolves to that value. Otherwise, it resolves to the latest release tag from GitHub (with any leading `v` removed).

## Where Variables Can Be Used

| Config Section | Fields | Supported Variables |
|---------------|--------|-------------------|
| `dotfiles` | `source`, `target` | Architecture, OS release |
| `install.github` | `asset`, `binary` | Architecture, OS release, `${RELEASE_VERSION}` (deferred) |
| `install.aptRepos` | `repo`, `key_url`, `packages` | Architecture, OS release |
| `install.apt` | — | Not currently supported |
| `install.scripts` | — | Not currently supported |
| `install.fonts` | — | Not currently supported |
| `install.snaps` | — | Not currently supported |

## Resolution Order

Variables are resolved in this order:

1. **OS release variables** — parsed from `/etc/os-release`
2. **Architecture variables** — `${ARCH}` and `${MS_ARCH}` override any conflicting OS release keys
3. **Deferred variables** — `${RELEASE_VERSION}` is left as-is during config loading and resolved per-item during installation

## Error Handling

If a variable reference cannot be resolved (e.g., `${NONEXISTENT}`), dottie reports an error during validation:

```
Unresolvable variable '${NONEXISTENT}' in profile 'default', entry 'docker', field 'repo'
```

Deferred variables like `${RELEASE_VERSION}` are allowed to remain unresolved at config load time without error.

## Complete Example

```yaml
profiles:
  default:
    dotfiles:
      - source: config/bashrc
        target: ~/.bashrc

    install:
      apt:
        - git
        - curl

      aptRepos:
        - name: docker
          key_url: https://download.docker.com/linux/${ID}/gpg
          repo: "deb [arch=${MS_ARCH}] https://download.docker.com/linux/${ID} ${VERSION_CODENAME} stable"
          packages:
            - docker-ce
            - docker-ce-cli

      github:
        - repo: BurntSushi/ripgrep
          asset: ripgrep-${RELEASE_VERSION}-${ARCH}-unknown-linux-musl.tar.gz
          binary: rg

        - repo: jdx/mise
          asset: mise-v${RELEASE_VERSION}-linux-${ARCH}.tar.gz
          binary: mise

        - repo: jgraph/drawio-desktop
          asset: drawio-${MS_ARCH}-${RELEASE_VERSION}.deb
          type: deb
```
