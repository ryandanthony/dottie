# CLI Contract: `dottie install`

**Feature**: 007-cli-install
**Date**: February 1, 2026

## Command Signature

```
dottie install [OPTIONS]
```

## Options

| Option | Short | Type | Default | Description |
|--------|-------|------|---------|-------------|
| --profile | -p | string | "default" | Profile name to use |
| --config | -c | string | `<repo-root>/dottie.yaml` | Path to configuration file |
| --dry-run | | flag | false | Preview without making changes |
| --help | -h | flag | | Show help information |

## Exit Codes

| Code | Condition |
|------|-----------|
| 0 | All installations succeeded or were skipped |
| 1 | One or more installations failed, OR configuration error, OR profile not found |

## Output Format

### Standard Progress (stderr-style, to console)

```
[icon] [item-name] [status] ([source-type]) [- message]
```

Where:
- `icon`: ✓ (green), ✗ (red), ⊘ (yellow), ⚠ (yellow)
- `status`: Success, Failed, Skipped, Warning
- `source-type`: GithubRelease, AptPackage, AptRepo, Script, Font, SnapPackage
- `message`: Optional details (version, path, error reason)

### Summary (always displayed)

```
Installation Summary:
  ✓ Succeeded: [count]
  ✗ Failed: [count]
  ⊘ Skipped: [count]
  ⚠ Warnings: [count]
```

### Grouped Failures (when failures exist)

```
Failed Installations:
  [[source-type]] [item-name]: [error-message]
  ...
```

### Dry-Run Header

```
Dry Run Mode: Previewing installation without making changes
```

## Behavior Contract

### Profile Resolution

1. If `--profile` not specified, use "default"
2. Resolve full inheritance chain via ProfileMerger
3. If profile not found AND not "default", exit 1 with error
4. If "default" not defined, treat as empty profile (no error)

### Installation Order (Priority)

1. GitHub Releases
2. APT Packages  
3. APT Repositories
4. Scripts
5. Fonts
6. Snap Packages

All items in priority N complete before priority N+1 begins.

### Already-Installed Detection

| Source Type | Detection Method |
|-------------|------------------|
| GitHub Release | Check ~/bin/{binary}, then `which {binary}` |
| APT Package | `dpkg -s {package}` exit code |
| APT Repository | Check if packages already installed via dpkg |
| Script | N/A - scripts always run |
| Font | Check ~/.local/share/fonts/{name}/ exists |
| Snap | `snap list {name}` exit code |

### Dry-Run Behavior

- Performs all detection checks (network requests for validation, system state checks)
- Does NOT execute any installations
- Does NOT modify filesystem
- Shows accurate "would install" vs "would skip" status

### Error Handling

- Individual item failures do NOT stop processing
- All failures collected and reported in grouped summary
- Exit code is 1 if ANY item failed
- Configuration errors (invalid YAML, missing file) exit immediately

### Environment Variables

| Variable | Usage |
|----------|-------|
| GITHUB_TOKEN | Optional. Used for authenticated GitHub API requests (higher rate limits) |

## Error Messages

### Profile Not Found

```
Error: Profile 'xyz' not found.
Available profiles: default, work, personal
```

### Configuration File Not Found

```
Error: Could not find dottie.yaml in the repository root.
```

### GitHub Rate Limit

```
GitHub API rate limit exceeded. Set GITHUB_TOKEN environment variable for higher limits.
```

### GitHub Version Not Found

```
GitHub release not found: owner/repo@1.2.3 (HTTP 404)
```

### Sudo Required

```
sudo required for APT package installation. Skipping APT packages.
```

## Idempotency Guarantee

Running `dottie install` multiple times with the same configuration produces the same final state:

- Already-installed items are skipped (not reinstalled)
- No duplicate installations
- No side effects from repeated runs
- Exit code 0 on subsequent runs (assuming first run succeeded)
