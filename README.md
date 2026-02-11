# dottie

A dotfile manager and software installation tool for Linux (Ubuntu).

## Quick Install

```bash
curl -s https://raw.githubusercontent.com/ryandanthony/dottie/main/scripts/install-linux.sh | bash
```

## Documentation

📚 **[Full Documentation](https://ryandanthony.github.io/dottie/)**

- [Getting Started](https://ryandanthony.github.io/dottie/docs/getting-started/installation)
- [Configuration Reference](https://ryandanthony.github.io/dottie/docs/configuration/overview)
- [CLI Commands](https://ryandanthony.github.io/dottie/docs/commands/validate)
- [How-To Guides](https://ryandanthony.github.io/dottie/docs/guides/profile-inheritance)

## Features

- **YAML Configuration**: Define dotfile symlinks and software installation in a single `dottie.yaml` file
- **Profile Support**: Create multiple profiles (e.g., `default`, `work`, `minimal`) with profile inheritance
- **Install Blocks**: Install software from APT, GitHub releases, Snap, Nerd Fonts, and custom scripts
- **Validation**: Validate configuration files before applying changes
- **Architecture-Aware**: GitHub release downloads support architecture placeholders (`{arch}`)

## Quick Start

```yaml
# dottie.yaml
profiles:
  default:
    dotfiles:
      - source: dotfiles/.bashrc
        target: ~/.bashrc

    install:
      apt:
        - git
        - curl
```

```bash
> dottie validate default
> dottie link
> dottie install
```

## License

MIT License - see [LICENSE](LICENSE) for details.

---

Project principles and contributor expectations live in `.specify/memory/constitution.md`.
