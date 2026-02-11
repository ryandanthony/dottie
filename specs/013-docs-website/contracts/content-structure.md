# Content Structure Contract

**Feature**: 013-docs-website  
**Date**: 2026-02-09

This document defines the required documentation pages and their content sources.

## Required Pages by Section

### Getting Started (Position: 1)

| Page | File | Source | Priority |
|------|------|--------|----------|
| Installation | `installation.md` | `README.md` → Installation + Quick Install sections | P1 |
| Quick Start | `quick-start.md` | `README.md` → Quick Start section | P1 |
| First Configuration | `first-config.md` | `README.md` → Configuration Reference (introductory subset) | P1 |

### Configuration (Position: 2)

| Page | File | Source | Priority |
|------|------|--------|----------|
| Overview | `overview.md` | `README.md` → Configuration Reference (intro) | P1 |
| Profiles | `profiles.md` | `README.md` → Profile Structure | P1 |
| Dotfiles | `dotfiles.md` | `README.md` → Dotfile Entries table | P1 |
| Install Blocks | `install-blocks.md` | `README.md` → Install Block Types table | P1 |

### Commands (Position: 3)

| Page | File | Source | Priority |
|------|------|--------|----------|
| Validate | `validate.md` | `README.md` → Validate Configuration section | P2 |
| Link | `link.md` | `README.md` → Link Dotfiles section | P2 |
| Install | `install.md` | `README.md` → Install Software section | P2 |
| Apply | `apply.md` | New content (placeholder) | P2 |
| Status | `status.md` | New content (placeholder) | P2 |
| Initialize | `initialize.md` | New content (placeholder) | P2 |

### Guides (Position: 4)

| Page | File | Source | Priority |
|------|------|--------|----------|
| Profile Inheritance | `profile-inheritance.md` | New content from design notes | P3 |
| GitHub Releases | `github-releases.md` | New content from design notes | P3 |
| APT Repos | `apt-repos.md` | New content from design notes | P3 |
| Fonts | `fonts.md` | New content from design notes | P3 |
| Scripts | `scripts.md` | New content from design notes | P3 |

### Architecture (Position: 5)

| Page | File | Source | Priority |
|------|------|--------|----------|
| Overview | `overview.md` | New content with Mermaid diagrams | P2 |
| Project Structure | `project-structure.md` | `README.md` → Project Structure section | P2 |
| Design Decisions | `design-decisions.md` | Design notes summary | P2 |

## Content Migration Rules

1. Content migrated from `README.md` MUST be adapted for the docs context (remove redundant headings, add appropriate frontmatter, adjust relative links).
2. The `README.md` MUST be simplified after migration to contain only a brief project description, installation one-liner, and a prominent link to the documentation site.
3. Pages sourced as "New content" may initially contain placeholder content with a `<!-- TODO: expand -->` marker, as long as they have a valid title and at least a one-paragraph description.
4. All command pages MUST include: description, usage syntax, options table, and at least one example.

## Pre-existing File Migration

| File | Current Location | Action | New Location |
|------|-----------------|--------|-------------|
| `PRE-COMMIT-CHECKLIST.md` | `docs/` | Move | `.specify/memory/` or `design-notes/` |
