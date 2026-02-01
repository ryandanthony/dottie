# FEATURE-12: GitHub API Authentication

## Overview

Support optional GitHub API authentication to increase rate limits and access private repositories when downloading releases.

## Problem Statement

Without authentication:
- Rate limit is 60 requests/hour (easily exceeded with multiple GitHub release items)
- Cannot access releases from private repositories
- Cannot access releases from organization repos with restricted access

## Proposed Solution

### Authentication Method

Support `GITHUB_TOKEN` environment variable containing a GitHub Personal Access Token (PAT) or fine-grained token.

```bash
export GITHUB_TOKEN=ghp_xxxxxxxxxxxxxxxxxxxx
dottie install
```

### Token Handling

- Read token from `GITHUB_TOKEN` environment variable
- Include as `Authorization: Bearer <token>` header on GitHub API requests
- Never log, display, or persist the token value
- Validate token format before use (starts with `ghp_`, `gho_`, or `github_pat_`)

### Scope Requirements

For public repositories:
- No special scopes required (public_repo read is implicit)

For private repositories:
- `repo` scope required for classic PATs
- "Contents: Read" permission for fine-grained tokens

### Error Handling

- Invalid token: Clear error message, suggest checking token value
- Insufficient permissions: Identify which repo failed, suggest required scope
- Expired token: Detect and guide user to refresh

## Security Considerations

- Token MUST NOT be logged in any verbosity mode
- Token MUST NOT be included in error messages
- Token MUST NOT be persisted to any file
- Mask token in any debug output (show only last 4 chars)

## Configuration

No configuration file support initially. Environment variable only to:
- Avoid accidental commits of tokens
- Follow 12-factor app principles
- Align with CI/CD patterns (secrets as env vars)

## Acceptance Criteria

- [ ] System reads `GITHUB_TOKEN` from environment variable
- [ ] System includes auth header when token is present
- [ ] System never logs or displays token value
- [ ] System provides clear error for invalid/expired tokens
- [ ] System works without token (falls back to unauthenticated)
- [ ] Rate limit increases to 5,000/hour when authenticated

## Open Questions

- Should we support GitHub App authentication for organization use?
- Should we support `~/.config/gh/hosts.yml` (GitHub CLI) token discovery?
- Should we validate token before starting installations?

## References

- [GitHub Authentication Documentation](https://docs.github.com/en/rest/authentication)
- [Creating a Personal Access Token](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token)
- FEATURE-06: Installation Sources (parent feature)
- FEATURE-11: Rate Limiting (related feature)
