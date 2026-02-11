---
title: Fonts
sidebar_position: 4
description: How to install Nerd Fonts and other fonts
---

# Fonts Installation Guide

This guide explains how to install fonts using dottie, including Nerd Fonts for terminal customization.

## Basic Configuration

```yaml
install:
  fonts:
    - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/FiraCode.zip
```

dottie downloads the font archive, extracts it, and installs the fonts to `~/.local/share/fonts/`.

## Installation Location

Fonts are installed to the user's local fonts directory:

```
~/.local/share/fonts/
```

After installation, the font cache is automatically refreshed with `fc-cache -f`.

## Nerd Fonts

[Nerd Fonts](https://www.nerdfonts.com/) patches developer-targeted fonts with icons and glyphs. They're essential for modern terminal setups with tools like Starship, Powerlevel10k, and Oh My Zsh themes.

### Popular Nerd Fonts

#### Fira Code

Popular monospace font with programming ligatures:

```yaml
fonts:
  - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/FiraCode.zip
```

#### JetBrains Mono

Clean, modern font designed for code:

```yaml
fonts:
  - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/JetBrainsMono.zip
```

#### Hack

A typeface designed for source code:

```yaml
fonts:
  - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/Hack.zip
```

#### Meslo

Apple's Menlo font with Nerd Font patches:

```yaml
fonts:
  - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/Meslo.zip
```

#### Source Code Pro

Adobe's monospace font for developers:

```yaml
fonts:
  - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/SourceCodePro.zip
```

## Finding Nerd Font URLs

1. Go to [Nerd Fonts Releases](https://github.com/ryanoasis/nerd-fonts/releases)
2. Find the version you want (e.g., v3.0.2)
3. Expand the **Assets** section
4. Copy the URL for your desired font (e.g., `FiraCode.zip`)

## After Installation

### Configure Your Terminal

After installing fonts, configure your terminal emulator to use them:

**GNOME Terminal**: Preferences → Profile → Custom font

**Alacritty** (`~/.config/alacritty/alacritty.yml`):
```yaml
font:
  normal:
    family: "FiraCode Nerd Font"
    style: Regular
  size: 12.0
```

**VS Code** (settings.json):
```json
{
  "terminal.integrated.fontFamily": "FiraCode Nerd Font",
  "editor.fontFamily": "FiraCode Nerd Font"
}
```

### Verify Installation

Check if fonts are installed:

```bash
fc-list | grep -i "fira"
```

## Non-Nerd Fonts

You can install any font from a URL:

```yaml
fonts:
  - url: https://example.com/MyFont.zip
```

Supported archive formats:

- `.zip`
- `.tar.gz`
- `.tar.xz`

The archive should contain `.ttf`, `.otf`, or other font files.

## Idempotency

dottie checks if font files already exist before downloading:

```
⊘ FiraCode Skipped (Font) - Already installed
```

To force reinstallation, remove the fonts manually:

```bash
rm -rf ~/.local/share/fonts/FiraCode*
> dottie install
```

## Complete Example

```yaml
install:
  fonts:
    # Primary coding font
    - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/FiraCode.zip

    # Backup/alternative font
    - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/JetBrainsMono.zip

    # Terminal-specific font
    - url: https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.2/Meslo.zip
```

## Troubleshooting

### Fonts Not Showing in Applications

Run the font cache refresh manually:

```bash
fc-cache -f -v
```

Then restart the application.

### Download Failed

```
✗ FiraCode Failed (Font) - Download failed
```

**Solution**: Verify the URL is correct and accessible.

### Old Font Version

To update to a newer version:

1. Remove the old fonts from `~/.local/share/fonts/`
2. Update the URL to the new version
3. Run `dottie install`
