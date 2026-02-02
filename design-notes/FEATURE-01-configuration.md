# FEATURE-01: Configuration (YAML)

**STATUS**: Done

## Summary

Configuration is defined in a single YAML file `dottie.yaml` at the repo root.

## File location

- Config file: `dottie.yaml` (repo root)

## Concepts

- **Profiles**: named configurations (e.g. `default`, `work`, `minimal`)
- **Dotfiles list**: mappings of `source` (inside repo) to `target` (in home dir)
- **Install blocks**: grouped by install mechanism (`github`, `apt`, `apt-repo`, `scripts`, `fonts`, `snap`)
- **Inheritance**: `extends: <profile>` allows a profile to inherit and override

## Example structure

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
          version: "0.24.0" # optional; defaults to latest

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
    extends: default
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

## Validation rules (proposed)

- `profiles` must exist
- `profiles.<name>.dotfiles[*].source` and `.target` required for each dotfile item
- `profiles.<name>.install` blocks are optional
- For `github` items: `repo`, `asset`, `binary` required; `version` optional
- For `apt-repo` items: `name`, `key_url`, `repo`, `packages` required
- For `snap` items: `name` required; `classic` optional

## Security constraints

- `scripts` must refer to files inside the repo (no external URLs)
