# Status Command Integration Test

This scenario tests the `dottie status` command functionality.

## Tests

1. **Default profile status** - Verify status command works without profile flag
2. **Dotfiles section** - Verify output includes dotfiles section header
3. **Software section** - Verify output includes software section header
4. **Work profile** - Test profile option with inherited profile
5. **Inheritance chain** - Verify inherited profile shows chain
6. **Non-existent profile** - Verify error handling for invalid profiles
7. **Minimal profile** - Test profile without inheritance

## Configuration

- `default` profile: 2 dotfiles + apt packages
- `work` profile: extends default, adds 1 dotfile + awscli
- `minimal` profile: 1 dotfile only, no inheritance

## Expected Behavior

- Status command always returns exit code 0 for successful execution
- Non-existent profile returns exit code 1
- Output shows both dotfiles and software sections
- Dotfile states shown: missing (not linked yet)
- Software states: missing (apt packages not installed in test)
