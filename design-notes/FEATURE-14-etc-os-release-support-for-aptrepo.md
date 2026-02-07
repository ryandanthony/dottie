# FEATURE-14: /etc/os-release Support for AptRepo

## Overview

Enhance the configuration system to support dynamic variable substitution from system information, specifically from `/etc/os-release` and system architecture detection. Apply this variable support across multiple installation sources: `aptrepo`, `dotfiles`, and `github`.

## Problem Statement

Currently, `aptrepo` entries require hardcoded repository URLs and architecture specifications. This makes configurations less portable across different Linux distributions and architectures. Users often need to manually create separate configurations for different OS versions (e.g., Ubuntu Focal vs Jammy) and architectures (e.g., amd64 vs arm64).

## Proposed Solution

Support variable substitution across install sources using:

1. **Architecture Variables:**
    - `${ARCH}`: Raw output from `uname -m` (e.g., `x86_64`, `aarch64`, `armv7l`)
    - `${MS_ARCH}`: Microsoft-style architecture mapping (e.g., `amd64`, `arm64`, `armhf`)

2. **OS Release Variables:**
    - All variables from `/etc/os-release` file (e.g., `${VERSION_CODENAME}`, `${VERSION_ID}`, `${ID}`)
    - Common variables include:
        - `VERSION_CODENAME`: Ubuntu/Debian release codename (e.g., `jammy`, `focal`)
        - `VERSION_ID`: OS version identifier (e.g., `22.04`)
        - `ID`: OS identifier (e.g., `ubuntu`, `debian`)
        - `PRETTY_NAME`: Human-readable OS name

3. **Release Version Variables:**
    - `${RELEASE_VERSION}`: The resolved GitHub release version (either manually specified or auto-detected latest)
    - Available in `github` and other sources that reference releases

## Example Output of `cat /etc/os-release`

```
PRETTY_NAME="Ubuntu 24.04.3 LTS"
NAME="Ubuntu"
VERSION_ID="24.04"
VERSION="24.04.3 LTS (Noble Numbat)"
VERSION_CODENAME=noble
ID=ubuntu
ID_LIKE=debian
UBUNTU_CODENAME=noble
```

## Architecture

```bash
ARCH=$(uname -m)
echo $ARCH
```

## Architecture Mapping

The `MS_ARCH` variable uses Microsoft's architecture naming conventions:

```bash
case "$(uname -m)" in
  x86_64)  MS_ARCH="amd64"   ;;
  aarch64) MS_ARCH="arm64"   ;;
  armv7l)  MS_ARCH="armhf"   ;;
  *)       echo "Unsupported architecture"; exit 1 ;;
esac
```

| uname -m | MS_ARCH |
| -------- | ------- |
| x86_64   | amd64   |
| aarch64  | arm64   |
| armv7l   | armhf   |

## Usage Examples

### Example 1: Microsoft Products Repository

```yaml
aptrepo:
    - name: microsoft-prod
      key_url: https://packages.microsoft.com/keys/microsoft.asc
      repo: >
          deb [arch=${MS_ARCH}]
          https://packages.microsoft.com/ubuntu/${VERSION_CODENAME}/prod
          ${VERSION_CODENAME} main
      packages:
          - dotnet-sdk-8.0
          - dotnet-sdk-9.0
          - dotnet-sdk-10.0
          - powershell
          - azure-cli
```

Expands on Ubuntu Jammy (arm64) to:

```
deb [arch=arm64] https://packages.microsoft.com/ubuntu/jammy/prod jammy main
```

### Example 2: Azure CLI Repository

```yaml
aptrepo:
    - name: azure-cli
      key_url: https://packages.microsoft.com/keys/microsoft.asc
      repo: >
          deb [arch=${MS_ARCH}]
          https://packages.microsoft.com/repos/azure-cli/
          ${VERSION_CODENAME} main
      packages:
          - azure-cli
```

### Example 3: Using Multiple Variables

```yaml
aptrepo:
    - name: google-chrome
      key_url: https://dl-ssl.google.com/linux/linux_signing_key.pub
      repo: >
          deb [arch=${MS_ARCH}]
          http://dl.google.com/linux/chrome/deb/
          stable main
      packages:
          - google-chrome-stable
```

## Usage Examples - Dotfiles

