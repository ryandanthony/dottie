---
title: Scripts
sidebar_position: 5
description: How to run custom setup scripts
---

# Scripts Guide

This guide explains how to use custom scripts in your dottie configuration for tasks that can't be handled by other install types.

## When to Use Scripts

Use scripts for:

- Complex setup steps that need custom logic
- Tool installation not covered by apt/github/snap
- Post-installation configuration
- Environment setup (nvm, pyenv, etc.)

:::caution Security
Scripts must be located within your repository. dottie will not run scripts from external URLs.
:::

## Basic Configuration

```yaml
install:
  scripts:
    - scripts/setup-nvm.sh
    - scripts/configure-docker.sh
```

Scripts are run in the order listed, from the repository root.

## Script Requirements

### Location

Scripts must be inside your repository. External scripts are not allowed:

```yaml
# ✅ Valid - script in repository
scripts:
  - scripts/setup.sh
  - tools/configure.sh

# ❌ Invalid - external URL
scripts:
  - https://example.com/setup.sh
```

### Permissions

Scripts don't need to be executable in git. dottie runs them with the user's shell:

```bash
# dottie runs scripts like this:
bash scripts/setup.sh
```

### Exit Codes

Scripts should return appropriate exit codes:

- `0` - Success
- Non-zero - Failure (stops installation)

## Writing Scripts

### Basic Template

```bash
#!/bin/bash
set -euo pipefail

# Your setup commands here
echo "Setting up..."

# Check if already done
if [ -f "$HOME/.setup-complete" ]; then
    echo "Already configured, skipping"
    exit 0
fi

# Do the setup
# ...

# Mark as complete
touch "$HOME/.setup-complete"
echo "Setup complete"
```

### Idempotent Scripts

Make scripts safe to run multiple times:

```bash
#!/bin/bash
set -euo pipefail

# Check if NVM is already installed
if [ -d "$HOME/.nvm" ]; then
    echo "NVM already installed, skipping"
    exit 0
fi

# Install NVM
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.5/install.sh | bash

echo "NVM installed"
```

## Common Use Cases

### Installing NVM (Node Version Manager)

`scripts/setup-nvm.sh`:
```bash
#!/bin/bash
set -euo pipefail

NVM_DIR="$HOME/.nvm"

if [ -d "$NVM_DIR" ]; then
    echo "NVM already installed"
    exit 0
fi

# Install NVM
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.5/install.sh | bash

# Load NVM
export NVM_DIR="$HOME/.nvm"
[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"

# Install latest LTS
nvm install --lts

echo "NVM and Node.js LTS installed"
```

### Installing pyenv

`scripts/setup-pyenv.sh`:
```bash
#!/bin/bash
set -euo pipefail

if [ -d "$HOME/.pyenv" ]; then
    echo "pyenv already installed"
    exit 0
fi

# Install dependencies
sudo apt-get update
sudo apt-get install -y make build-essential libssl-dev zlib1g-dev \
    libbz2-dev libreadline-dev libsqlite3-dev wget curl llvm \
    libncursesw5-dev xz-utils tk-dev libxml2-dev libxmlsec1-dev \
    libffi-dev liblzma-dev

# Install pyenv
curl https://pyenv.run | bash

echo "pyenv installed"
```

### Post-Install Configuration

`scripts/configure-docker.sh`:
```bash
#!/bin/bash
set -euo pipefail

# Add user to docker group (if not already)
if groups "$USER" | grep -q docker; then
    echo "User already in docker group"
    exit 0
fi

sudo usermod -aG docker "$USER"
echo "Added $USER to docker group"
echo "Note: Log out and back in to apply group changes"
```

### Creating Directories

`scripts/setup-directories.sh`:
```bash
#!/bin/bash
set -euo pipefail

# Create common directories
mkdir -p "$HOME/bin"
mkdir -p "$HOME/projects"
mkdir -p "$HOME/.config"
mkdir -p "$HOME/.local/share"

echo "Directories created"
```

## Execution Order

Scripts run after other install types:

1. GitHub Releases
2. APT Packages
3. APT Repositories
4. **Scripts** ← Here
5. Fonts
6. Snap Packages

This lets scripts use tools installed by earlier steps.

## Error Handling

If a script fails (non-zero exit), dottie stops installation:

```
✓ git Success (AptPackage)
✗ setup-nvm.sh Failed (Script) - Exit code 1

Installation Summary:
  ✓ Succeeded: 1
  ✗ Failed: 1

Failed Installations:
  [Script] setup-nvm.sh: Exit code 1
```

### Graceful Failures

For non-critical scripts, handle errors gracefully:

```bash
#!/bin/bash

# Try to do something, but don't fail the whole install
if ! some-command; then
    echo "Warning: some-command failed, but continuing"
fi

exit 0  # Always succeed
```

## Best Practices

1. **Make scripts idempotent** - Safe to run multiple times
2. **Use `set -euo pipefail`** - Fail on errors
3. **Check preconditions** - Skip if already configured
4. **Provide feedback** - Echo what's happening
5. **Keep scripts focused** - One purpose per script

## Complete Example

Configuration:
```yaml
install:
  apt:
    - git
    - curl
    - build-essential

  scripts:
    - scripts/setup-directories.sh
    - scripts/setup-nvm.sh
    - scripts/configure-shell.sh
```

`scripts/setup-directories.sh`:
```bash
#!/bin/bash
set -euo pipefail
mkdir -p "$HOME/bin" "$HOME/projects"
echo "Directories ready"
```

`scripts/setup-nvm.sh`:
```bash
#!/bin/bash
set -euo pipefail

if [ -d "$HOME/.nvm" ]; then
    echo "NVM already installed"
    exit 0
fi

curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.5/install.sh | bash
echo "NVM installed"
```

`scripts/configure-shell.sh`:
```bash
#!/bin/bash
set -euo pipefail

# Configure shell settings
if ! grep -q "DOTTIE_CONFIGURED" "$HOME/.bashrc"; then
    echo "# DOTTIE_CONFIGURED" >> "$HOME/.bashrc"
    echo 'export PATH="$HOME/bin:$PATH"' >> "$HOME/.bashrc"
    echo "Shell configured"
else
    echo "Shell already configured"
fi
```
