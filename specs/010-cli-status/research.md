# Research: CLI Command `status`

**Feature**: 010-cli-status
**Date**: February 3, 2026

## Research Summary

This feature adds a new read-only `StatusCommand` to display the current state of dotfiles and software installations. Research focuses on reusing existing infrastructure and defining new state enumeration.

## Gap Analysis

### Current State vs. Specification

| Requirement | Current State | Gap |
|-------------|---------------|-----|
| FR-001: Display dotfile states (linked/missing/broken/conflicting) | ⚠️ `ConflictDetector` handles some | Need to extend for broken links, add "unknown" state |
| FR-002: Display software states (installed/missing/outdated) | ⚠️ Installers have partial detection | Need unified status check interface |
| FR-003: Resolve profile inheritance | ✅ `ProfileMerger` exists | None |
| FR-004: `--profile` option | ✅ Pattern in `ProfileAwareSettings` | None |
| FR-005: Default profile when not specified | ✅ Pattern in existing commands | None |
| FR-006: Human-readable output | ⚠️ `ConflictFormatter` exists | Need new `StatusFormatter` |
| FR-007: Outdated software detection | ⚠️ Version pinning exists | Need version comparison |
| FR-008: Error on non-existent profile | ✅ Pattern in existing commands | None |
| FR-009: Handle invalid config | ✅ `ConfigurationLoader` handles | None |
| FR-010: Broken symlink detection | ❌ Not detected | Need to add |
| FR-011: Empty config message | ❌ Not handled | Need to add |
| FR-012: Output organization (dotfiles, then software) | ❌ New requirement | Design output format |
| FR-013: Unknown state for permission errors | ❌ Not handled | Need to add |
| FR-014: Exit code 0 on success | ✅ Standard pattern | None |

## Decisions

### Decision 1: Dotfile State Detection Strategy

**Decision**: Create new `DotfileStatusChecker` that extends `ConflictDetector` patterns to detect all five states (linked/missing/broken/conflicting/unknown).

