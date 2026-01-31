# Implementation Plan: Configuration Profiles

**Branch**: `003-profiles` | **Date**: 2026-01-30 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-profiles/spec.md`

**Note**: This template is filled in by the `/speckit.plan` workflow. In this repo, see
`.specify/scripts/powershell/setup-plan.ps1` and `.specify/scripts/powershell/create-new-feature.ps1`.

## Summary

Enhance the existing profile system with `--profile` CLI flag support, implicit default profile behavior, profile name validation, dotfile deduplication by target path, and improved profile listing with inheritance visualization. Built on top of the existing `ProfileMerger`, `ProfileResolver`, and `ConfigProfile` infrastructure from 001-yaml-configuration.

## Technical Context

**Language/Version**: .NET 10 (C# 13), Native AOT / trimmed self-contained deployment  
**Primary Dependencies**:
- `Spectre.Console` — Rich terminal output, tables, tree rendering
- `Spectre.Console.Cli` — Command-line argument parsing with global options
- `YamlDotNet` — YAML parsing (existing)

**Storage**: Filesystem only (`dottie.yaml` at repo root)  
**Testing**: xUnit + FluentAssertions, extending existing test suites  
**Target Platform**: Linux x64/arm64 (Ubuntu/Debian), single-file trimmed executable  
**Project Type**: Single CLI application (existing)  
**Performance Goals**: Profile resolution <100ms for 10-level inheritance chains  
**Constraints**: No new dependencies; extend existing infrastructure only  
**Scale/Scope**: Configs with up to 20 profiles, inheritance depth up to 10 levels

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **Safety & Explicitness** | ✅ Pass | Profile selection is explicit via `--profile` flag; default behavior documented |
| **Idempotency** | ✅ Pass | Same profile always resolves to same merged configuration |
| **Security** | ✅ Pass | No new external interactions; profile names validated |
| **Ubuntu-First** | ✅ Pass | No platform-specific changes |
| **Observability** | ✅ Pass | Clear error messages for invalid profiles; inheritance chain visible in output |
| **Simplicity** | ✅ Pass | Extends existing infrastructure; minimal new code |
| **Testing** | ✅ Pass | All changes testable via existing patterns |

**Constitution violations**: None identified.

## Project Structure

### Documentation (this feature)

```text
specs/003-profiles/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── examples/
│       ├── profile-inheritance.yaml
│       ├── profile-deduplication.yaml
│       ├── profile-deep-chain.yaml
│       └── profile-default.yaml
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Dottie.Cli/
│   ├── Commands/
│   │   ├── ProfileAwareSettings.cs      # NEW: Base class with --profile option
│   │   ├── ValidateCommand.cs           # MODIFY: Use --profile flag
│   │   └── ValidateCommandSettings.cs   # MODIFY: Inherit ProfileAwareSettings
│   └── Dottie.Cli.csproj
│
├── Dottie.Configuration/
│   ├── Inheritance/
│   │   └── ProfileMerger.cs             # MODIFY: Dotfile deduplication
│   ├── ProfileInfo.cs                   # NEW: Profile summary for listing
│   ├── ProfileResolver.cs               # MODIFY: Implicit default support
│   ├── Validation/
│   │   └── ConfigurationValidator.cs    # MODIFY: Profile name validation
│   └── Dottie.Configuration.csproj

tests/
├── Dottie.Configuration.Tests/
│   ├── Fixtures/
│   │   ├── profile-invalid-name.yaml    # NEW: Test fixture
│   │   └── profile-dedup.yaml           # NEW: Test fixture
│   ├── Inheritance/
│   │   └── ProfileMergerTests.cs        # MODIFY: Add deduplication tests
│   ├── ProfileResolverTests.cs          # MODIFY: Add implicit default tests
│   └── Validation/
│       └── ProfileNameValidatorTests.cs # NEW: Profile name validation tests
└── Dottie.Cli.Tests/
    └── Commands/
        └── ValidateCommandTests.cs      # MODIFY: Test --profile flag
```

**Structure Decision**: Extend existing single project structure. No new projects needed - all changes are modifications or additions to existing assemblies.

## Complexity Tracking

> No constitution violations — this section is empty.
