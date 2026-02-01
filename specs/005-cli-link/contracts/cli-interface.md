# CLI Interface Contract: link command

**Feature**: 005-cli-link  
**Date**: January 31, 2026

## Command Signature

```
dottie link [OPTIONS]
```

## Options

| Option | Short | Type | Default | Description |
|--------|-------|------|---------|-------------|
| --profile | -p | string | "default" | Profile to use for linking |
| --config | -c | string | dottie.yaml | Path to configuration file |
| --force | -f | flag | false | Backup conflicts and overwrite |
| --dry-run | -d | flag | false | Preview without making changes |

## Flag Constraints

- `--force` and `--dry-run` are **mutually exclusive**
- If both specified, display error and exit with code 1

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | All operations completed successfully |
| 1 | One or more operations failed, or validation error |

## Output Behavior

### Normal Mode (no flags)

1. **Progress bar** displayed during linking
2. **Summary** displayed after completion:
   ```
   ✓ Created 5 symlink(s).
   Skipped 2 file(s) (already linked).
   ```
3. **Errors** displayed for failures:
   ```
   Failed to link 1 file(s):
     • ~/.config/nvim: Permission denied
   ```

### Dry Run Mode (--dry-run)

1. **No filesystem changes**
2. **Preview output** showing planned operations:
   ```
   Dry run - no changes will be made.
   
   Would create 5 symlink(s):
     • dotfiles/bashrc → ~/.bashrc
     • dotfiles/vimrc → ~/.vimrc
     ...
   
   Would skip 2 file(s) (already linked):
     • ~/.gitconfig
     ...
   
   Conflicts detected (use --force to resolve):
     • ~/.zshrc (existing file)
   ```

### Force Mode (--force)

1. **Backup output** before linking:
   ```
   Backed up 2 file(s):
     • ~/.bashrc → ~/.bashrc.dottie-backup-20260131-143052
     • ~/.vimrc → ~/.vimrc.dottie-backup-20260131-143052
   ```
2. **Normal linking output** follows

### Conflict Mode (conflicts without --force)

1. **Conflict list** displayed:
   ```
   Error: Conflicting files detected. Use --force to backup and overwrite.
   
   Conflicts:
     • ~/.bashrc
       Type: Existing file
     • ~/.config/nvim
       Type: Symlink pointing to /other/path
   
   Found 2 conflict(s).
   ```
2. **Exit code 1**

## Error Messages

### Configuration Errors

```
Error: Could not find dottie.yaml in the repository root.
Hint: Make sure you're running from within a git repository.
```

```
Error: Profile 'work' not found.
Available profiles: default, home, server
```

### Flag Validation Errors

```
Error: --dry-run and --force cannot be used together.
```

### Windows Symlink Permission Error

```
Error: Unable to create symbolic link - insufficient permissions.

On Windows, symbolic links require either:
  • Run dottie as Administrator, OR
  • Enable Developer Mode in Windows Settings > Update & Security > For developers
```

## Examples

```bash
# Link using default profile
dottie link

# Link using specific profile
dottie link --profile work
dottie link -p work

# Preview what would happen
dottie link --dry-run
dottie link -d

# Force link, backing up conflicts
dottie link --force
dottie link -f

# Combine profile and force
dottie link --profile server --force
dottie link -p server -f

# Use custom config path
dottie link --config ./custom-dottie.yaml
dottie link -c ./custom-dottie.yaml
```
