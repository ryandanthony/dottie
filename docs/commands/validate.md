---
title: Validate
sidebar_position: 1
description: Validate your dottie configuration file
---

# Validate Command

The `validate` command checks your `dottie.yaml` configuration for errors before applying changes.

## Usage

```bash
> dottie validate <profile>
```

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--config` | `-c` | Path to configuration file (default: `dottie.yaml`) |

## Examples

### Validate a specific profile

```bash
> dottie validate default
```

### Validate with custom config path

```bash
> dottie validate default -c /path/to/dottie.yaml
```

### List available profiles

Run without specifying a profile to see available options:

```bash
> dottie validate
```

## Output

### Valid Configuration

```
Configuration is valid.
Profile 'default' contains:
  - 5 dotfile entries
  - 3 APT packages
  - 2 GitHub releases
```

### Invalid Configuration

```
Configuration validation failed:
  • Line 12: Unknown field 'soruce' - did you mean 'source'?
  • Line 15: Target path '~/.config/nvim' must start with ~/
```

## What Gets Validated

- YAML syntax correctness
- Required fields are present (`source`, `target` for dotfiles)
- Profile inheritance chains are valid (no circular references)
- Referenced files exist in the repository
- Install block entries have required fields

## Best Practices

1. **Always validate before applying** - Run `validate` before `link` or `install`
2. **Validate in CI** - Add validation to your CI pipeline to catch config errors early
3. **Validate after edits** - Re-validate whenever you modify your configuration
