# Implementation Plan: YAML Configuration System

**Branch**: `001-yaml-configuration` | **Date**: 2026-01-28 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-yaml-configuration/spec.md`

## Summary

Implement a YAML-based configuration system that parses `dottie.yaml` files, supports multiple named profiles with inheritance, validates dotfile mappings and install block definitions, and provides clear error messages. Built as a trimmed .NET 10 standalone CLI using Spectre.Console for output formatting.

## Technical Context

**Language/Version**: .NET 10 (C# 13), Native AOT / trimmed self-contained deployment  
**Primary Dependencies**:
- `Spectre.Console` — Rich terminal output, tables, progress indicators
- `Spectre.Console.Cli` — Command-line argument parsing
- `YamlDotNet` — YAML parsing (trimming-compatible with source generators)
- `LibGit2Sharp` — Git repository detection (if needed for repo root discovery)

**Storage**: Filesystem only (`dottie.yaml` at repo root)  
**Testing**: xUnit + FluentAssertions, contract tests for config parsing, integration tests for CLI  
**Target Platform**: Linux x64/arm64 (Ubuntu/Debian), single-file trimmed executable  
**Project Type**: Single CLI application  
**Performance Goals**: Parse and validate 50-entry config in <2 seconds (SC-004)  
**Constraints**: <50MB binary size (trimmed AOT), no runtime JIT dependencies  
**Scale/Scope**: Single-user CLI tool, configs typically <500 lines

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **Safety & Explicitness** | ✅ Pass | Config parsing is read-only; template generation is explicit action |
| **Idempotency** | ✅ Pass | Parsing same file always produces same result |
| **Security** | ✅ Pass | FR-019 validates script paths stay within repo; no network calls during parsing |
| **Ubuntu-First** | ✅ Pass | Install block types (apt, snap) are Ubuntu-native |
| **Observability** | ✅ Pass | FR-020 requires actionable errors with line numbers |
| **Simplicity** | ✅ Pass | Single responsibility: parse and validate config |
| **Testing** | ✅ Pass | Config parsing is pure/deterministic, highly testable |

**Constitution violations**: None identified.

## Project Structure

### Documentation (this feature)

```text
specs/001-yaml-configuration/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (JSON schemas, examples)
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Dottie.Cli/                    # CLI entry point (Spectre.Console.Cli)
│   ├── Program.cs
│   ├── Commands/
│   │   └── ValidateCommand.cs     # dottie validate <profile>
│   └── Dottie.Cli.csproj
│
├── Dottie.Configuration/          # Core config parsing library
│   ├── Models/                    # Record types for config structure
│   │   ├── DottieConfiguration.cs
│   │   ├── Profile.cs
│   │   ├── DotfileEntry.cs
│   │   └── InstallBlocks/
│   │       ├── InstallBlock.cs
│   │       ├── GithubReleaseItem.cs
│   │       ├── AptRepoItem.cs
│   │       ├── SnapItem.cs
│   │       └── FontItem.cs
│   ├── Parsing/
│   │   ├── ConfigurationLoader.cs
│   │   └── YamlDeserializer.cs
│   ├── Validation/
│   │   ├── ConfigurationValidator.cs
│   │   ├── ProfileValidator.cs
│   │   └── ValidationResult.cs
│   ├── Inheritance/
│   │   └── ProfileMerger.cs
│   ├── Templates/
│   │   └── StarterTemplate.cs
│   └── Dottie.Configuration.csproj

tests/
├── Dottie.Configuration.Tests/
│   ├── Parsing/
│   │   └── ConfigurationLoaderTests.cs
│   ├── Validation/
│   │   └── ConfigurationValidatorTests.cs
│   ├── Inheritance/
│   │   └── ProfileMergerTests.cs
│   └── Fixtures/                  # Sample YAML files for testing
│       ├── valid-minimal.yaml
│       ├── valid-full.yaml
│       ├── invalid-missing-source.yaml
│       └── ...
└── Dottie.Cli.Tests/
    └── Commands/
        └── ValidateCommandTests.cs
```

**Structure Decision**: Single project structure with two assemblies:
1. `Dottie.Configuration` — Pure library for parsing/validation (testable, reusable)
2. `Dottie.Cli` — Thin CLI wrapper using Spectre.Console.Cli

This separation enables unit testing the configuration logic without CLI dependencies and supports future consumption by other tools.

## Complexity Tracking

> No constitution violations — this section is empty.
