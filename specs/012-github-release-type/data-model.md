# Data Model: GitHub Release Asset Type

**Feature**: 012-github-release-type
**Date**: 2026-02-08

## Entities

### GithubReleaseAssetType (NEW)

An enumeration that controls the installation pathway for a downloaded GitHub release asset.

| Value | Description | Default |
|-------|-------------|---------|
| `Binary` | Current behavior — extract/copy binary to bin directory, make executable | Yes (default when `type` is omitted) |
| `Deb` | Install `.deb` package via `dpkg -i`, resolve dependencies via `apt-get install -f` | No |

**Design notes**:
- Enum with integer backing values for future extensibility (e.g., `Rpm = 2`, `AppImage = 3`)
- YamlDotNet maps YAML lowercase values (`binary`, `deb`) to PascalCase enum members automatically via `CamelCaseNamingConvention`

### GithubReleaseItem (MODIFIED)

A configuration entry describing a software asset to install from a GitHub release.

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `Repo` | `string` | Always | — | Repository in `owner/name` format |
| `Asset` | `string` | Always | — | Glob pattern to match release asset filename |
| `Binary` | `string` | When `Type` = `Binary` | — | Name of the binary to extract |
| `Type` | `GithubReleaseAssetType` | No | `Binary` | Installation pathway for the asset |
| `Version` | `string?` | No | `null` (latest) | Specific release tag to download |

**Changes from current model**:
- `Binary` changes from `required` to conditionally required (required only when `Type` is `Binary`)
- `Type` is a new optional property with default value `Binary`

**Merge behavior** (profile inheritance): No changes. `MergeKey` remains `Repo`. Child profiles can override all fields including `Type`.

### Validation Rules (MODIFIED)

| Rule | Applies to | Current | New |
|------|-----------|---------|-----|
| `Repo` required | All types | ✅ | ✅ (unchanged) |
| `Asset` required | All types | ✅ | ✅ (unchanged) |
| `Binary` required | `type: binary` only | ✅ (always) | ✅ (conditional) |
| `Binary` ignored | `type: deb` | N/A | ✅ (new — no validation error if present, just unused) |
| `Type` value validation | All | N/A | ✅ (new — unknown values rejected by enum deserialization) |

### Installation Decision Matrix

| Type | HasSudo | dpkg available | DryRun | Action |
|------|---------|----------------|--------|--------|
| `binary` | any | any | `false` | Download → extract/copy → chmod +x (existing behavior) |
| `binary` | any | any | `true` | Validate release/asset exist → report (existing behavior) |
| `deb` | `true` | `true` | `false` | Download .deb → dpkg-deb -W → dpkg -s → dpkg -i → apt-get install -f |
| `deb` | `true` | `true` | `true` | Validate release/asset exist → report "would install via dpkg" |
| `deb` | `false` | any | any | Return `Warning`: "Sudo required to install .deb packages" |
| `deb` | `true` | `false` | `false` | Return `Failed`: "dpkg is not available on this system" |
| `deb` | `true` | `false` | `true` | Return `Failed`: "dpkg is not available on this system" |

### InstallResult Patterns for Deb Type

| Scenario | Status | Message Pattern |
|----------|--------|-----------------|
| Package already installed | `Skipped` | "{repo}: package '{package}' already installed" |
| Successful installation | `Success` | "{repo}: installed via dpkg" |
| Dry-run valid | `Skipped` | "{repo}: would be installed via dpkg" |
| No sudo | `Warning` | "Sudo required to install .deb packages" |
| No dpkg | `Failed` | "dpkg is not available on this system" |
| Asset not .deb | `Failed` | "Asset does not appear to be a .deb package" |
| dpkg -i failure | `Failed` | "dpkg installation failed: {error}" |
| Dependency resolution failure | `Failed` | "Dependency resolution failed: {error}" |

## State Transitions

### Deb Installation Lifecycle

```
[Start]
  │
  ├─ HasSudo = false → Warning("Sudo required") → [End]
  │
  ├─ dpkg unavailable → Failed("dpkg not available") → [End]
  │
  ├─ DryRun = true
  │   ├─ Release/asset valid → Skipped("would install") → [End]
  │   └─ Release/asset invalid → Failed("not found") → [End]
  │
  └─ DryRun = false
      │
      ├─ Download .deb
      │   ├─ Extract package name (dpkg-deb -W)
      │   ├─ dpkg -s → already installed → Skipped("already installed") → [Cleanup] → [End]
      │   └─ dpkg -s → not installed
      │       ├─ dpkg -i → success
      │       │   └─ apt-get install -f → Success("installed via dpkg") → [Cleanup] → [End]
      │       └─ dpkg -i → failure → Failed("installation failed") → [Cleanup] → [End]
      │
      └─ Download failed → Failed("download error") → [End]
```