**Rationale**:
- `ConflictDetector` already handles: linked (via `AlreadyLinked`), conflicting (via `Conflicts`), and safe-to-link (implicit missing)
- Need to add: broken link detection (symlink exists but target doesn't), unknown state (access errors)
- Keeping detection logic in `Configuration` library maintains separation of concerns

**State Definitions**:
| State | Condition |
|-------|-----------|
| Linked | Symlink exists and points to correct source |
| Missing | Target path does not exist (no file, no symlink) |
| Broken | Symlink exists but points to non-existent file |
| Conflicting | File/directory exists but is not the expected symlink |
| Unknown | Cannot determine state (permission error, access issue) |

**Alternatives Considered**:
- Modify `ConflictDetector` directly - rejected to avoid breaking existing linking workflow
- Reuse `ConflictResult` types - rejected as status has different semantics (informational vs. action-blocking)

### Decision 2: Software Installation State Detection

**Decision**: Create `SoftwareStatusChecker` that queries each installer type for "is installed" status using existing detection logic.

**Rationale**:
- `GithubReleaseInstaller` already has binary detection (~/bin/ + PATH)
- APT packages can be checked via `dpkg -s <package>`
- Snap packages can be checked via `snap list`
- Fonts can be checked via file existence in fonts directory
- Scripts: N/A (no persistent state to check)

**State Definitions**:
| State | Condition |
|-------|-----------|
| Installed | Tool is present on system |
| Missing | Tool is not found |
| Outdated | Tool is installed but version doesn't match pinned version (GitHub releases only) |
| Unknown | Cannot determine state (detection error) |

**Detection Methods by Source Type**:
| Source | Detection Method | Version Check |
|--------|-----------------|---------------|
| GitHub Release | File exists in ~/bin/ or `which` returns path | Check binary --version output |
| APT Package | `dpkg -s <package>` exit code 0 | `dpkg -s` shows version |
| Snap Package | `snap list <package>` exit code 0 | `snap list` shows version |
| Font | File exists in fonts directory | N/A (no version) |
| Script | N/A | N/A |
| APT Repo | N/A (repository only) | N/A |

### Decision 3: Output Format Design

**Decision**: Organize output by category with Dotfiles section first, then Software section. Use Spectre.Console tables for clean formatting.

**Rationale**:
- Per spec clarification: group by category (FR-012)
- Matches mental model: "what's linked" vs "what's installed"
- Tables provide consistent alignment and readability

**Output Format**:
```text
Profile: work (inherited from: default)

═══ Dotfiles ═══
┌─────────────────────┬────────────┬──────────────────────────┐
│ Source              │ Status     │ Target                   │
├─────────────────────┼────────────┼──────────────────────────┤
│ dotfiles/bashrc     │ ✓ Linked   │ ~/.bashrc                │
│ dotfiles/vimrc      │ ○ Missing  │ ~/.vimrc                 │
│ dotfiles/config     │ ✗ Conflict │ ~/.config (existing dir) │
│ dotfiles/old        │ ⚠ Broken   │ ~/.old (target missing)  │
└─────────────────────┴────────────┴──────────────────────────┘

═══ Software ═══
┌──────────────────┬────────────────┬─────────────────────────┐
│ Item             │ Status         │ Details                 │
├──────────────────┼────────────────┼─────────────────────────┤
│ [GitHub] ripgrep │ ✓ Installed    │ ~/bin/rg                │
│ [GitHub] fd      │ ⬆ Outdated     │ 8.7.0 → 9.0.0           │
│ [APT] git        │ ✓ Installed    │                         │
│ [APT] htop       │ ○ Missing      │                         │
│ [Font] JetBrains │ ✓ Installed    │                         │
└──────────────────┴────────────────┴─────────────────────────┘

Summary: 1 linked, 1 missing, 1 conflict, 1 broken | 3 installed, 1 missing, 1 outdated
```

### Decision 4: Empty Section Handling

**Decision**: When a section has no items, display a friendly message instead of an empty table.

**Rationale**:
- Per spec clarification: display "no items configured" message (FR-011)
- Provides clear feedback rather than confusing empty output

**Format**:
```text
═══ Software ═══
No software items configured for this profile.
```

### Decision 5: Version Detection for Outdated Status

**Decision**: Only support "outdated" detection for GitHub releases with pinned versions. APT/Snap version comparison is out of scope for initial implementation.

**Rationale**:
- GitHub releases have explicit `version` field in config
- Binary version detection is straightforward: run `binary --version` and parse
- APT/Snap version comparison requires additional complexity (semantic versioning, repository versions)
- Can extend in future if needed

**Limitations**:
- APT packages: show "Installed" only (no version comparison)
- Snap packages: show "Installed" only (no version comparison)
- GitHub releases without pinned version: show "Installed" only

### Decision 6: Permission Error Handling

**Decision**: Catch access exceptions during status checks and report "Unknown" state with brief error message.

**Rationale**:
- Per spec clarification: show "unknown" state with error reason (FR-013)
- Graceful degradation - one inaccessible item shouldn't fail entire command
- Helps users diagnose permission issues

**Implementation**:
```csharp
try
{
    // Check symlink status
}
catch (UnauthorizedAccessException ex)
{
    return new DotfileStatusEntry(entry, DotfileLinkState.Unknown, $"Permission denied: {ex.Message}");
}
```

## Existing Infrastructure Reuse

### Components to Reuse

| Component | Usage |
|-----------|-------|
| `ProfileMerger` | Resolve profile inheritance to get full dotfile/install lists |
| `ConfigurationLoader` | Load and parse dottie.yaml |
| `ProfileAwareSettings` | Base class for command settings |
| `RepoRootFinder` | Find repository root directory |
| `ConflictDetector.ExpandPath()` | Expand ~ in target paths |
| Binary detection pattern | From `GithubReleaseInstaller` for ~/bin/ + PATH checks |
| `IProcessRunner` | Execute `dpkg`, `snap`, and version check commands |

### Patterns to Follow

| Pattern | Source | Application |
|---------|--------|-------------|
| Command structure | `LinkCommand`, `InstallCommand` | `StatusCommand` follows same layout |
| Settings inheritance | `ProfileAwareSettings` | `StatusCommandSettings` extends it |
| Output formatting | `ConflictFormatter` | `StatusFormatter` follows similar patterns |
| Error display | `ErrorFormatter` | Reuse for config load errors |
| Test structure | `ConflictDetectorTests` | File-based test fixtures |

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Version parsing fails for some binaries | Medium | Low | Catch exceptions, report "Unknown" |
| `which` command not available | Low | Medium | Fall back to PATH parsing |
| Slow status checks for large configs | Low | Medium | No network calls; filesystem only |
| Symlink permission errors on Windows | Medium | Low | Report "Unknown" state gracefully |
