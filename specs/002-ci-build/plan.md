# Implementation Plan: CI/CD Build Pipeline

**Branch**: `002-ci-build` | **Date**: January 28, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-ci-build/spec.md`

## Summary

Implement a complete CI/CD pipeline using GitHub Actions with GitVersion for semantic versioning, automated testing with coverage enforcement (80% threshold), cross-platform artifact publishing (linux-x64, win-x64), and automatic GitHub Releases on successful main branch builds. Integration tests run in Docker for cross-platform consistency. Includes a PowerShell script to configure branch protection rules using the `gh` CLI.

## Technical Context

**Language/Version**: .NET 10 (C# 13)  
**Primary Dependencies**: GitVersion (semantic versioning), ReportGenerator (coverage), GitHub Actions, Docker  
**Storage**: N/A (CI/CD infrastructure)  
**Testing**: xunit, coverlet (existing), Docker for integration tests  
**Target Platform**: GitHub Actions (ubuntu-latest runner)
**Project Type**: Single solution with CLI + library projects  
**Performance Goals**: Build feedback within 10 minutes, release creation within 5 minutes of build completion  
**Constraints**: 80% minimum code coverage, 7-day artifact retention  
**Scale/Scope**: Single repository, 2 target platforms (linux-x64, win-x64)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Safety & Explicitness | ✅ PASS | No destructive operations; build creates artifacts, doesn't modify repo directly |
| Idempotency | ✅ PASS | Pipeline runs are independent; same commit produces same version |
| Security | ✅ PASS | Uses GitHub Actions built-in secrets; no custom installers; gh CLI is trusted |
| Security values | ✅ PASS | No secrets in workflow files; GITHUB_TOKEN scoped; inputs validated |
| Compatibility | ⚠️ N/A | CI runs on ubuntu-latest; publishes artifacts for both linux and windows |
| Observability | ✅ PASS | Clear step names; coverage reports; failure attribution per step |
| Decision rules | ✅ PASS | Using simplest approach: single workflow, sequential jobs |
| Guiding principles | ✅ PASS | Composable steps; explicit configuration; no magic |
| Testing philosophy | ✅ PASS | Existing tests run in CI; coverage enforced; Docker for integration tests |
| TDD | ⚠️ N/A | CI/CD configuration is infrastructure, not application code |
| Code Standards | ⚠️ N/A | FestinaLente.CodeStandards already enforced via Directory.Build.props |

## Project Structure

### Documentation (this feature)

```text
specs/002-ci-build/
├── plan.md              # This file
├── research.md          # Phase 0 output (GitVersion config, Actions best practices)
├── data-model.md        # Phase 1 output (workflow structure, job dependencies)
├── quickstart.md        # Phase 1 output (how to use the pipeline)
├── contracts/           # Phase 1 output (workflow YAML schema reference)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
# Existing structure (no changes to source)
src/
├── Dottie.Cli/          # CLI application
└── Dottie.Configuration/ # Configuration library

tests/
├── Dottie.Cli.Tests/           # CLI unit tests
└── Dottie.Configuration.Tests/ # Configuration unit tests + integration tests

# New CI/CD files to create
.github/
└── workflows/
    └── build.yml        # Main CI/CD workflow

tests/
└── integration/
    ├── Dockerfile           # Ubuntu-based test image (injects dottie + scripts)
    ├── scenarios/           # Test scenarios with configs + expected outcomes
    │   ├── basic-symlinks/
    │   ├── package-install/
    │   └── full-profile/
    └── scripts/
        └── run-scenarios.sh # Validates: downloads, installs, symlinks

scripts/
└── Set-BranchProtection.ps1  # Branch protection configuration script

global.json              # .NET SDK version pinning (new)
GitVersion.yml           # GitVersion configuration (new)
```

**Structure Decision**: Infrastructure-only feature. Adds workflow configuration and supporting scripts without modifying application source code.

## Complexity Tracking

No violations. Using simplest approach:
- Single workflow file (not split into reusable workflows)
- Sequential pipeline (not parallel jobs that require coordination)
- Standard GitHub Actions marketplace actions (not custom actions)
