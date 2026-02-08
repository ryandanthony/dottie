# FEATURE-15: GitHub Release Asset Type

## Overview

Add a `type` field to GitHub release items that controls how the downloaded asset is installed. Today every asset is treated as either a standalone binary or an archive containing a binary. This enhancement adds support for `.deb` packages (installed via `dpkg`) and leaves room for future types (`.rpm`, `.AppImage`, etc.).

---

## Problem

Some GitHub projects publish `.deb` packages as their primary release artifact (e.g., `jgraph/drawio-desktop`). Currently there is no way to install these — the installer tries to extract a binary from the asset, which fails for `.deb` files.

Users must fall back to shell scripts for these packages, losing the benefits of version tracking, idempotency checks, and dry-run validation that the GitHub release installer provides.

---

## Proposed Solution

Add an optional `type` field to `GithubReleaseItem`:

| Value | Behavior | Default |
|-------|----------|---------|
| `binary` | Current behavior — extract/copy binary to `~/bin/`, `chmod +x` | **Yes** (default when `type` is omitted) |
| `deb` | Install `.deb` package via `sudo dpkg -i`, then `sudo apt-get install -f` to resolve deps | No |

---

## Configuration Examples

### Type: `deb`

```yaml
github:
  - repo: jgraph/drawio-desktop
    asset: "drawio-arm64-*.deb"
    type: deb
```

Note: `binary` field is **not required** when `type: deb` — the package manager handles installation.

### Type: `binary` (default, current behavior)

```yaml
github:
  - repo: jqlang/jq
    asset: "jq-linux-amd64"
    binary: jq
    type: binary
```

Equivalent to today's behavior when `type` is omitted:

```yaml
github:
  - repo: jqlang/jq
    asset: "jq-linux-amd64"
    binary: jq
```

### With variables

```yaml
github:
  - repo: jgraph/drawio-desktop
    asset: "drawio-${MS_ARCH}-${RELEASE_VERSION}.deb"
    type: deb
    version: "29.3.6"
```

---

## Installer Behavior by Type

### `type: binary` (default)

No changes to current behavior:

1. Download asset from GitHub release
2. If archive (`.tar.gz`, `.tgz`, `.zip`): extract, find `binary` in extracted files
3. If standalone file: use directly as the binary
4. Copy to `~/bin/<binary>`, `chmod +x`

### `type: deb`

New installation path:

1. Download `.deb` asset from GitHub release
2. Validate the file looks like a `.deb` (check file extension or magic bytes)
3. Run `sudo dpkg -i <path-to-deb>`
4. Run `sudo apt-get install -f -y` to resolve any missing dependencies
5. Clean up temp files

#### Idempotency

For `type: deb`, idempotency is checked differently from binaries:

- **Binary type**: checks `~/bin/<binary>` and `PATH` for the binary name
- **Deb type**: use `dpkg -s <package-name>` to check if already installed
  - The package name can be extracted from the `.deb` filename or from `dpkg-deb --showformat='${Package}' -W <file>` after download
  - Alternatively, derive from the `repo` name or add an optional `package` field

#### Dry Run

For `type: deb` with `--dry-run`:

- Validate the GitHub release and asset exist (same as today)
- Report: `"github-release: drawio-desktop would be installed via dpkg"`
- Do **not** download or install

#### Sudo Requirement

`type: deb` requires sudo for `dpkg -i`. If `context.HasSudo` is false:

- Return a warning: `"Sudo required to install .deb packages"`
- Same pattern as `AptRepoInstaller` when sudo is unavailable

---

## Future Types (Out of Scope)

These are **not** part of this feature but the `type` field is designed to accommodate them later:

| Type | Description | Install mechanism |
|------|-------------|-------------------|
| `rpm` | Red Hat / Fedora packages | `sudo rpm -i` or `sudo dnf install` |
| `appimage` | Portable Linux apps | Copy to `~/bin/`, `chmod +x`, optional desktop integration |
| `dmg` | macOS disk images | Mount, copy `.app` to `/Applications` |
| `msi` | Windows installers | `msiexec /i` |

---

## Backwards Compatibility

- `type` defaults to `"binary"` when omitted — **zero changes** to existing configs
- `binary` field remains `required` for `type: binary` (current validation unchanged)
- `binary` field becomes optional only when `type: deb` is specified

---

## Error Cases

| Scenario | Behavior |
|----------|----------|
| `type: deb` but asset is not a `.deb` file | Fail: `"Asset does not appear to be a .deb package"` |
| `type: deb` without sudo | Warning: `"Sudo required to install .deb packages"` |
| `dpkg -i` fails (missing deps) | `apt-get install -f` attempts to fix; if still fails, report error |
| `type: deb` on non-Debian system | Fail: `"dpkg is not available on this system"` |
| Unknown `type` value | Fail: `"Unsupported asset type: <value>"` |
| `type: binary` without `binary` field | Fail: validation error (existing behavior) |
| `type: deb` with `binary` field set | Ignored — `binary` is not used for deb installation |
