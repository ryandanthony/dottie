# Research: CLI Command `install`

**Feature**: 007-cli-install
**Date**: February 1, 2026

## Research Summary

This feature enhances the existing `InstallCommand` rather than building from scratch. The primary research focuses on gaps between current implementation and specification requirements.

## Gap Analysis

### Current State vs. Specification

| Requirement | Current State | Gap |
|-------------|---------------|-----|
| FR-001: Read install sources from resolved profile | ✅ Reads from profile | None |
| FR-002: Resolve profile inheritance | ❌ Direct profile lookup only | Need to use `ProfileMerger.Resolve()` |
| FR-003: Execute in priority order | ✅ Hardcoded order in loop | None |
| FR-004: Detect already-installed, skip | ⚠️ Partial - varies by installer | Need ~/bin/ + PATH check for GitHub |
| FR-005: Default to "default" profile | ✅ Implemented | None |
| FR-006: Validate profile exists | ✅ Returns error if not found | None |
| FR-007: --dry-run flag | ✅ Implemented | None |
| FR-008: Display planned installations in dry-run | ⚠️ Shows validation, not system state | Need to check installed status |
| FR-009: Display progress | ✅ Shows per-item results | None |
| FR-010: Summary with grouped failures | ⚠️ Shows counts only | Need grouped failure list |
| FR-011: Continue on failure | ✅ Implemented | None |
| FR-012: Non-zero exit on failure | ✅ Implemented | None |
| FR-021: GITHUB_TOKEN support | ✅ Already reads from env | None |
| FR-022: Binary detection ~/bin/ then PATH | ❌ Not implemented | Need to add |

## Decisions

### Decision 1: Profile Inheritance Integration

**Decision**: Use existing `ProfileMerger.Resolve()` in `InstallCommand.ExecuteInstallAsync()`

**Rationale**: 
- `ProfileMerger` already handles full inheritance chain resolution
- Returns `ResolvedProfile` with merged `Install` block
- Handles cycle detection and error reporting

**Alternatives Considered**:
- Manual recursion through `Extends` property - rejected as duplicates existing logic
- Creating new resolver - rejected as violates DRY principle

### Decision 2: Binary Detection Strategy

**Decision**: Check ~/bin/ first, then use `which` command for PATH lookup

**Rationale**:
- ~/bin/ is dottie's install location - checking here first is fastest
- PATH lookup catches tools installed by other means (user's prior installations)
- `which` is POSIX-standard and available on Ubuntu

**Alternatives Considered**:
- Only check ~/bin/ - rejected per spec clarification (option C chosen)
- Use .NET's `Environment.GetEnvironmentVariable("PATH")` parsing - more complex, platform differences

### Decision 3: Dry-Run System State Checking

**Decision**: During dry-run, perform actual binary detection but skip installation

**Rationale**:
- Per spec clarification: dry-run should show "would install" vs "would skip"
- Users need accurate preview to make informed decisions
- Matches behavior of `apt-get --dry-run`

**Alternatives Considered**:
- Skip system checks in dry-run (faster) - rejected per clarification session

### Decision 4: Grouped Failure Summary Format

**Decision**: Collect all failures during execution, display grouped summary after all results

**Rationale**:
- Per spec clarification: grouped summary at end (not inline during execution)
- Keeps progress output clean
- Provides actionable list for user to address

**Implementation**:
```text
Installation Summary:
  ✓ Succeeded: 5
  ✗ Failed: 2
  ⊘ Skipped: 3

Failed Installations:
  [GitHub] ripgrep: Version 99.0.0 not found
  [APT] nonexistent-pkg: Package not found in repositories
```

### Decision 5: GitHub Rate Limit Error Message

**Decision**: On 403/rate limit, suggest setting GITHUB_TOKEN

**Rationale**:
- Per spec: support GITHUB_TOKEN (already implemented in GithubReleaseInstaller)
- Need helpful error message when rate limited

**Error Message Format**:
```text
GitHub API rate limit exceeded. Set GITHUB_TOKEN environment variable for higher limits.
```

## Technology Patterns

### Existing Patterns to Follow

1. **Command Pattern**: Inherit from `AsyncCommand<TSettings>` (Spectre.Console.Cli)
2. **Result Objects**: Use `InstallResult` factory methods (`Success`, `Skipped`, `Failed`)
3. **Installer Interface**: `IInstallSource.InstallAsync()` returns `IEnumerable<InstallResult>`
4. **Output Rendering**: Separate renderer class (`InstallProgressRenderer`) for testability

### Binary Existence Check Pattern

```csharp
// Check ~/bin/ first (dottie's install location)
var binPath = Path.Combine(context.BinDirectory, binaryName);
if (File.Exists(binPath))
{
    return InstallResult.Skipped(binaryName, SourceType, "Already installed in ~/bin/");
}

// Fall back to PATH check using 'which'
var whichResult = await _processRunner.RunAsync("which", binaryName);
if (whichResult.ExitCode == 0)
{
    return InstallResult.Skipped(binaryName, SourceType, $"Already installed at {whichResult.Output.Trim()}");
}
```

## Dependencies

### Existing Dependencies (no changes needed)

- **Spectre.Console** (2.0.0): CLI framework and rich output
- **Flurl.Http** (4.0.2): HTTP client for GitHub API
- **YamlDotNet** (16.3.0): YAML configuration parsing

### No New Dependencies Required

All functionality can be implemented with existing dependencies and .NET BCL.

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| `which` command not available | Low | Medium | Use `command -v` as fallback (POSIX) |
| Profile inheritance breaks existing configs | Low | High | Inheritance is additive; no breaking changes |
| Rate limit errors confuse users | Medium | Low | Clear error message with GITHUB_TOKEN hint |

## Implementation Notes

### Files to Modify

1. **InstallCommand.cs**: Replace direct profile lookup with `ProfileMerger.Resolve()`
2. **GithubReleaseInstaller.cs**: Add binary existence check before download
3. **InstallProgressRenderer.cs**: Add grouped failure summary method

### Files to Add

None - all changes are enhancements to existing classes.

### Test Files to Modify

1. **InstallCommandTests.cs**: Add tests for profile inheritance, dry-run state checking
2. **GithubReleaseInstallerTests.cs**: Add tests for binary detection logic
