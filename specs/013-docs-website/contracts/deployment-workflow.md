# GitHub Actions Deployment Contract

**Feature**: 013-docs-website  
**Date**: 2026-02-09

This document defines the required configuration for the documentation deployment workflow.

## Workflow: `.github/workflows/docs.yml`

### Triggers

```yaml
on:
  push:
    branches: [main]
    paths:
      - 'website/**'
      - 'docs/**'
  workflow_dispatch: {}
```

**Requirements**:
- MUST trigger on push to `main` when `website/` or `docs/` files change (FR-007, FR-008)
- MUST NOT trigger on changes to other paths (`src/`, `tests/`, etc.) (FR-008)
- MUST support manual trigger via `workflow_dispatch` (FR-014)

### Permissions

```yaml
permissions:
  contents: read
  pages: write
  id-token: write
```

### Concurrency

```yaml
concurrency:
  group: pages
  cancel-in-progress: true
```

Only one deployment may run at a time. New pushes cancel in-progress deployments.

### Build Job

| Step | Command | Working Directory |
|------|---------|-------------------|
| Checkout | `actions/checkout@v4` | — |
| Setup Node | `actions/setup-node@v4` (node 20, npm cache) | — |
| Install dependencies | `npm ci` | `./website` |
| Build | `npm run build` | `./website` |
| Upload artifact | `actions/upload-pages-artifact@v3` | path: `website/build` |

### Deploy Job

| Step | Action |
|------|--------|
| Deploy | `actions/deploy-pages@v4` |

**Environment**: `github-pages`  
**Depends on**: `build` job

### Build Validation

The `npm run build` step serves as the validation gate:
- Broken internal links → build failure (`onBrokenLinks: 'throw'`)
- Invalid Markdown syntax → build failure
- Missing frontmatter → build warning
- Invalid Docusaurus config → build failure

A failed build prevents deployment. The previously deployed version remains live.
