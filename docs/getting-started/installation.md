---
title: Installation
sidebar_position: 1
description: How to install Dottie on Linux
---

# Installation

dottie is a dotfile manager and software installation tool designed for Linux, with Ubuntu as the primary supported distribution.

## Quick Install (Linux)

Install the latest release directly to `~/bin`:

```bash
curl -s https://raw.githubusercontent.com/ryandanthony/dottie/main/scripts/install-linux.sh | bash
```

## PATH Configuration

Make sure `~/bin` is in your PATH. If not, add this to your shell profile (`~/.bashrc`, `~/.zshrc`, etc.):

```bash
export PATH="$HOME/bin:$PATH"
```

After adding the line, reload your shell:

```bash
source ~/.bashrc  # or source ~/.zshrc
```

## Verify Installation

Run the following command to verify dottie is installed correctly:

```bash
> dottie --help
```

You should see the help output showing available commands and options.

## System Requirements

- **Operating System**: Linux (Ubuntu LTS recommended)
- **Shell**: Bash or Zsh
- **Dependencies**: `curl` for installation

## Next Steps

Once installed, continue to the [Quick Start](./quick-start) guide to create your first configuration.
