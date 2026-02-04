# Research: CLI Command `apply`

**Feature**: 009-cli-apply  
**Date**: February 3, 2026

## Overview

This document captures research findings for implementing the `apply` command. Since the feature primarily composes existing components (`LinkCommand` and `InstallCommand` functionality), research focuses on integration patterns and reusability.

## Research Tasks

### 1. Existing Command Patterns

**Question**: How do existing commands (`link`, `install`) structure their execution flow?

**Findings**:

- **LinkCommand** (sync): Uses `Command<LinkCommandSettings>` base
  - `Execute()` → `FindRepoRoot()` → `LoadAndResolveProfile()` → `ExecuteDryRun()` or `ExecuteLinking()`
  - Delegates to `LinkingOrchestrator.ExecuteLink()` for actual work
  - Returns `LinkExecutionResult` with conflicts, backups, and link results

- **InstallCommand** (async): Uses `AsyncCommand<InstallCommandSettings>` base
  - `ExecuteAsync()` → `FindRepoRoot()` → `ExecuteInstallAsync()` → `RunInstallersAsync()`
  - Creates installers inline and processes sequentially
  - Returns `List<InstallResult>` with status per item

**Decision**: Use `AsyncCommand` since install operations are async. Extract common setup logic into shared helper.

### 2. Profile Resolution

**Question**: How is profile inheritance resolved?

**Findings**:

- `ProfileMerger.Resolve(profileName)` returns `InheritanceResolveResult`
- Result contains `ResolvedProfile` with merged `Dotfiles` and `Install` block
- Handles `extends` chains automatically
- Already handles precedence (child overrides parent)

**Decision**: Reuse existing `ProfileMerger` - no changes needed.

### 3. Installation Priority Order

**Question**: What is the defined installation order and does existing code respect it?

**Findings**:

From spec (FR-007):
1. GitHub Releases
2. APT Packages  
3. Private APT Repositories
4. Shell Scripts
5. Fonts
6. Snap Packages

From `InstallCommand.RunInstallersAsync()`:
```csharp
var installers = new List<IInstallSource>
{
    new GithubReleaseInstaller(),
    new AptPackageInstaller(),
    new AptRepoInstaller(),
    new ScriptRunner(),
    new FontInstaller(),
    new SnapPackageInstaller(),
};
```

**Decision**: Existing order matches spec. Reuse the same installer list construction.

### 4. Conflict Detection and Force Behavior

**Question**: How does the existing `--force` flag work for linking?

**Findings**:

- `LinkingOrchestrator.ExecuteLink(profile, repoRoot, force)` accepts force parameter
- When `force=false` and conflicts exist: returns `LinkExecutionResult.Blocked(conflictResult)`
- When `force=true`: backs up conflicts via `BackupService`, then creates symlinks
- Backup results included in `LinkExecutionResult.BackupResults`

**Decision**: Pass through `--force` to `LinkingOrchestrator`. Apply command should fail early (before any installs) if conflicts exist and `--force` not provided.

### 5. Dry Run Behavior

**Question**: How should dry-run work across link + install?

**Findings**:

- `LinkCommand.ExecuteDryRun()`: Uses `ConflictDetector` and renders preview via `ConflictFormatter.WriteDryRunPreview()`
- `InstallCommand` passes `DryRun` in `InstallContext`, installers check and skip actual work

**Decision**: 
1. For dry-run: execute conflict detection for links, show what would be linked
2. For dry-run: pass through to installers which already support it
3. Combine into unified dry-run output showing both phases

### 6. Summary Rendering

**Question**: What existing renderers can be reused for verbose output?

**Findings**:

- `ConflictFormatter`: `WriteDryRunPreview()`, `WriteConflicts()`, `WriteBackupResults()`, `WriteLinkResults()`
- `InstallProgressRenderer`: `RenderSummary()`, `RenderGroupedFailures()`

**Decision**: Create `ApplyProgressRenderer` that:
1. Renders link phase summary (reuse formatters)
2. Renders install phase summary (reuse formatters)
3. Adds overall apply summary with total counts

### 7. Exit Code Strategy

**Question**: What exit code should apply return in fail-soft mode?

**Findings**:

- `LinkCommand`: Returns 1 on blocked (conflicts), 0 on success
- `InstallCommand`: Returns 1 if any `InstallResult.Status == Failed`, 0 otherwise

**Decision**: 
- Return 1 if linking fails (blocks command)
- Return 1 if any installation fails (fail-soft means continue but report failure)
- Return 0 only if all operations succeed or skip

## Technology Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Command base | `AsyncCommand<ApplyCommandSettings>` | Install operations are async |
| Profile resolution | Reuse `ProfileMerger` | Already handles inheritance |
| Linking | Reuse `LinkingOrchestrator` | Full conflict/backup/link flow |
| Installing | Reuse existing installers | Maintain priority order |
| Output | New `ApplyProgressRenderer` | Combine link + install summaries |

## Alternatives Considered

### Alternative 1: Subcommand Delegation

**Description**: Have `apply` literally call `link` and `install` as subprocesses.

**Rejected Because**: 
- Loses type safety and structured error handling
- Duplicates config loading
- Can't provide unified progress/summary

### Alternative 2: Shared Orchestrator Service

**Description**: Extract a new `ApplyOrchestrator` in `Dottie.Configuration`.

**Rejected Because**:
- Premature abstraction - `ApplyCommand` can directly compose existing orchestrators
- Would add indirection without clear benefit
- Constitution prefers avoiding over-abstraction

## Open Questions

None - all questions resolved through codebase analysis.
