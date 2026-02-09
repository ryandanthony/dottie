# YAML Configuration Contract: GitHub Release Asset Type

**Feature**: 012-github-release-type
**Date**: 2026-02-08

## Schema

### `github` Array Item

```yaml
# Full schema with all fields
github:
  - repo: string          # REQUIRED — GitHub repository in "owner/name" format
    asset: string          # REQUIRED — Glob pattern to match release asset filename
    binary: string         # REQUIRED when type is "binary" (or omitted); OPTIONAL otherwise
    type: string           # OPTIONAL — "binary" (default) or "deb"
    version: string        # OPTIONAL — Specific release tag; defaults to latest
```

### Type Field Values

| Value | Description | Binary field |
|-------|-------------|--------------|
| `binary` | Extract/copy binary to ~/bin/, chmod +x. **Default when `type` is omitted.** | Required |
| `deb` | Install .deb package via dpkg, resolve dependencies via apt-get | Optional (ignored if present) |

### Examples

#### Install a .deb package

```yaml
profiles:
  desktop:
    install:
      github:
        - repo: jgraph/drawio-desktop
          asset: "drawio-arm64-*.deb"
          type: deb
```

#### Install a .deb package with pinned version

```yaml
profiles:
  desktop:
    install:
      github:
        - repo: jgraph/drawio-desktop
          asset: "drawio-arm64-*.deb"
          type: deb
          version: "v29.3.6"
```

#### Install a .deb package with variables

```yaml
profiles:
  desktop:
    install:
      github:
        - repo: jgraph/drawio-desktop
          asset: "drawio-${MS_ARCH}-${RELEASE_VERSION}.deb"
          type: deb
```

#### Explicit binary type (same as omitting type)

```yaml
profiles:
  tools:
    install:
      github:
        - repo: jqlang/jq
          asset: "jq-linux-amd64"
          binary: jq
          type: binary
```

#### Backward compatible (type omitted, defaults to binary)

```yaml
profiles:
  tools:
    install:
      github:
        - repo: jqlang/jq
          asset: "jq-linux-amd64"
          binary: jq
```

### Validation Rules

| Rule | Condition | Error message |
|------|-----------|---------------|
| `repo` required | Always | "GitHub release must have a 'repo' field (format: owner/repo)" |
| `asset` required | Always | "GitHub release must have an 'asset' pattern field" |
| `binary` required | `type` is `binary` or omitted | "GitHub release must have a 'binary' name field" |
| `binary` optional | `type` is `deb` | No error — field is ignored if present |
| Unknown `type` | Value not in enum | "Unsupported asset type: \<value\>" (caught and transformed from YamlDotNet deserialization error) |

### Profile Inheritance

The `type` field participates in profile inheritance like all other `GithubReleaseItem` fields. When a child profile overrides a parent's GitHub entry (matched by `repo`), the entire item is replaced — including the `type` field.

```yaml
profiles:
  base:
    install:
      github:
        - repo: jgraph/drawio-desktop
          asset: "drawio-amd64-*.deb"
          type: deb

  arm-desktop:
    extends: base
    install:
      github:
        - repo: jgraph/drawio-desktop
          asset: "drawio-arm64-*.deb"
          type: deb
```

In this example, `arm-desktop` overrides the `base` entry for `jgraph/drawio-desktop` entirely (different asset pattern for ARM architecture).
