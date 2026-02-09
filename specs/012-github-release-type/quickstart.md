# Quickstart: GitHub Release Asset Type

**Feature**: 012-github-release-type
**Date**: 2026-02-08

## What Changed

GitHub release entries in your dottie configuration now support a `type` field that controls how downloaded assets are installed. Previously, all assets were treated as binaries (extracted from archives or copied directly to `~/bin/`). Now you can install `.deb` packages directly.

## How to Use

### Install a .deb package from a GitHub release

Add a GitHub release entry with `type: deb`:

```yaml
profiles:
  my-profile:
    install:
      github:
        - repo: jgraph/drawio-desktop
          asset: "drawio-arm64-*.deb"
          type: deb
```

Then run your normal install command. The system will:

1. Download the matching `.deb` asset from the latest release
2. Install it using `dpkg` with automatic dependency resolution
3. On subsequent runs, skip the installation if the package is already present

### Pin to a specific version

```yaml
github:
  - repo: jgraph/drawio-desktop
    asset: "drawio-arm64-*.deb"
    type: deb
    version: "v29.3.6"
```

### Dry run

Use `--dry-run` to preview what would happen without installing anything:

```
dottie install --profile my-profile --dry-run
```

Output will indicate: "drawio-desktop: would be installed via dpkg"

## Nothing Changes for Existing Configs

If you don't use the `type` field, everything works exactly as before. The `type` defaults to `binary`, which is the existing behavior.

```yaml
# These two are identical:
github:
  - repo: jqlang/jq
    asset: "jq-linux-amd64"
    binary: jq

github:
  - repo: jqlang/jq
    asset: "jq-linux-amd64"
    binary: jq
    type: binary
```

## Requirements

- **`type: deb`** requires:
  - A Debian-based system with `dpkg` available
  - `sudo` access (same requirement as apt repository installs)
- **`type: binary`** (default): No additional requirements beyond existing behavior

## Key Differences: binary vs deb

| Aspect | `type: binary` | `type: deb` |
|--------|----------------|-------------|
| `binary` field | Required | Not needed |
| Install location | `~/bin/<binary>` | System paths (managed by dpkg) |
| Idempotency check | Binary exists in PATH | `dpkg -s <package>` |
| Sudo required | No | Yes |
| Dependency resolution | N/A | Automatic via `apt-get install -f` |
