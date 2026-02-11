---
title: Design Decisions
sidebar_position: 3
description: Key architectural decisions and their rationale
---

# Design Decisions

This document summarizes key design decisions made during dottie's development.

## Configuration Format: YAML

**Decision**: Use YAML for configuration files.

**Rationale**:
- Human-readable and easy to edit
- Widely familiar to developers
- Supports complex nested structures
- Good tooling support (syntax highlighting, linting)

**Alternatives Considered**:
- JSON: Too verbose, no comments
- TOML: Less familiar, limited nesting
- Custom DSL: Higher learning curve

## Profile Inheritance Model

**Decision**: Single inheritance with explicit `extends` keyword.

**Rationale**:
- Simple mental model
- Avoids diamond inheritance complexity
- Clear override semantics (child wins)

**Example**:
```yaml
profiles:
  base:
    install:
      apt: [git]
  
  work:
    extends: base  # Inherits from base
    install:
      apt: [docker.io]  # Merged with base
```

## Installation Order

**Decision**: Fixed installation order (GitHub → APT → AptRepos → Scripts → Fonts → Snaps).

**Rationale**:
- Predictable behavior
- APT repos must be added before their packages
- GitHub binaries available for scripts to use
- No dependency graph complexity

## Idempotency by Default

**Decision**: All operations detect existing state and skip unnecessary work.

**Rationale**:
- Safe to re-run at any time
- Efficient for incremental updates
- Reduces user anxiety about "applying too many times"

**Implementation**:
- APT: Check `dpkg -l` before install
- GitHub: Check if binary exists in `~/bin`
- Links: Compare symlink targets

## Dry-Run Mode

**Decision**: Every write operation supports `--dry-run`.

**Rationale**:
- Builds user confidence
- Enables safe experimentation
- Reduces support burden

## Error Handling Philosophy

**Decision**: Fail fast with actionable messages.

**Rationale**:
- Users can quickly identify and fix issues
- Partial application states are avoided
- Clear distinction between warnings and errors

**Example**:
```
Error: Profile 'work' extends 'base', but 'base' does not exist.
Hint: Available profiles: default, minimal
```

## No Auto-Update

**Decision**: dottie does not auto-update itself.

**Rationale**:
- Predictable behavior
- User controls when updates happen
- No surprise breaking changes
- Simpler security model

## Target Platform: Ubuntu

**Decision**: Ubuntu LTS as primary supported platform.

**Rationale**:
- Clear focus enables better testing
- APT integration is first-class
- Most popular Linux distribution for developers
- WSL compatibility

**Other distributions** may work but are not actively tested.
