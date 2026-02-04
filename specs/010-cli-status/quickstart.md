# Quickstart: CLI Command `status`

**Feature**: 010-cli-status
**Date**: February 3, 2026

## Overview

The `dottie status` command displays the current state of your dotfiles and software installations. It shows which dotfiles are linked, missing, broken, or conflicting, and which software is installed, missing, or outdated—helping you understand what actions you need to take.

## Basic Usage

### Check Status (Default Profile)

```bash
dottie status
```

Displays the status of all dotfiles and software defined in the "default" profile.

### Check Specific Profile

```bash
dottie status --profile work
```

Displays status for the "work" profile, including items inherited from parent profiles.

### Specify Config File

```bash
dottie status --config ~/dotfiles/dottie.yaml
```

## Example Output

```text
Profile: work (inherited from: default)

═══ Dotfiles ═══
┌─────────────────────┬────────────┬──────────────────────────┐
│ Source              │ Status     │ Target                   │
├─────────────────────┼────────────┼──────────────────────────┤
│ dotfiles/bashrc     │ ✓ Linked   │ ~/.bashrc                │
│ dotfiles/vimrc      │ ○ Missing  │ ~/.vimrc                 │
│ dotfiles/config     │ ✗ Conflict │ ~/.config (existing dir) │
│ dotfiles/old        │ ⚠ Broken   │ ~/.old (target missing)  │
└─────────────────────┴────────────┴──────────────────────────┘

═══ Software ═══
┌──────────────────┬────────────────┬─────────────────────────┐
│ Item             │ Status         │ Details                 │
├──────────────────┼────────────────┼─────────────────────────┤
│ [GitHub] ripgrep │ ✓ Installed    │ ~/bin/rg                │
│ [GitHub] fd      │ ⬆ Outdated     │ 8.7.0 → 9.0.0           │
│ [APT] git        │ ✓ Installed    │                         │
│ [APT] htop       │ ○ Missing      │                         │
│ [Font] JetBrains │ ✓ Installed    │                         │
└──────────────────┴────────────────┴─────────────────────────┘

Summary: 1 linked, 1 missing, 1 conflict, 1 broken | 3 installed, 1 missing, 1 outdated
```

## Status Indicators

### Dotfile States

| Status | Symbol | Meaning | Action |
|--------|--------|---------|--------|
| Linked | ✓ | Symlink exists and points to correct source | None needed |
| Missing | ○ | Target doesn't exist yet | Run `dottie link` |
| Conflict | ✗ | File/directory exists but isn't the expected symlink | Run `dottie link --force` to backup and replace |
| Broken | ⚠ | Symlink exists but source file is missing | Check source file or remove stale link |
| Unknown | ? | Cannot determine state (permission issue) | Check permissions |

### Software States

| Status | Symbol | Meaning | Action |
|--------|--------|---------|--------|
| Installed | ✓ | Software is installed (and version matches if pinned) | None needed |
| Missing | ○ | Software is not installed | Run `dottie install` |
| Outdated | ⬆ | Installed version differs from pinned version | Run `dottie install` to update |
| Unknown | ? | Cannot determine installation state | Check manually |

## Configuration Example

```yaml
# dottie.yaml
profiles:
  default:
    dotfiles:
      - source: dotfiles/bashrc
        target: ~/.bashrc
      - source: dotfiles/vimrc
        target: ~/.vimrc
      - source: dotfiles/config/nvim
        target: ~/.config/nvim

    install:
      github:
        - repo: BurntSushi/ripgrep
          asset: ripgrep-*-x86_64-unknown-linux-musl.tar.gz
          binary: rg
        - repo: sharkdp/fd
          asset: fd-*-x86_64-unknown-linux-musl.tar.gz
          binary: fd
          version: "9.0.0"  # Pinned version enables "outdated" detection
      
      apt:
        - git
        - curl
        - htop
      
      fonts:
        - name: JetBrainsMono
          url: https://github.com/JetBrains/JetBrainsMono/releases/download/v2.304/JetBrainsMono-2.304.zip
          files:
            - "*.ttf"

  work:
    extends: default
    dotfiles:
      - source: dotfiles/work-gitconfig
        target: ~/.gitconfig
    
    install:
      apt:
        - docker-ce
```

## Common Scenarios

### After Fresh Clone

```bash
cd ~/dotfiles
dottie status
```

Shows all dotfiles as "Missing" and software as "Missing" - a clean slate ready for setup.

### After Running `dottie link`

```bash
dottie status
```

Should show all dotfiles as "Linked" (assuming no conflicts).

### Before Updating Configuration

```bash
dottie status --profile work
```

Check current state before making changes to understand what's already configured.

### Troubleshooting Broken Links

If you see "Broken" status, the symlink exists but points to a missing source file:

```bash
# Check what the symlink points to
ls -la ~/.bashrc

# Verify source exists
ls -la ~/dotfiles/dotfiles/bashrc

# If source was moved/renamed, update dottie.yaml and re-link
dottie link --force
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Status check completed successfully (regardless of item states) |
| 1 | Error loading configuration or invalid profile |

The status command always returns 0 if it successfully displays the status, even if items are missing, broken, or conflicting. This is intentional—the command is informational, and "problems" are expected states that require user action.

## Integration with Other Commands

The status command is designed to be used in a workflow:

1. **Check status** to understand current state: `dottie status`
2. **Link dotfiles** to create symlinks: `dottie link`
3. **Install software** to set up tools: `dottie install`
4. **Check status again** to verify: `dottie status`

## Tips

- Run `dottie status` before and after `dottie link` or `dottie install` to see changes
- Use `--profile` to check specific environments (work, home, server)
- "Outdated" detection only works for GitHub releases with pinned versions
- Permission errors show as "Unknown" - check that you have read access to target directories
