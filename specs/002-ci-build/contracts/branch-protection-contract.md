# Contract: Branch Protection Script

**Feature**: 002-ci-build  
**Date**: January 28, 2026  
**Purpose**: Define the interface for the branch protection configuration script

## Script File

**Path**: `scripts/Set-BranchProtection.ps1`

## Usage

```powershell
# Configure branch protection for current repository
./scripts/Set-BranchProtection.ps1

# Configure for specific repository
./scripts/Set-BranchProtection.ps1 -Owner "ryandanthony" -Repo "dottie"

# Preview without applying (dry-run)
./scripts/Set-BranchProtection.ps1 -WhatIf
```

## Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| Owner | string | No | Auto-detect from git remote | Repository owner |
| Repo | string | No | Auto-detect from git remote | Repository name |
| Branch | string | No | "main" | Branch to protect |
| WhatIf | switch | No | false | Preview changes without applying |

## Prerequisites

- `gh` CLI installed and authenticated
- Repository admin permissions

## Behavior

### Protection Rules Applied

| Rule | Value | Description |
|------|-------|-------------|
| required_status_checks.strict | true | Require branch to be up-to-date |
| required_status_checks.contexts | ["build"] | Required CI check name |
| enforce_admins | false | Admins can bypass (for emergencies) |
| required_pull_request_reviews | null | No review requirement |
| restrictions | null | No push restrictions |
| allow_force_pushes | false | Prevent force pushes |
| allow_deletions | false | Prevent branch deletion |

### Idempotency

The script is idempotent:
- Running multiple times produces the same result
- Existing protection rules are replaced (PUT operation)
- No error if protection already exists

## Output

### Success

```
✓ Branch protection configured for main
  - Required status check: build
  - Require up-to-date: Yes
  - Enforce admins: No
```

### Dry-run (WhatIf)

```
What if: Would configure branch protection for main
  - Required status check: build
  - Require up-to-date: Yes
  - Enforce admins: No
```

### Error

```
✗ Failed to configure branch protection
  Error: Resource not accessible by integration
  Hint: Ensure you have admin permissions on the repository
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Authentication failure |
| 2 | Permission denied |
| 3 | Repository not found |
| 4 | API error |

## API Call

```
PUT /repos/{owner}/{repo}/branches/{branch}/protection
```

### Request Body

```json
{
  "required_status_checks": {
    "strict": true,
    "contexts": ["build"]
  },
  "enforce_admins": false,
  "required_pull_request_reviews": null,
  "restrictions": null,
  "allow_force_pushes": false,
  "allow_deletions": false
}
```
