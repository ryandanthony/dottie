# FEATURE-03: Directory Structure

## Summary

Dottie expects a standard repository layout.

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