### Example 4: Platform-Specific Dotfile Sources

```yaml
dotfiles:
    - source: dotfiles/some-script-${VERSION_CODENAME}-${ARCH}.sh
      target: ~/.scripts/some-script.sh
```

## Usage Examples - GitHub Releases with Version Substitution

### Example 5: GitHub Release with Architecture-Specific Assets

```yaml
github:
    - repo: mozilla/firefox
      asset: firefox-*-linux-${MS_ARCH}.tar.bz2
      binary: firefox
      # On arm64: matches firefox-128.0-linux-arm64.tar.bz2
      # On amd64: matches firefox-128.0-linux-amd64.tar.bz2
```

### Example 6: GitHub Release with Version Substitution

```yaml
github:
    - repo: junegunn/fzf
      asset: fzf-${RELEASE_VERSION}-linux_${MS_ARCH}.tar.gz
      binary: fzf
      version: 0.52.0
      # Expands to: fzf-0.52.0-linux_amd64.tar.gz (on amd64)
      # ${RELEASE_VERSION} = manually specified version or auto-detected latest
```

### Example 7: Combining OS and Architecture Variables in GitHub Assets

```yaml
github:
    - repo: cli/cli
      asset: gh_*_linux_${ARCH}.tar.gz
      binary: gh-${ARCH}
      # On x86_64: matches gh_2.47.0_linux_x86_64.tar.gz
      # On aarch64: matches gh_2.47.0_linux_aarch64.tar.gz
```

## Implementation Considerations

### Variable Resolution Order

1. Attempt to read `/etc/os-release` file and parse variables
2. Calculate `${ARCH}` from `uname -m`
3. Calculate `${MS_ARCH}` using the mapping rules
4. Resolve `${RELEASE_VERSION}` from GitHub API (only for `github` items with version references)
5. Perform string substitution on repository URLs, asset patterns, source URLs, etc.

### Variable Resolution for GitHub Releases

For `github` install items:

- If `version` is specified: `${RELEASE_VERSION}` resolves to that value
- If `version` is not specified: Query GitHub API for latest release tag and resolve to that value
- `${RELEASE_VERSION}` is available in both `asset` and `binary` patterns for dynamic asset selection

### Error Handling

- If `/etc/os-release` doesn't exist, warn but continue (may be non-standard distribution)
- If architecture is unsupported for `${MS_ARCH}`, error and fail installation
- If a variable reference is not found, leave it as-is or error depending on strictness setting
- If `${RELEASE_VERSION}` cannot be resolved (API failure, invalid repo), fail with clear error message

### Backwards Compatibility

- Non-variable URLs should continue to work as before
- Only expand variables when `${...}` pattern is detected
- No changes to existing configuration format
- All variable substitution is optional

### Security Considerations

- Ensure variable values are properly escaped when used in shell commands
- Validate that variable values only contain expected characters (alphanumeric, dash, underscore, dot)
- Consider shell injection risks when constructing commands
- Sanitize OS release file values to prevent injection attacks

## Variables Output / Debug Information

Users can enable a `--show-variables` flag (or similar) with the install command to display resolved variables:

```bash
$ dottie install --show-variables
```

Output example:

```
=== System Variables ===
ARCH: x86_64
MS_ARCH: amd64

=== OS Release Variables ===
PRETTY_NAME: "Ubuntu 24.04.3 LTS"
NAME: Ubuntu
VERSION_ID: 24.04
VERSION: 24.04.3 LTS (Noble Numbat)
VERSION_CODENAME: noble
ID: ubuntu
ID_LIKE: debian
UBUNTU_CODENAME: noble

=== Release Version Variables ===
[Package: mozilla/firefox] RELEASE_VERSION: 131.0
[Package: junegunn/fzf] RELEASE_VERSION: 0.52.0

=== Resolved Configurations ===
aptrepo[microsoft-prod].repo: deb [arch=amd64] https://packages.microsoft.com/ubuntu/noble/prod noble main
github[fzf].asset: fzf-0.52.0-linux_amd64.tar.gz
dotfiles[platform-configs].source: https://github.com/user/dotfiles/archive/ubuntu-noble.tar.gz
```

This allows users to:

- Verify variable resolution is working correctly
- Troubleshoot configuration expansion issues
- Understand what values will be used before installation
