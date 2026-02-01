# Link Command Integration Test

This scenario tests the `dottie link` command functionality including:

1. **Dry-run mode**: Previews linking operations without making changes
2. **Profile selection**: Tests default and named profile resolution
3. **Output validation**: Verifies correct output format

## Files

- `dottie.yaml` - Configuration file with multiple profiles
- `dotfiles/` - Source files to be linked
  - `bashrc` - Bash configuration
  - `vimrc` - Vim configuration
  - `work-config` - Work-specific configuration

## Expected Behavior

### Dry-run Mode

```bash
dottie link --dry-run -c dottie.yaml
```

Should output:
- "Dry run - no changes will be made."
- List of files that would be linked
- No actual symlinks created

### Profile Selection

```bash
dottie link --dry-run -p work -c dottie.yaml
```

Should only show work profile dotfiles.
