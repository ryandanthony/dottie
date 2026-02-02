# FEATURE-02: Profiles

**STATUS**: Done

## Summary

Profiles allow different configurations for different machines/use cases.

## Features

- `extends: profile-name` — inherit from another profile
- Override or add to dotfiles/install lists
- Select profile with `--profile <name>` flag
- Default profile: `default`

## Use cases

- `work` vs `personal` machines
- `minimal` vs `full` setups
- `laptop` vs `server` configurations

## Resolution rules (proposed)

- A profile may `extends` another profile
- Effective config is computed by merging base + overrides
- Lists may be concatenated (or overridden); define exact semantics during implementation
