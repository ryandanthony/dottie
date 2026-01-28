# FEATURE-06: Installation Sources

## Summary

Dottie installs tools from multiple sources, in priority order.

## Priority order

1. GitHub Releases
2. APT Packages
3. Private APT Repositories
4. Shell Scripts
5. Fonts
6. Snap Packages

---

## 1) GitHub Releases (Priority 1)

Download binaries from GitHub release assets.

### Config (GitHub)

```yaml
github:
  - repo: owner/repo-name # required
    asset: pattern-*.tar.gz # required - glob pattern for asset name
    binary: binary-name # required - name of binary inside archive
    version: "1.2.3" # optional - pin version (default: latest)
```

### Behavior (GitHub)

- Download specified asset from the release
- Extract archives (`.tar.gz`, `.zip`, `.tgz`)
- Copy binary to `~/bin/`
- Make executable (`chmod +x`)

---

## 2) APT Packages (Priority 2)

Standard Ubuntu packages.

### Config (APT)

```yaml
apt:
  - package-name
  - another-package
```

### Behavior (APT)

- Run `sudo apt-get update` (once per apply)
- Run `sudo apt-get install -y <packages>`

---

## 3) Private APT Repositories (Priority 3)

Add third-party apt sources with GPG keys.

### Config (APT repo)

```yaml
apt-repo:
  - name: descriptive-name # required
    key_url: https://...gpg # required - URL to GPG key
    repo: "deb [arch=...] ..." # required - sources.list line
    packages: # required
      - package-name
```

### Behavior (APT repo)

1. Download GPG key from `key_url`
2. Add key to `/etc/apt/trusted.gpg.d/` or use `signed-by`
3. Add repo to `/etc/apt/sources.list.d/<name>.list`
4. `apt-get update`
5. Install specified packages

---

## 4) Shell Scripts (Priority 4)

Run custom installation scripts from the repo.

### Config (Scripts)

```yaml
scripts:
  - scripts/install-nvm.sh
  - scripts/setup-something.sh
```

### Behavior (Scripts)

- Scripts must exist in the repo (no external URLs for security)
- Run scripts with bash: `bash scripts/install-nvm.sh`
- Scripts run from repo root directory

---

## 5) Fonts (Priority 5)

Install fonts to user directory.

### Config (Fonts)

```yaml
fonts:
  - name: Font Name # required - descriptive name
    url: https://...zip # required - download URL
```

### Behavior (Fonts)

1. Download font archive
2. Extract to `~/.local/share/fonts/<name>/`
3. Run `fc-cache -fv` to refresh font cache

---

## 6) Snap Packages (Priority 6)

Install snap packages.

### Config (Snap)

```yaml
snap:
  - name: package-name # required
    classic: true # optional - use --classic flag
```

### Behavior (Snap)

- Run `sudo snap install <name> [--classic]`

---

## Shared conventions

- Binary install location: `~/bin/` (auto-created, assumed in PATH on Ubuntu)
- Font install location: `~/.local/share/fonts/`
