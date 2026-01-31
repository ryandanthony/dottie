# FEATURE-03: Directory Structure

> **Note:** This document describes *recommended conventions*, not enforced requirements.
> The YAML schema accepts any valid relative path for `source` and `scripts` entries.
> Directory structure guidance belongs in user documentation, not in code validation.

## Summary

Dottie recommends (but does not enforce) a standard repository layout.

## Proposed structure

```text
~/.dottie/                    # or wherever repo is cloned
├── dottie.yaml               # main configuration
├── dotfiles/                 # dotfiles managed by dottie
│   ├── bashrc
│   ├── vimrc
│   ├── gitconfig
│   └── config/
│       └── nvim/
│           └── init.lua
├── scripts/                  # installation scripts
│   ├── install-nvm.sh
│   └── setup-golang.sh
└── ...
```

## Conventions

- Binary install location: `~/bin/` (auto-created)
- Font install location: `~/.local/share/fonts/`
