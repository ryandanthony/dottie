# Implementation Plan: CLI Command `install`

**Branch**: `007-cli-install` | **Date**: February 1, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-cli-install/spec.md`

**Note**: This plan enhances the existing `InstallCommand` implementation to fully meet the specification requirements.

## Summary

Enhance the existing `dottie install` CLI command to fully support profile inheritance resolution, improved dry-run with system state checking, grouped failure summaries, and better "already installed" detection. The existing infrastructure (installers, orchestrator, output renderer) provides a solid foundation—this plan fills gaps to meet spec requirements.

## Technical Context

**Language/Version**: C# 12 / .NET 9.0
**Primary Dependencies**: Spectre.Console (CLI/output), Flurl.Http (HTTP), YamlDotNet (config)
**Storage**: N/A (filesystem operations only)
**Testing**: xUnit, FluentAssertions, NSubstitute
**Target Platform**: Linux (Ubuntu LTS primary), cross-platform for development
**Project Type**: Single project with CLI + Configuration libraries
**Performance Goals**: Process 50 installation items within 5 minutes (excluding download/install time)
**Constraints**: Idempotent operations, no silent overwrites, clear error messages
**Scale/Scope**: Single-user dotfiles tool, typical configs have 10-30 install items

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Safety & Explicitness | ✅ Pass | --dry-run default behavior, no silent changes |
| Idempotency | ✅ Pass | Skip already-installed tools, re-running safe |
| Security | ✅ Pass | Scripts must be in-repo, GITHUB_TOKEN for auth |
| Security values | ✅ Pass | Token from env var, not stored |
| Compatibility | ✅ Pass | Ubuntu-first, package manager integration |
| Observability | ✅ Pass | Progress display, grouped failure summary |
| Decision rules | ✅ Pass | Uses existing installer infrastructure |
| Guiding principles | ✅ Pass | Extends existing code, no over-abstraction |
| Testing philosophy | ✅ Pass | TDD with behavior-focused tests |

## Project Structure

### Documentation (this feature)

```text
specs/007-cli-install/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (existing structure)

```text
src/
├── Dottie.Cli/
│   ├── Commands/
│   │   ├── InstallCommand.cs         # EXISTS - enhance
│   │   ├── InstallCommandSettings.cs # EXISTS - no changes needed
│   │   └── ProfileAwareSettings.cs   # EXISTS - no changes needed
│   └── Output/
│       └── InstallProgressRenderer.cs # EXISTS - enhance for grouped failures
└── Dottie.Configuration/
    ├── Inheritance/
    │   └── ProfileMerger.cs          # EXISTS - use for inheritance
    └── Installing/
        ├── GithubReleaseInstaller.cs # EXISTS - enhance binary detection
        ├── AptPackageInstaller.cs    # EXISTS - no changes needed
        ├── InstallContext.cs         # EXISTS - no changes needed
        └── InstallResult.cs          # EXISTS - no changes needed

tests/
├── Dottie.Cli.Tests/
│   └── Commands/
│       └── InstallCommandTests.cs    # EXISTS - add tests
└── Dottie.Configuration.Tests/
    └── Installing/
        └── GithubReleaseInstallerTests.cs # EXISTS - add detection tests
```

**Structure Decision**: Uses existing single-project structure. All required infrastructure exists; implementation involves enhancing existing classes rather than creating new ones.

## Complexity Tracking

> No constitution violations. Using existing infrastructure.

| Area | Approach | Justification |
|------|----------|---------------|
| Profile inheritance | Use existing `ProfileMerger` | Already implements full inheritance chain resolution |
| Binary detection | Enhance `GithubReleaseInstaller` | Check ~/bin/ first, then PATH lookup |
| Failure grouping | Enhance `InstallProgressRenderer` | Collect failures, display grouped summary at end |

## Constitution Check (Post-Design)

*Re-evaluated after Phase 1 design completion.*

| Principle | Status | Verification |
|-----------|--------|--------------|
| Safety & Explicitness | ✅ Pass | --dry-run shows accurate preview; no hidden changes |
| Idempotency | ✅ Pass | Binary detection skips already-installed; re-running safe |
| Security | ✅ Pass | Scripts in-repo only; GITHUB_TOKEN from env, not stored |
| Security values | ✅ Pass | No secrets in code; fail on invalid version (no fallback) |
| Compatibility | ✅ Pass | Ubuntu-first; `which` is POSIX-standard |
| Observability | ✅ Pass | Grouped failure summary; clear error messages |
| Decision rules | ✅ Pass | Enhances existing code; no new abstractions |
| Guiding principles | ✅ Pass | Small changes to existing components |
| Testing philosophy | ✅ Pass | TDD approach; tests before implementation |

**Gate Status**: ✅ PASSED - Ready for Phase 2 task generation
