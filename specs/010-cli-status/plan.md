# Implementation Plan: CLI Command `status`

**Branch**: `010-cli-status` | **Date**: February 3, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-cli-status/spec.md`

**Note**: This plan adds a new `StatusCommand` to the existing CLI, leveraging existing infrastructure for profile resolution, conflict detection, and installation status checking.

## Summary

Add a `dottie status` CLI command that displays the current state of dotfiles (linked/missing/broken/conflicting/unknown) and software installations (installed/missing/outdated). The command provides an informational overview without making changes, helping users understand what actions they need to take.

## Technical Context

**Language/Version**: C# 12 / .NET 9.0
**Primary Dependencies**: Spectre.Console (CLI/output), YamlDotNet (config parsing)
**Storage**: N/A (read-only filesystem inspection)
**Testing**: xUnit, FluentAssertions, NSubstitute
**Target Platform**: Linux (Ubuntu LTS primary), cross-platform for development
**Project Type**: Single project with CLI + Configuration libraries
**Performance Goals**: Display complete status within 5 seconds (spec SC-001)
**Constraints**: Read-only operation, exit code 0 on success regardless of item states
**Scale/Scope**: Single-user dotfiles tool, typical configs have 10-30 items

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Safety & Explicitness | ✅ Pass | Read-only command; no modifications to filesystem |
| Idempotency | ✅ Pass | Pure inspection; running multiple times yields same output |
| Security | ✅ Pass | No network calls; no credentials needed |
| Security values | ✅ Pass | No secrets involved; read-only inspection |
| Compatibility | ✅ Pass | Ubuntu-first; uses standard symlink APIs |
| Observability | ✅ Pass | Clear status output with actionable information |
| Decision rules | ✅ Pass | Leverages existing ConflictDetector and installer detection |
| Guiding principles | ✅ Pass | Composable with existing infrastructure |
| Testing philosophy | ✅ Pass | TDD with behavior-focused tests |

## Project Structure

### Documentation (this feature)

```text
specs/010-cli-status/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (N/A - no API contracts)
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
src/
├── Dottie.Cli/
│   ├── Commands/
│   │   ├── StatusCommand.cs          # NEW - main command implementation
│   │   ├── StatusCommandSettings.cs  # NEW - command settings
│   │   └── ProfileAwareSettings.cs   # EXISTS - base class for settings
│   └── Output/
│       └── StatusFormatter.cs        # NEW - formats status output
└── Dottie.Configuration/
    ├── Status/                       # NEW - status checking logic
    │   ├── DotfileLinkState.cs       # NEW - enum for dotfile states
    │   ├── DotfileStatusEntry.cs     # NEW - result for single dotfile
    │   ├── DotfileStatusChecker.cs   # NEW - checks dotfile link states
    │   ├── SoftwareInstallState.cs   # NEW - enum for software states
    │   ├── SoftwareStatusEntry.cs    # NEW - result for single software item
    │   ├── SoftwareStatusChecker.cs  # NEW - checks software install states
    │   └── StatusReport.cs           # NEW - aggregated status result
    ├── Linking/
    │   └── ConflictDetector.cs       # EXISTS - reuse for conflict detection
    └── Installing/
        └── [various installers]      # EXISTS - reuse for "is installed" checks

tests/
├── Dottie.Cli.Tests/
│   └── Commands/
│       └── StatusCommandTests.cs     # NEW - command behavior tests
└── Dottie.Configuration.Tests/
    └── Status/                       # NEW - status checking tests
        ├── DotfileStatusCheckerTests.cs  # NEW
        └── SoftwareStatusCheckerTests.cs # NEW

tests/integration/scenarios/
    └── status-command/               # NEW - integration test scenario
        ├── dottie.yaml
        ├── dotfiles/
        └── validate.sh
```

**Structure Decision**: Creates a new `Status/` namespace in `Dottie.Configuration` for status-checking logic, keeping it separate from the action-oriented `Linking/` and `Installing/` namespaces. The CLI command follows the existing pattern established by `LinkCommand` and `InstallCommand`.

## Complexity Tracking

> No constitution violations. Using existing infrastructure where possible.

| Area | Approach | Justification |
|------|----------|---------------|
| Dotfile state detection | Enhance existing `ConflictDetector` patterns | Already detects linked/conflict states; extend for broken/unknown |
| Software install check | Reuse installer "already installed" logic | `GithubReleaseInstaller` already has binary detection |
| Profile resolution | Use existing `ProfileMerger` | Already handles inheritance chain |
| Output formatting | New `StatusFormatter` | Distinct from `ConflictFormatter`; status-specific layout |

## Constitution Check (Post-Design)

*Re-evaluated after Phase 1 design completion.*

| Principle | Status | Verification |
|-----------|--------|--------------|
| Safety & Explicitness | ✅ Pass | Read-only command; no filesystem modifications |
| Idempotency | ✅ Pass | Pure inspection; same output for same state |
| Security | ✅ Pass | No network calls; no secrets; read-only |
| Security values | ✅ Pass | No credentials involved; local inspection only |
| Compatibility | ✅ Pass | Ubuntu-first; standard symlink/process APIs |
| Observability | ✅ Pass | Clear status output; actionable information |
| Decision rules | ✅ Pass | Reuses existing infrastructure; no over-engineering |
| Guiding principles | ✅ Pass | Small, composable Status/ namespace |
| Testing philosophy | ✅ Pass | TDD approach; behavior-focused tests |

**Gate Status**: ✅ PASSED - Ready for Phase 2 task generation
