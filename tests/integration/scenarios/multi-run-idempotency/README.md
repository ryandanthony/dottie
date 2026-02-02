# Multi-Run Idempotency Scenario

## Purpose
Test that running dottie commands multiple times produces consistent results with no errors or side effects.

## What is tested
1. **Idempotent link command** - Running `dottie link` twice should produce the same result
2. **No duplicate errors** - Multiple runs shouldn't create duplicate symlinks or errors
3. **Config consistency** - Configuration remains valid across multiple loads
4. **State consistency** - Existing symlinks aren't recreated or modified unnecessarily

## Expected behavior
1. First `dottie link` succeeds and creates symlinks
2. Second `dottie link` succeeds and exits cleanly (symlinks already exist)
3. Both runs produce the same output indicating same actions
4. No errors or warnings about existing files
5. Symlinks are unchanged and still valid

## Rationale
This ensures dottie can be safely re-run (via cron, configuration automation, etc.) without issues.
