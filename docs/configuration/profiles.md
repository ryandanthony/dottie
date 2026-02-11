---
title: Profiles
sidebar_position: 2
description: Profile configuration and inheritance
---

# Profiles

Profiles are the top-level organizing structure in dottie. Each profile defines a complete configuration that can be applied independently.

## Defining Profiles

```yaml
profiles:
  default:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc
    install:
      apt: [git, curl]

  work:
    dotfiles:
      - source: dotfiles/.work-config
        target: ~/.work-config
    install:
      apt: [docker.io]
```

## Using Profiles

Specify a profile when running commands:

```bash
> dottie link --profile work
> dottie install -p work
```

If no profile is specified, `default` is used.

## Profile Inheritance

Profiles can extend other profiles using the `extends` keyword:

```yaml
profiles:
  base:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc
    install:
      apt: [git, curl, vim]

  work:
    extends: base
    dotfiles:
      - source: dotfiles/.work-gitconfig
        target: ~/.gitconfig
    install:
      apt: [docker.io, kubectl]
```

The `work` profile inherits everything from `base` and adds its own configuration.

### Inheritance Rules

1. **Dotfiles are merged** - Child profile's dotfiles are added to parent's
2. **Install blocks are merged** - Packages from both profiles are installed
3. **Child overrides parent** - If the same target appears in both, child wins
4. **Single inheritance only** - A profile can only extend one parent

### Multi-Level Inheritance

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
    extends: developer
    install:
      apt: [docker.io]
```

`work` profile will have: `git`, `curl`, `vim`, and `docker.io`.

### Circular Reference Detection

dottie detects and prevents circular inheritance:

```yaml
profiles:
  a:
    extends: b
  b:
    extends: a  # Error: Circular reference
```

## Listing Profiles

Run validate without a profile to see available profiles:

```bash
> dottie validate
Available profiles:
  - default
  - work
  - server
```

## Best Practices

### Organize by Context

```yaml
profiles:
  default:      # Base for all machines
  work:         # Work machine additions
  home:         # Personal machine extras
  server:       # Minimal server setup
```

### Use Meaningful Names

Profile names should describe their purpose:

- ✅ `default`, `work`, `server`, `minimal`
- ❌ `profile1`, `test`, `temp`

### Keep Base Profiles Minimal

Put only truly common configuration in base profiles. It's easier to add than to override.

### Document Inheritance

Add comments explaining the inheritance chain:

```yaml
profiles:
  # Base profile - common to all machines
  default:
    # ...

  # Work profile - extends default with work tools
  work:
    extends: default
    # ...
```
