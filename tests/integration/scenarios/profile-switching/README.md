# Profile Switching Scenario

## Purpose
Test that dottie correctly handles switching between different profiles and maintains proper state.

## What is tested
1. **Default profile** - Links files for default profile
2. **Profile switching** - Links files for alternate profile
3. **Profile isolation** - Only selected profile files are linked
4. **State transitions** - Moving from one profile to another maintains consistency
5. **Profile-specific behavior** - Each profile's mappings are respected

## Expected behavior
1. Apply default profile creates set A of symlinks
2. Apply work profile creates/updates symlinks for set B
3. Only files in selected profile are handled
4. Previous symlinks aren't removed when switching profiles
5. Running with different profiles sequentially produces cumulative correct state

## Rationale
Users may have multiple profiles (personal, work, minimal) and switch between them.
This tests that profile switching is safe and predictable.
