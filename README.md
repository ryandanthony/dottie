# dottie

A dotfile manager and software installation tool for Linux (Ubuntu).

## Installation

### Quick Install (Linux)

Install the latest release directly to `~/bin`:

```bash
curl -s https://raw.githubusercontent.com/ryandanthony/dottie/main/scripts/install-linux.sh | bash
```

Make sure `~/bin` is in your PATH. If not, add this to your shell profile (`~/.bashrc`, `~/.zshrc`, etc.):

```bash
export PATH="$HOME/bin:$PATH"
```

Then run `dottie` to verify the installation worked:

```bash
dottie --help
```

## Quick Start

1. Create a `dottie.yaml` in your dotfiles repository:

```yaml
profiles:
  default:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc
      - source: dotfiles/.gitconfig
        target: ~/.gitconfig

    install:
      apt:
        - git
        - curl
        - vim
```

2. Validate your configuration:

```bash
dottie validate default
```

## Features

- **YAML Configuration**: Define dotfile symlinks and software installation in a single `dottie.yaml` file
- **Profile Support**: Create multiple profiles (e.g., `default`, `work`, `minimal`) with profile inheritance
- **Install Blocks**: Install software from APT, GitHub releases, Snap, Nerd Fonts, and custom scripts
- **Validation**: Validate configuration files before applying changes
- **Architecture-Aware**: GitHub release downloads support architecture placeholders (`{arch}`)

## Configuration Reference

### Profile Structure

```yaml
profiles:
  default:
    dotfiles:
      - source: path/to/source
        target: ~/path/to/target

    install:
      apt: [package1, package2]
      scripts: [scripts/setup.sh]
      github:
        - repo: owner/repo
          asset: binary-{arch}.tar.gz
          binary: binary-name
      snaps:
        - name: code
          classic: true
      fonts:
        - url: https://example.com/font.zip
      aptRepos:
        - name: repo-name
          key_url: https://example.com/key.gpg
          repo: "deb https://example.com/apt stable main"
          packages:
            - package-from-repo

  work:
    extends: default  # Inherit from another profile
    dotfiles:
      - source: dotfiles/.work-specific
        target: ~/.work-specific
```

### Dotfile Entries

| Field | Required | Description |
|-------|----------|-------------|
| `source` | Yes | Path to source file (relative to repo root) |
| `target` | Yes | Target path for symlink (supports `~` expansion) |

### Install Block Types

| Type | Description |
|------|-------------|
| `apt` | APT packages (Debian/Ubuntu) |
| `scripts` | Shell scripts to run (must be within repository) |
| `github` | Download binaries from GitHub releases |
| `snaps` | Snap packages |
| `fonts` | Nerd Fonts or other font downloads |
| `aptRepos` | Add APT repositories before installing packages |

## CLI Commands

### Validate Configuration

```bash
# Validate configuration file
dottie validate <profile>

# Validate with custom config path
dottie validate <profile> -c /path/to/dottie.yaml

# Validate without specifying profile (lists available profiles)
dottie validate
```

### Link Dotfiles

Create symbolic links from your repository to their target locations.

```bash
# Link dotfiles using default profile
dottie link

# Link dotfiles using a specific profile
dottie link --profile work
dottie link -p work

# Preview changes without creating symlinks (dry-run)
dottie link --dry-run
dottie link -d

# Force linking - backup existing files and overwrite
dottie link --force
dottie link -f

# Use custom config path
dottie link --config /path/to/dottie.yaml
dottie link -c /path/to/dottie.yaml
```

**Options:**

| Option | Short | Description |
|--------|-------|-------------|
| `--profile` | `-p` | Profile to use (default: "default") |
| `--config` | `-c` | Path to configuration file |
| `--dry-run` | `-d` | Preview changes without creating symlinks |
| `--force` | `-f` | Backup existing files and overwrite conflicts |

**Note:** `--dry-run` and `--force` are mutually exclusive.

**Output Examples:**

```bash
# Normal operation
✓ Created 5 symlink(s).
Skipped 2 file(s) (already linked).

# With conflicts (no --force)
Error: Conflicting files detected. Use --force to backup and overwrite.
Conflicts:
  • ~/.bashrc (file)
  • ~/.config/nvim (symlink → /other/path)
Found 2 conflict(s).

# Dry-run preview
Dry run - no changes will be made.
Would create 3 symlink(s):
  • dotfiles/bashrc → ~/.bashrc
  • dotfiles/vimrc → ~/.vimrc
  • dotfiles/gitconfig → ~/.gitconfig
Would skip 1 file(s) (already linked):
  • ~/.zshrc
```

### Install Software

Install software packages defined in your configuration's install block.

```bash
# Install software using default profile
dottie install

# Install using a specific profile
dottie install --profile work
dottie install -p work

# Preview what would be installed (dry-run)
dottie install --dry-run

# Use custom config path
dottie install --config /path/to/dottie.yaml
dottie install -c /path/to/dottie.yaml
```

**Options:**

| Option | Short | Description |
|--------|-------|-------------|
| `--profile` | `-p` | Profile to use (default: "default") |
| `--config` | `-c` | Path to configuration file |
| `--dry-run` | | Preview installation without making changes |

**Installation Priority Order:**

1. GitHub Releases (binaries downloaded to `~/bin/`)
2. APT Packages
3. APT Repositories (added before package installation)
4. Scripts (custom setup scripts)
5. Fonts (installed to `~/.local/share/fonts/`)
6. Snap Packages

**Idempotency:** Already-installed tools are automatically detected and skipped.

**Output Examples:**

```bash
# Successful installation
✓ rg Success (GithubRelease) - Installed to ~/bin/rg
⊘ git Skipped (AptPackage) - Already installed
✓ curl Success (AptPackage)

Installation Summary:
  ✓ Succeeded: 2
  ⊘ Skipped: 1

# Dry-run preview
Dry Run Mode: Previewing installation without making changes
✓ rg Success (GithubRelease) - GitHub release BurntSushi/ripgrep@latest would be installed
⊘ fd Skipped (GithubRelease) - Already installed in ~/bin/

Installation Summary:
  ✓ Succeeded: 1
  ⊘ Skipped: 1

# With failures
✓ rg Success (GithubRelease)
✗ nonexistent Failed (GithubRelease) - Version 99.0.0 not found

Installation Summary:
  ✓ Succeeded: 1
  ✗ Failed: 1

Failed Installations:
  [GithubRelease] nonexistent: Version 99.0.0 not found
```

## Building from Source

```bash
# Build
dotnet build

# Run tests
dotnet test

# Publish for Linux
dotnet publish src/Dottie.Cli/Dottie.Cli.csproj -c Release -r linux-x64

# Publish for Windows
dotnet publish src/Dottie.Cli/Dottie.Cli.csproj -c Release -r win-x64
```

## Project Structure

```
src/
├── Dottie.Cli/              # CLI application
│   ├── Commands/            # CLI commands
│   ├── Output/              # Console output formatting
│   └── Utilities/           # Helper utilities
└── Dottie.Configuration/    # Configuration parsing library
    ├── Inheritance/         # Profile inheritance logic
    ├── Models/              # Domain models
    ├── Parsing/             # YAML parsing
    ├── Templates/           # Starter templates
    ├── Utilities/           # Helper utilities
    └── Validation/          # Configuration validation
```

## License

MIT License - see [LICENSE](LICENSE) for details.

---

Project principles and contributor expectations live in `.specify/memory/constitution.md`.
