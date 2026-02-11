---
title: Profile Inheritance
sidebar_position: 1
description: How to use profile inheritance for configuration reuse
---

# Profile Inheritance Guide

Profile inheritance lets you build configurations that extend and customize base profiles, reducing duplication and enabling context-specific setups.

## Why Use Inheritance?

Instead of duplicating configuration across profiles:

```yaml
# ❌ Without inheritance - lots of duplication
profiles:
  home:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc
      - source: dotfiles/.gitconfig
        target: ~/.gitconfig
    install:
      apt: [git, curl, vim]

  work:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc
      - source: dotfiles/.gitconfig
        target: ~/.gitconfig
      - source: dotfiles/.work-config
        target: ~/.work-config
    install:
      apt: [git, curl, vim, docker.io]
```

Use inheritance to share common configuration:

```yaml
# ✅ With inheritance - clean and maintainable
profiles:
  base:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc
      - source: dotfiles/.gitconfig
        target: ~/.gitconfig
    install:
      apt: [git, curl, vim]

  home:
    extends: base
    # Inherits everything from base

  work:
    extends: base
    dotfiles:
      - source: dotfiles/.work-config
        target: ~/.work-config
    install:
      apt: [docker.io]  # Added to base's packages
```

## Basic Inheritance

Use the `extends` keyword to inherit from another profile:

```yaml
profiles:
  base:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc

  work:
    extends: base
```

The `work` profile now includes everything from `base`.

## Merging Behavior

### Dotfiles

Dotfiles from both profiles are combined:

```yaml
profiles:
  base:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc

  work:
    extends: base
    dotfiles:
      - source: dotfiles/.work-config
        target: ~/.work-config
```

Result: `work` has both `.bashrc` and `.work-config` entries.

### Install Blocks

Install blocks are merged per type:

```yaml
profiles:
  base:
    install:
      apt: [git, curl]
      github:
        - repo: BurntSushi/ripgrep
          asset: ripgrep-{arch}.tar.gz
          binary: rg

  work:
    extends: base
    install:
      apt: [docker.io]  # Added to [git, curl]
```

Result: `work` installs `git`, `curl`, `docker.io`, and `ripgrep`.

### Override Behavior

When the same target appears in both profiles, the child wins:

```yaml
profiles:
  base:
    dotfiles:
      - source: dotfiles/.gitconfig
        target: ~/.gitconfig

  work:
    extends: base
    dotfiles:
      - source: dotfiles/.work-gitconfig
        target: ~/.gitconfig  # Overrides base
```

Result: `work` uses `.work-gitconfig` for `~/.gitconfig`.

## Multi-Level Inheritance

Inheritance can chain multiple levels:

```yaml
profiles:
  base:
    install:
      apt: [git]

  developer:
    extends: base
    install:
      apt: [curl, vim]

  work:
    extends: developer  # Gets git, curl, vim
    install:
      apt: [docker.io]
```

Final result for `work`: `git`, `curl`, `vim`, `docker.io`

## Common Patterns

### Layered Configuration

```yaml
profiles:
  # Layer 1: Absolute minimum
  minimal:
    install:
      apt: [git]

  # Layer 2: Development basics
  developer:
    extends: minimal
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc
    install:
      apt: [curl, vim]

  # Layer 3: Full workstation
  workstation:
    extends: developer
    install:
      apt: [build-essential]
      github:
        - repo: BurntSushi/ripgrep
          asset: ripgrep-{arch}.tar.gz
          binary: rg
```

### Context-Specific Profiles

```yaml
profiles:
  base:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc

  home:
    extends: base
    install:
      apt: [steam, discord]

  work:
    extends: base
    install:
      apt: [docker.io, kubectl]
```

## Error Handling

### Circular References

dottie detects and prevents circular inheritance:

```yaml
profiles:
  a:
    extends: b
  b:
    extends: a  # Error!
```

Error message:
```
Configuration validation failed:
  • Circular profile inheritance detected: a → b → a
```

### Missing Parent

```yaml
profiles:
  work:
    extends: nonexistent
```

Error message:
```
Configuration validation failed:
  • Profile 'work' extends 'nonexistent', but 'nonexistent' does not exist.
```

## Best Practices

1. **Keep base profiles minimal** - Only include truly common configuration
2. **Use meaningful names** - `base`, `developer`, `work` tell a clear story
3. **Document inheritance** - Add comments explaining the chain
4. **Test each profile** - Validate all profiles, not just the ones you use
5. **Avoid deep chains** - 2-3 levels is usually sufficient
