# Dottie Design Notes

## Summary

Dottie is a dotfile manager and software installation tool for Linux (Ubuntu-first) that:

- Manages dotfiles from a git repo using symlinks
- Installs software from multiple sources based on a declarative YAML config

## Goals

- Single repo holds dotfiles + configuration
- Deterministic, repeatable setup from config
- Safe-by-default behavior (avoid destructive overwrites unless explicitly requested)

## Non-goals (v1)

- Support for Windows/macOS
- Uninstall/unlink
- Auto-update
- Encrypted secrets management
- Templating in dotfiles

## Core principles

- Git-based: all dotfiles and config live in one git repository
- Symlinks: dotfiles are symlinked from the repo to targets (e.g., `~/.bashrc` → `repo/dotfiles/bashrc`)
- Declarative config: a single YAML file describes what to link and install
- Ubuntu-first: only Ubuntu is supported initially

## Key user flows

- Initialize a repo clone + bootstrap config
- Apply configuration (link + install)
- Inspect state/status

## Feature specs

- [FEATURE-01-configuration.md](FEATURE-01-configuration.md)
- [FEATURE-02-profiles.md](FEATURE-02-profiles.md)
- [FEATURE-03-directory-structure.md](FEATURE-03-directory-structure.md)
- [FEATURE-04-conflict-handling.md](FEATURE-04-conflict-handling.md)
- [FEATURE-05-cli-link.md](FEATURE-05-cli-link.md)
- [FEATURE-06-installation-sources.md](FEATURE-06-installation-sources.md)
- [FEATURE-07-cli-install.md](FEATURE-07-cli-install.md)
- [FEATURE-08-cli-apply.md](FEATURE-08-cli-apply.md)
- [FEATURE-09-cli-status.md](FEATURE-09-cli-status.md)
- [FEATURE-10-cli-initialize.md](FEATURE-10-cli-initialize.md)
