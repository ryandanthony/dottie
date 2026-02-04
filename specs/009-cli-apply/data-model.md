# Data Model: CLI Command `apply`

**Feature**: 009-cli-apply  
**Date**: February 3, 2026

## Overview

The `apply` command primarily orchestrates existing data models. This document captures new models needed for the unified apply operation and their relationships to existing models.

## New Entities

### ApplyResult

Aggregates results from both linking and installation phases.

| Field | Type | Description |
|-------|------|-------------|
| LinkPhase | LinkPhaseResult | Results from dotfile linking phase |
| InstallPhase | InstallPhaseResult | Results from software installation phase |
| OverallSuccess | bool | True only if both phases completed without failures |

### LinkPhaseResult

Wrapper for linking operation results with phase-level metadata.

| Field | Type | Description |
|-------|------|-------------|
| WasExecuted | bool | False if skipped (e.g., no dotfiles in profile) |
| WasBlocked | bool | True if conflicts prevented linking |
| ExecutionResult | LinkExecutionResult? | Detailed link results (existing model) |

### InstallPhaseResult

Wrapper for installation operation results with phase-level metadata.

| Field | Type | Description |
|-------|------|-------------|
| WasExecuted | bool | False if skipped (e.g., no install block in profile) |
| Results | IReadOnlyList\<InstallResult\> | Individual install results (existing model) |
| HasFailures | bool | True if any install failed |

### ApplyOperationSummary

Model for verbose summary output (FR-010).

| Field | Type | Description |
|-------|------|-------------|
| ProfileName | string | Name of applied profile |
| LinksSummary | LinksSummary | Aggregated link statistics |
| InstallsSummary | InstallsSummary | Aggregated install statistics |
| TotalOperations | int | Total count of all operations |
| SuccessCount | int | Operations that succeeded |
| FailedCount | int | Operations that failed |
| SkippedCount | int | Operations that were skipped |

### LinksSummary

| Field | Type | Description |
|-------|------|-------------|
| Created | int | New symlinks created |
| Skipped | int | Already-linked entries skipped |
| BackedUp | int | Files backed up before overwrite |
| Failed | int | Link operations that failed |

### InstallsSummary

| Field | Type | Description |
|-------|------|-------------|
| Installed | int | Packages/tools successfully installed |
| Skipped | int | Already-installed items skipped |
| Failed | int | Installation failures |
| BySource | Dictionary\<InstallSourceType, SourceSummary\> | Breakdown by source type |

### SourceSummary

| Field | Type | Description |
|-------|------|-------------|
| Installed | int | Items installed from this source |
| Skipped | int | Items skipped from this source |
| Failed | int | Items failed from this source |

## Existing Models (Reused)

### From Dottie.Configuration.Linking

- **LinkExecutionResult**: Contains conflict result, backup results, link operation result
- **ConflictResult**: Lists safe entries, already-linked, and conflicts
- **BackupResult**: Source path, backup path, success status
- **LinkOperationResult**: Successful, skipped, and failed link results

### From Dottie.Configuration.Installing

- **InstallResult**: Name, source type, status (Success/Failed/Skipped), message
- **InstallContext**: Repo root, has sudo, dry run flag
- **InstallBlock**: Contains all install source configurations

### From Dottie.Configuration.Inheritance

- **ResolvedProfile**: Merged profile with dotfiles and install block
- **InheritanceResolveResult**: Success/failure with resolved profile or error

## Entity Relationships

```text
ApplyCommand
    │
    ├── uses → ProfileMerger (existing)
    │              └── produces → ResolvedProfile (existing)
    │
    ├── delegates to → LinkingOrchestrator (existing)
    │                      └── produces → LinkExecutionResult (existing)
    │                                         └── wrapped in → LinkPhaseResult (new)
    │
    ├── delegates to → Installers (existing)
    │                      └── produces → List<InstallResult> (existing)
    │                                         └── wrapped in → InstallPhaseResult (new)
    │
    └── aggregates into → ApplyResult (new)
                              └── rendered via → ApplyOperationSummary (new)
```

## State Transitions

### Apply Operation States

```text
┌─────────────┐
│   START     │
└──────┬──────┘
       │
       ▼
┌─────────────┐   config error    ┌─────────────┐
│ Load Config │ ────────────────► │   FAILED    │
└──────┬──────┘                   └─────────────┘
       │ success
       ▼
┌─────────────┐   profile error   ┌─────────────┐
│Resolve Prof │ ────────────────► │   FAILED    │
└──────┬──────┘                   └─────────────┘
       │ success
       ▼
┌─────────────┐  conflicts &      ┌─────────────┐
│ Link Phase  │  !force           │   BLOCKED   │
└──────┬──────┘ ────────────────► └─────────────┘
       │ success or force
       ▼
┌─────────────┐   (fail-soft)     
│Install Phase│ ──────────────────┐
└──────┬──────┘                   │
       │                          │
       ▼                          ▼
┌─────────────┐              ┌─────────────┐
│  SUCCESS    │              │PARTIAL FAIL │
│ (exit 0)    │              │ (exit 1)    │
└─────────────┘              └─────────────┘
```

## Validation Rules

### ApplyCommandSettings Validation

| Rule | Constraint |
|------|------------|
| ProfileName | Optional; defaults to "default" |
| ConfigPath | Optional; defaults to `{repoRoot}/dottie.yaml` |
| Force | Boolean; defaults to false |
| DryRun | Boolean; defaults to false |

### Pre-execution Validation

1. Configuration file must exist
2. Configuration must parse successfully
3. Specified profile must exist in configuration
4. Profile must have at least one operation (dotfiles or install block)
