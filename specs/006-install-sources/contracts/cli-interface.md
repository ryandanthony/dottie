# CLI Interface Contract: Install Command

**Feature**: 006-install-sources  
**Date**: January 31, 2026

## Command Signature

```
dottie install [OPTIONS]
```

## Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--profile`, `-p` | string | "default" | Profile to install from |
| `--config`, `-c` | string | dottie.yaml in repo root | Path to configuration file |
| `--dry-run` | flag | false | Preview what would be installed |

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All installations successful (or skipped with warnings) |
| 1 | One or more installations failed |

## Output Format

### Normal Mode

```
Checking prerequisites...
  ✓ Running as user (sudo available)
  ✓ GITHUB_TOKEN configured

Installing from GitHub Releases...
  ⠋ Downloading fzf from junegunn/fzf...
  ✓ fzf v0.54.0 → ~/bin/fzf
  ✓ ripgrep v14.1.0 → ~/bin/rg

Installing APT packages...
  ⠋ Running apt-get update...
  ✓ Installed: curl, wget, git

Installing from private APT repos...
  ⠋ Adding Docker repository...
  ✓ docker-ce installed

Running scripts...
  ⠋ Executing scripts/install-nvm.sh...
  ✓ scripts/install-nvm.sh completed

Installing fonts...
  ⠋ Downloading JetBrains Mono...
  ✓ JetBrains Mono → ~/.local/share/fonts/JetBrainsMono/
  ⠋ Refreshing font cache...

Installing snap packages...
  ⠋ Installing code...
  ✓ code (classic) installed

╭────────────────────────────────────────────────────╮
│                 Installation Summary               │
├──────────────────┬─────────┬──────────┬───────────┤
│ Source           │ Success │ Warnings │ Failed    │
├──────────────────┼─────────┼──────────┼───────────┤
│ GitHub Releases  │ 2       │ 0        │ 0         │
│ APT Packages     │ 3       │ 0        │ 0         │
│ Private APT Repos│ 1       │ 0        │ 0         │
│ Scripts          │ 1       │ 0        │ 0         │
│ Fonts            │ 1       │ 0        │ 0         │
│ Snap Packages    │ 1       │ 0        │ 0         │
├──────────────────┼─────────┼──────────┼───────────┤
│ Total            │ 9       │ 0        │ 0         │
╰──────────────────┴─────────┴──────────┴───────────╯
```

### Dry Run Mode

```
Checking prerequisites...
  ✓ Running as user (sudo available)
  ⚠ GITHUB_TOKEN not configured (rate limits may apply)

[DRY RUN] Would install from GitHub Releases:
  • junegunn/fzf → ~/bin/fzf
  • BurntSushi/ripgrep → ~/bin/rg

[DRY RUN] Would install APT packages:
  • curl
  • wget
  • git

[DRY RUN] Would add private APT repos:
  • docker (https://download.docker.com/linux/ubuntu)
    Packages: docker-ce, docker-ce-cli

[DRY RUN] Would run scripts:
  • scripts/install-nvm.sh

[DRY RUN] Would install fonts:
  • JetBrains Mono → ~/.local/share/fonts/JetBrainsMono/

[DRY RUN] Would install snap packages:
  • code (classic)
```

### Without Sudo

```
Checking prerequisites...
  ⚠ Running as user (sudo NOT available)
  ⚠ APT packages, private APT repos, and snap packages will be skipped

Installing from GitHub Releases...
  ✓ fzf v0.54.0 → ~/bin/fzf

⚠ Skipping APT packages (sudo required)
⚠ Skipping private APT repos (sudo required)

Running scripts...
  ✓ scripts/install-nvm.sh completed

Installing fonts...
  ✓ JetBrains Mono → ~/.local/share/fonts/JetBrainsMono/

⚠ Skipping snap packages (sudo required)

╭────────────────────────────────────────────────────╮
│                 Installation Summary               │
├──────────────────┬─────────┬──────────┬───────────┤
│ Source           │ Success │ Warnings │ Failed    │
├──────────────────┼─────────┼──────────┼───────────┤
│ GitHub Releases  │ 1       │ 0        │ 0         │
│ APT Packages     │ 0       │ 3        │ 0         │
│ Private APT Repos│ 0       │ 1        │ 0         │
│ Scripts          │ 1       │ 0        │ 0         │
│ Fonts            │ 1       │ 0        │ 0         │
│ Snap Packages    │ 0       │ 1        │ 0         │
├──────────────────┼─────────┼──────────┼───────────┤
│ Total            │ 3       │ 5        │ 0         │
╰──────────────────┴─────────┴──────────┴───────────╯

⚠ Some installations were skipped. Run with sudo for full installation.
```

### Error Cases

#### Configuration Error
```
Error: Could not find dottie.yaml in the repository root.
Hint: Run 'dottie init' to create a configuration file.
```

#### Profile Not Found
```
Error: Profile 'work' not found in configuration.
Available profiles: default, home
```

#### No Install Block
```
Profile 'default' has no install block defined.
Nothing to install.
```

#### GitHub Rate Limit
```
Installing from GitHub Releases...
  ✗ junegunn/fzf: GitHub API rate limit exceeded

Hint: Set GITHUB_TOKEN environment variable for higher rate limits.
      See: https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token
```

#### Network Failure
```
Installing from GitHub Releases...
  ⚠ junegunn/fzf: Download failed after 3 retries (network timeout)

Continuing with remaining installations...
```

#### Script Failure
```
Running scripts...
  ✗ scripts/install-nvm.sh: Exit code 1

Script output:
  curl: (7) Failed to connect to raw.githubusercontent.com
```

## Behavior Specifications

### Installation Order
Sources are processed in strict priority order:
1. GitHub Releases
2. APT Packages  
3. Private APT Repositories
4. Shell Scripts
5. Fonts
6. Snap Packages

### Retry Logic
- Network downloads retry up to 3 times
- Delays: 1s, 2s, 4s (exponential backoff)
- After all retries fail: warn and continue with next item

### Sudo Handling
- Check sudo availability at startup
- Warn user which sources will be skipped
- Continue with non-sudo sources
- Exit code 0 if only warnings (no failures)

### Idempotency
- GitHub releases: Skip if binary exists in ~/bin/ with same version
- APT packages: apt-get handles idempotency
- Scripts: Always run (script is responsible for idempotency)
- Fonts: Skip if font directory exists
- Snaps: snap handles idempotency
