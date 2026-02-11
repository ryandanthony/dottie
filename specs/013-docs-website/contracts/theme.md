# Theme Contract

**Feature**: 013-docs-website  
**Date**: 2026-02-09

This document defines the required CSS custom properties and theme configuration for the documentation site.

## Color Mode Configuration

- Default mode: **dark**
- Switch enabled: **yes**
- Respect system preference: **yes**

## CSS Custom Properties

### Light Mode (`:root`)

```css
:root {
  --ifm-color-primary: #16a34a;
  --ifm-color-primary-dark: #15803d;
  --ifm-color-primary-darker: #166534;
  --ifm-color-primary-darkest: #14532d;
  --ifm-color-primary-light: #22c55e;
  --ifm-color-primary-lighter: #4ade80;
  --ifm-color-primary-lightest: #86efac;
  --ifm-font-family-monospace: 'JetBrains Mono', 'Fira Code', monospace;
  --ifm-background-color: #ffffff;
}
```

### Dark Mode (`[data-theme="dark"]`)

```css
[data-theme="dark"] {
  /* Per style-guide.md: Terminal Green #33cc66, Background #000000, Text #eaeaea */
  --ifm-color-primary: #33cc66;
  --ifm-color-primary-dark: #2eb85c;
  --ifm-color-primary-darker: #29a352;
  --ifm-color-primary-darkest: #1f7a3d;
  --ifm-color-primary-light: #47d178;
  --ifm-color-primary-lighter: #5cd88a;
  --ifm-color-primary-lightest: #85e3a8;
  --ifm-background-color: #000000;
  --ifm-background-surface-color: #111111;
  --ifm-font-color-base: #eaeaea;
  --ifm-font-color-secondary: #cccccc;
}
```

### Code Block Styling

```css
pre code {
  font-family: var(--ifm-font-family-monospace);
}

[data-theme="dark"] .prism-code {
  background-color: #000000 !important;
  border: 1px solid #cccccc;
}
```

## Color Roles Reference

*Source: [style-guide.md](../style-guide.md)*

| Role | Dark Mode | Light Mode |
|------|-----------|------------|
| Background | `#000000` | `#ffffff` |
| Surface/Card | `#111111` | `#f6f8fa` |
| Primary | `#33cc66` (Terminal Green) | `#16a34a` |
| Primary hover | `#2eb85c` | `#15803d` |
| Text primary | `#eaeaea` (Off-white) | `#1f2937` |
| Text secondary | `#cccccc` (Light Gray) | `#6b7280` |
| Border | `#cccccc` | `#d1d5db` |
| Code background | `#000000` | `#f0f0f0` |
| Accent | `#58a6ff` (blue) | `#2563eb` |

## Typography

- **Headings**: System sans-serif stack
- **Body**: System sans-serif
- **Code / CLI examples**: `JetBrains Mono`, `Fira Code`, or system `monospace`

## Accessibility Requirements

- WCAG AA minimum contrast ratio (4.5:1 for normal text, 3:1 for large text) in both themes
- Lighthouse accessibility score ≥ 90
