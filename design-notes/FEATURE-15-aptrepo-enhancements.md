# FEATURE-15: APT Repo Enhancements

## Overview

Two enhancements to the `aptRepos` install source:

1. **DEB822 format support** — write `.sources` files in addition to one-line `.list` files
2. **`${SIGNING_FILE}` variable** — expose the path where the downloaded GPG key was written, so users can reference it in `signed-by=` clauses or `Signed-By` for DEB822 

---

## 1) DEB822 (.sources) Format Support

### Problem

The current implementation only writes a single-line entry to `/etc/apt/sources.list.d/<name>.list`:

```
deb [arch=amd64] https://packages.microsoft.com/ubuntu/noble/prod noble main
```

Modern Debian/Ubuntu (starting with Debian 12 Bookworm and Ubuntu 24.04 Noble) prefer the structured **DEB822** format in `/etc/apt/sources.list.d/<name>.sources`. Many upstream vendors already document their repos exclusively in this format. The `.list` one-line format is deprecated and may be removed in future releases.

### DEB822 Format Overview

The DEB822 `.sources` file is a structured, key-value format:

```
Types: deb
URIs: https://cli.github.com/packages
Suites: stable
Components: main
Architectures: amd64
Signed-By: /etc/apt/trusted.gpg.d/github-cli.gpg
```

Compared to the equivalent one-line `.list` format:

```
deb [arch=amd64 signed-by=/etc/apt/trusted.gpg.d/github-cli.gpg] https://cli.github.com/packages stable main
```

Key differences:

| Aspect | `.list` (one-line) | `.sources` (DEB822) |
|--------|-------------------|---------------------|
| File extension | `.list` | `.sources` |
| Format | Single line with `deb` prefix | Structured key-value pairs |
| `signed-by` | Inline in `[options]` | Dedicated `Signed-By:` field |
| Architectures | Inline `arch=` option | Dedicated `Architectures:` field |
| Multiple URIs | Separate lines | Space-separated in one `URIs:` field |
| Multiple suites | Separate entries | Space-separated in one `Suites:` field |
| Readability | Compact but dense | Clear and self-documenting |

### Proposed Configuration

Add an optional `format` field to `AptRepoItem` that controls the output format:

```yaml
profiles:
  default:
    install:
      aptRepos:
        # DEB822 format (modern)
        - name: github-cli
          format: deb822
          key_url: https://cli.github.com/packages/githubcli-archive-keyring.gpg
          sources: |
            Types: deb
            URIs: https://cli.github.com/packages
            Suites: stable
            Components: main
            Architectures: ${MS_ARCH}
            Signed-By: ${SIGNING_FILE}

        # Legacy one-line format (current behavior, remains the default)
        - name: docker
          key_url: https://download.docker.com/linux/ubuntu/gpg
          repo: "deb [arch=${MS_ARCH}] https://download.docker.com/linux/ubuntu ${VERSION_CODENAME} stable"
          packages:
            - docker-ce
            - docker-ce-cli

        # Legacy one-line format (current behavior, remains the default) with alias
        - name: docker
          key_url: https://download.docker.com/linux/ubuntu/gpg
          lists: "deb [arch=${MS_ARCH}] https://download.docker.com/linux/ubuntu ${VERSION_CODENAME} stable"
          packages:
            - docker-ce
            - docker-ce-cli            
```

### New `AptRepoItem` Fields for DEB822

| Field | Type | Required (deb822) | Description |
|-------|------|-------------------|-------------|
| `format` | `string` | No | `"deb822"` or `"list"` (default: `"list"` for backwards compatibility) |

When `format: deb822`, the existing `repo` / `lists` field is ignored — the `sources` field above are used instead.

### Generated Output — DEB822

For the `github-cli` example above, the installer would write `/etc/apt/sources.list.d/github-cli.sources`:

```
Types: deb
URIs: https://cli.github.com/packages
Suites: stable
Components: main
Architectures: amd64
Signed-By: /etc/apt/trusted.gpg.d/github-cli.gpg
```

### Generated Output — Legacy (unchanged)

For the `docker` example, the installer still writes `/etc/apt/sources.list.d/docker.list`:

```
deb [arch=amd64] https://download.docker.com/linux/ubuntu noble stable
```

### Validation Rules

- If `format: deb822`: `sources`, is **required**; `repo` or `lists` is **ignored**
- If `format: list` (or omitted): `repo` or `lists` is **required** (current behavior); DEB822 fields are ignored
- `format` must be one of `"deb822"` or `"list"` (case-insensitive)


## 2) `${SIGNING_FILE}` Variable for Key Path

### Problem

When adding an APT repository, the GPG key is downloaded and written to `/etc/apt/trusted.gpg.d/<name>.gpg`. Users who use the one-line `.list` format often need to reference this path in a `signed-by=` clause:

