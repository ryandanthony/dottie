# dottie

A dotfile manager and software installation tool for Linux (Ubuntu).

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
      apt-repos:
        - name: repo-name
          key-url: https://example.com/key.gpg
          repo-line: "deb https://example.com/apt stable main"

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
| `apt-repos` | Add APT repositories before installing packages |

## CLI Commands

```bash
# Validate configuration file
dottie validate <profile>

# Validate with custom config path
dottie validate <profile> -c /path/to/dottie.yaml

# Validate without specifying profile (lists available profiles)
dottie validate
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
