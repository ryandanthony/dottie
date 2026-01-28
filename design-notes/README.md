# Dottie Design Notes

A dotfile manager and software installation tool for Linux (Ubuntu).

## Overview

Dottie helps you:

1. **Manage dotfiles** — Keep dotfiles in a git repo, symlink them to their proper locations
2. **Install software** — Automatically install tools from multiple sources based on configuration

## Core Principles

- **Git-based**: All dotfiles and configuration live in a single git repository
- **Symlinks**: Dotfiles are symlinked from the repo to their target locations (e.g., `~/.bashrc` → `repo/dotfiles/bashrc`)
- **Declarative config**: A single YAML file describes what to link and install
- **Ubuntu-first**: Only Ubuntu is supported at this time

---

## Configuration

### Format: YAML

Config file: `dottie.yaml` (in repo root)

### Example Structure

```yaml
profiles:
  default:
    dotfiles:
      - source: dotfiles/bashrc
        target: ~/.bashrc
      - source: dotfiles/vimrc
        target: ~/.vimrc
      - source: dotfiles/config/nvim
        target: ~/.config/nvim

    install:
      github:
        - repo: junegunn/fzf
          asset: fzf-*-linux_amd64.tar.gz
          binary: fzf
        - repo: sharkdp/bat
          asset: bat-*-x86_64-unknown-linux-musl.tar.gz
          binary: bat
          version: "0.24.0" # pinned version (optional, defaults to latest)

      apt:
        - git
        - curl
        - htop

      apt-repo:
        - name: docker
          key_url: https://download.docker.com/linux/ubuntu/gpg
          repo: "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"
          packages:
            - docker-ce
            - docker-ce-cli

      scripts:
        - scripts/install-nvm.sh
        - scripts/setup-golang.sh

      fonts:
        - name: JetBrains Mono
          url: https://github.com/JetBrains/JetBrainsMono/releases/download/v2.304/JetBrainsMono-2.304.zip

      snap:
        - name: code
          classic: true
        - name: spotify

  work:
    extends: default # inherit from default profile
    install:
      apt:
        - awscli
        - kubectl

  minimal:
    dotfiles:
      - source: dotfiles/bashrc
        target: ~/.bashrc
    install:
      apt:
        - git
        - vim
```

---

## CLI Commands

### `dottie initialize <git-repo-url> [--location <location-to-clone>]`

Clone repo and set up initial configuration.

**Behavior:**

1. Clone the provided git repo to `~/.dottie` (or configurable location)
2. Generate initial `dottie.yaml` with `default` profile
3. Scan for common dotfiles in home directory (`.bashrc`, `.vimrc`, `.gitconfig`, etc.)
4. Adds discovered dotfiles to the `default` profile in `dottie.yaml`
5. Move found dotfiles into the repo's `dotfiles/` directory
6. Create symlinks back to original locations

### `dottie apply [--profile <name>] [--force] [--dry-run]`

Apply everything — create symlinks and install all software.

**Flags:**

- `--profile <name>`: Use specific profile (default: `default`)
- `--force`: Overwrite existing files (backs them up first)
- `--dry-run`: Show what would happen without making changes

### `dottie link [--profile <name>] [--force] [--dry-run]`

Only create symlinks for dotfiles.

### `dottie install [--profile <name>] [--dry-run]`

Only install software (no dotfile linking).

### `dottie status [--profile <name>]`

Show current state:

- Which dotfiles are linked / missing / conflicting
- Which software is installed / missing / outdated

---

## Installation Sources

### 1. GitHub Releases (Priority 1)

Download binaries from GitHub release assets.

**Config:**

```yaml
github:
  - repo: owner/repo-name # required
    asset: pattern-*.tar.gz # required - glob pattern for asset name
    binary: binary-name # required - name of binary inside archive
    version: "1.2.3" # optional - pin version (default: latest)
```

**Behavior:**

- Download the specified asset from the release
- Extract archives (`.tar.gz`, `.zip`, `.tgz`)
- Copy binary to `~/bin/`
- Make executable (`chmod +x`)

### 2. APT Packages (Priority 2)

Standard Ubuntu packages.

**Config:**

```yaml
apt:
  - package-name
  - another-package
```

**Behavior:**

- Run `sudo apt-get update` (once per apply)
- Run `sudo apt-get install -y <packages>`

### 3. Private APT Repositories (Priority 3)

Add third-party apt sources with GPG keys.

**Config:**

```yaml
apt-repo:
  - name: descriptive-name # required
    key_url: https://...gpg # required - URL to GPG key
    repo: "deb [arch=...] ..." # required - sources.list line
    packages: # required
      - package-name
```

**Behavior:**

1. Download GPG key from `key_url`
2. Add key to `/etc/apt/trusted.gpg.d/` or use `signed-by`
3. Add repo to `/etc/apt/sources.list.d/<name>.list`
4. `apt-get update`
5. Install specified packages

### 4. Shell Scripts (Priority 4)

Run custom installation scripts from the repo.

**Config:**

```yaml
scripts:
  - scripts/install-nvm.sh
  - scripts/setup-something.sh
```

**Behavior:**

- Scripts must exist in the repo (no external URLs for security)
- Run scripts with bash: `bash scripts/install-nvm.sh`
- Scripts run from repo root directory

### 5. Fonts (Priority 5)

Install fonts to user directory.

**Config:**

```yaml
fonts:
  - name: Font Name # required - descriptive name
    url: https://...zip # required - download URL
```

**Behavior:**

1. Download font archive
2. Extract to `~/.local/share/fonts/<name>/`
3. Run `fc-cache -fv` to refresh font cache

### 6. Snap Packages (Priority 6)

Install snap packages.

**Config:**

```yaml
snap:
  - name: package-name # required
    classic: true # optional - use --classic flag
```

**Behavior:**

- Run `sudo snap install <name> [--classic]`

---

## Conflict Handling

When a target file already exists (and is not already a symlink to our repo):

1. **Default behavior**: Fail with error, show which files conflict
2. **With `--force`**:
   - Back up existing file to `<filename>.backup.<timestamp>`
   - Then create the symlink

---

## Directory Structure

```
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

**Binary install location:** `~/bin/` (auto-created, already in PATH on Ubuntu)

**Font install location:** `~/.local/share/fonts/`

---

## Profiles

Profiles allow different configurations for different machines/use cases.

**Features:**

- `extends: profile-name` — inherit from another profile
- Override or add to dotfiles/install lists
- Select profile with `--profile <name>` flag
- Default profile: `default`

**Use cases:**

- `work` vs `personal` machines
- `minimal` vs `full` setups
- `laptop` vs `server` configurations

---

## Future Considerations (Out of Scope for v1)

- Windows/macOS support
- Uninstall/unlink commands
- Auto-update command
- Encrypted secrets management
- Templating in dotfiles (e.g., machine-specific values)
