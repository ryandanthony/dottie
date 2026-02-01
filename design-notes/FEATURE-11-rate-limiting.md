# FEATURE-11: GitHub API Rate Limiting

## Overview

Handle GitHub API rate limiting gracefully when downloading releases. This feature ensures users receive clear guidance when rate limits are encountered and can continue using Dottie effectively.

## Problem Statement

GitHub's API has rate limits:
- **Unauthenticated**: 60 requests/hour per IP
- **Authenticated**: 5,000 requests/hour per token

When users hit rate limits during `dottie install`, they receive a 403 or 429 response with no guidance on how to proceed.

## Proposed Solution

### Detection

- Detect HTTP 403/429 responses from GitHub API
- Parse `X-RateLimit-Remaining` and `X-RateLimit-Reset` headers
- Distinguish between rate limiting and actual authorization failures

### User Guidance

When rate limited, display:
1. Clear message that rate limit was hit
2. Time until limit resets (parsed from header)
3. Suggestion to authenticate with `GITHUB_TOKEN` (if not already authenticated)
4. Option to retry with `--retry-after <minutes>` or wait

### Example Output

```
⚠️ GitHub API rate limit exceeded (0/60 requests remaining)
   Resets at: 2026-01-31 15:30:00 UTC (in 45 minutes)
   
   To increase your rate limit to 5,000 requests/hour:
   export GITHUB_TOKEN=<your-personal-access-token>
   
   Skipping remaining GitHub release installations...
```

## Dependencies

- Requires FEATURE-12 (GitHub Authentication) for the auth suggestion to be actionable

## Acceptance Criteria

- [ ] System detects GitHub API rate limit responses (403/429)
- [ ] System displays remaining requests and reset time
- [ ] System suggests authentication when unauthenticated
- [ ] System continues with non-GitHub sources when rate limited
- [ ] System reports skipped GitHub items in final summary

## Open Questions

- Should we cache GitHub API responses to reduce request count?
- Should we support automatic retry after rate limit reset?
- How should we handle rate limits in `--dry-run` mode?

## References

- [GitHub Rate Limiting Documentation](https://docs.github.com/en/rest/rate-limit)
- FEATURE-06: Installation Sources (parent feature)