```
deb [arch=amd64 signed-by=/etc/apt/trusted.gpg.d/github-cli.gpg] https://cli.github.com/packages stable main
```

Today, users must hardcode this path — duplicating the `name` field and assuming the key directory. This is fragile and verbose.

### Proposed Solution

Introduce a new per-item variable `${SIGNING_FILE}` that resolves to the absolute path where the GPG key was (or will be) written:

```
/etc/apt/trusted.gpg.d/<name>.gpg
```

This variable is available **only** within `aptRepos` entries and is resolved at install time (after the key path is determined).

### Example Usage

```yaml
profiles:
  default:
    install:
      aptRepos:
        # GitHub CLI — uses ${SIGNING_FILE} in the repo line
        - name: github-cli
          key_url: https://cli.github.com/packages/githubcli-archive-keyring.gpg
          repo: "deb [arch=amd64 signed-by=${SIGNING_FILE}] https://cli.github.com/packages stable main"
          packages:
            - gh

        # Docker — same pattern
        - name: docker
          key_url: https://download.docker.com/linux/ubuntu/gpg
          repo: "deb [arch=${MS_ARCH} signed-by=${SIGNING_FILE}] https://download.docker.com/linux/ubuntu ${VERSION_CODENAME} stable"
          packages:
            - docker-ce
            - docker-ce-cli

        # GitHub CLI — uses ${SIGNING_FILE} in the sources line
        - name: github-cli
          format: deb822
          key_url: https://cli.github.com/packages/githubcli-archive-keyring.gpg
          sources: |
            Types: deb
            URIs: https://cli.github.com/packages
            Suites: stable
            Components: main
            Architectures: ${MS_ARCH}
            Signed-By: ${SIGNING_FILE}            
```

### Resolution Timing

`${SIGNING_FILE}` is a **deferred variable** — similar to how `${RELEASE_VERSION}` is deferred for GitHub release items:

1. During config load, `VariableResolver` resolves OS release and architecture variables but **skips** `${SIGNING_FILE}` (leaves it as a literal token)
2. At install time, `AptRepoInstaller.ConfigureRepositoryAsync` resolves `${SIGNING_FILE}` **per-item** after determining the key path from the item's `name`

This follows the same deferred-variable pattern established by `${RELEASE_VERSION}` in `GithubReleaseInstaller`.

### Implementation Notes

- Add `"SIGNING_FILE"` to a deferred-variables set for apt repo items in `VariableResolver` (similar to `GithubDeferredVariables`)
- In `AptRepoInstaller.ConfigureRepositoryAsync`, before calling `AddSourcesListAsync`, replace `${SIGNING_FILE}` in `repo.Repo` with the computed key path
- The key path format is already established: `/etc/apt/trusted.gpg.d/{repo.Name}.gpg`
- For DEB822 format, `${SIGNING_FILE}` is not needed — the `Signed-By` line is auto-injected

### Validation

- `${SIGNING_FILE}` is only valid inside `aptRepos` entries. If found elsewhere, it should be reported as an unresolvable variable
- Works with dry-run: the resolved path is predictable (derived from `name`), so it can be shown even without actually writing the key

---

## Combined Example

Both features work together — a config can mix legacy `.list` entries (using `${SIGNING_FILE}`) and modern DEB822 entries:

```yaml
profiles:
  default:
    install:
      aptRepos:
        # Modern DEB822 format — Signed-By is auto-injected
        - name: github-cli
          format: deb822
          key_url: https://cli.github.com/packages/githubcli-archive-keyring.gpg
          sources: |
            Types: deb
            URIs: https://cli.github.com/packages
            Suites: stable
            Components: main
            Architectures: ${MS_ARCH}
            Signed-By: ${SIGNING_FILE} 
          packages:
            - gh

        # Legacy format with ${SIGNING_FILE}
        - name: docker
          key_url: https://download.docker.com/linux/ubuntu/gpg
          repo: "deb [arch=${MS_ARCH} signed-by=${SIGNING_FILE}] https://download.docker.com/linux/ubuntu ${VERSION_CODENAME} stable"
          packages:
            - docker-ce
            - docker-ce-cli

        # Legacy format without signed-by (current behavior, still works)
        - name: hashicorp
          key_url: https://apt.releases.hashicorp.com/gpg
          repo: "deb [arch=${MS_ARCH}] https://apt.releases.hashicorp.com ${VERSION_CODENAME} main"
          packages:
            - terraform
```

---

## Backwards Compatibility

- **Default format is `list`** — no existing configs break
- **`${SIGNING_FILE}` is optional** — existing `repo` lines without it continue to work exactly as before
- **DEB822 fields are optional** — only required when `format: deb822` is set
- No changes to the YAML schema for users who don't opt in

## Migration Path

Users can migrate incrementally:

1. Add `signed-by=${SIGNING_FILE}` to existing `repo` lines (recommended best practice)
2. Switch individual entries to `format: deb822` when ready
3. No bulk migration required
