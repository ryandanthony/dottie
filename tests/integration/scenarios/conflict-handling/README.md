# Conflict Handling Scenario

## Purpose

Test that dottie correctly handles conflicts when target files already exist.

## Scenarios Tested

1. **Conflict Detection**: When target files exist, `dottie link` should fail with exit code 1 and list all conflicts
2. **Safe Behavior**: Without `--force`, no files should be modified
3. **Force Link with Backup**: `dottie link --force` should:
   - Create backups of conflicting files with timestamp naming (`*.backup.YYYYMMDD-HHMMSS`)
   - Create symlinks to the source files
   - Display the backup path mappings
4. **Idempotent Operation**: Running `dottie link` again on already-linked files should succeed (skip operation)

## Files

- `dottie.yaml` - Configuration with dotfile mappings
- `bashrc` - Test bashrc file
- `vimrc` - Test vimrc file  
- `config/nvim/init.lua` - Test neovim config

## Expected Behavior

```bash
# First run - should fail due to conflicts
$ dottie link
Error: Conflicting files detected. Use --force to backup and overwrite.
Conflicts:
  • ~/.test-bashrc (file)
  • ~/.test-vimrc (file)
  • ~/.config/test-nvim/init.lua (file)
Found 3 conflict(s).
$ echo $?
1

# Force link - should backup and create symlinks
$ dottie link --force
Backed up 3 file(s):
  • ~/.test-bashrc → ~/.test-bashrc.backup.20240115-103000
  • ~/.test-vimrc → ~/.test-vimrc.backup.20240115-103000
  • ~/.config/test-nvim/init.lua → ~/.config/test-nvim/init.lua.backup.20240115-103000

✓ Created 3 symlink(s).
$ echo $?
0

# Re-run - should skip (already linked)
$ dottie link
Skipped 3 file(s) (already linked).
$ echo $?
0
```

## Related Features

- FEATURE-04: Conflict Handling
- FEATURE-05: CLI Link Command
