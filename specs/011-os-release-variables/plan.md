# Implementation Plan: OS Release Variable Substitution

**Branch**: `011-os-release-variables` | **Date**: February 7, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/011-os-release-variables/spec.md`

**Note**: Builds on existing configuration and installation infrastructure from specs 001, 006, and 007.

## Summary

Add dynamic variable substitution (`${KEY_NAME}`) to configuration fields across three install sources: `aptrepo`, `dotfiles`, and `github`. Variables are sourced from `/etc/os-release` (e.g., `${VERSION_CODENAME}`), system architecture detection (`${ARCH}`, `${MS_ARCH}`), and GitHub release resolution (`${RELEASE_VERSION}`). This is implemented as a new `VariableResolver` service in the Configuration library that resolves variables during configuration loading, before any installation actions execute. The existing `ArchitectureDetector` is extended to provide raw architecture and Microsoft-style architecture mappings. A new `OsReleaseParser` reads `/etc/os-release`. All substitution is opt-in via `${...}` patterns; configurations without variables are unchanged (full backwards compatibility).

## Technical Context

**Language/Version**: C# 13.0 / .NET 10.0
**Primary Dependencies**: Spectre.Console 0.50.0, Spectre.Console.Cli 0.50.0, YamlDotNet 16.3.0, Flurl.Http 4.0.2
**Storage**: Filesystem (`/etc/os-release` read-only; no writes)
**Testing**: xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.70
**Target Platform**: Linux (Ubuntu LTS primary), cross-platform for development
**Project Type**: Single project with CLI + Configuration libraries
**Performance Goals**: Variable resolution adds < 1 second to configuration processing
**Constraints**: Must not break existing configurations; `/etc/os-release` may not exist; architecture mapping is finite
**Scale/Scope**: Single-user dotfiles tool, typical configs have 10–30 items with 0–5 variable references each

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Safety & Explicitness | ✅ PASS | Variable resolution is read-only; no filesystem writes. Unresolvable variables produce clear errors before any install action executes. Existing dry-run behavior preserved. |
| Idempotency | ✅ PASS | Variable resolution is deterministic — same OS, same architecture, same config → same result. No state mutations. |
| Security | ✅ PASS | Reads only `/etc/os-release` (a standard system file); no secrets involved. No new network calls except existing GitHub API for `${RELEASE_VERSION}`. All inputs validated. |
| Security values | ✅ PASS | No secrets stored or logged. Variable values are system metadata (OS name, architecture). Fail closed on unresolvable variables. |
| Compatibility | ✅ PASS | Ubuntu-first — `/etc/os-release` is standard on Ubuntu LTS. Missing file produces warning + graceful degradation. Architecture detection uses `RuntimeInformation` (cross-platform). |
| Observability | ✅ PASS | Clear error messages identify variable name, entry name, and field. Verbose logging shows resolved values. |
| Decision rules | ✅ PASS | Simple regex-based substitution. No template engine, no recursive expansion, no escape syntax — simplest approach that meets requirements. |
| Guiding principles | ✅ PASS | Small, composable units: `OsReleaseParser`, `ArchitectureDetector` (extended), `VariableResolver`. Each independently testable. No over-abstraction. |
| Testing philosophy | ✅ PASS | Each component behavior-testable with deterministic inputs. `OsReleaseParser` tested with file content strings. `VariableResolver` tested with dictionary inputs. No mocking needed for core logic. |

**Gate Status**: ✅ PASSED — Ready for Phase 0 research

## Project Structure

### Documentation (this feature)

```text
specs/011-os-release-variables/
├── plan.md              # This file
├── research.md          # Phase 0: decisions and rationale
├── data-model.md        # Phase 1: entity definitions
├── quickstart.md        # Phase 1: developer guide
├── contracts/           # Phase 1: interface contracts
│   └── IVariableResolver.md
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Dottie.Configuration/
│   ├── Models/
│   │   └── InstallBlocks/
│   │       ├── AptRepoItem.cs               [EXISTING]
│   │       ├── GithubReleaseItem.cs          [EXISTING]
│   │       └── InstallBlock.cs               [EXISTING]
│   ├── Parsing/
│   │   └── ConfigurationLoader.cs            [EXISTING - enhance to call VariableResolver]
│   ├── Utilities/
│   │   ├── ArchitectureDetector.cs           [EXISTING - extend with RawArchitecture, MsArchitecture]
│   │   ├── OsReleaseParser.cs                [CREATE - parse /etc/os-release]
│   │   ├── VariableResolver.cs               [CREATE - ${...} substitution engine]
│   │   └── PathExpander.cs                   [EXISTING]
│   ├── Installing/
│   │   └── GithubReleaseInstaller.cs         [EXISTING - extract version for ${RELEASE_VERSION}]
│   └── Validation/
│       └── ConfigurationValidator.cs         [EXISTING - validate variable references]
├── Dottie.Cli/
│   └── Commands/
│       └── InstallCommand.cs                 [EXISTING - no changes expected]

tests/
├── Dottie.Configuration.Tests/
│   ├── Utilities/
│   │   ├── OsReleaseParserTests.cs           [CREATE]
│   │   ├── VariableResolverTests.cs          [CREATE]
│   │   └── ArchitectureDetectorTests.cs      [EXISTING - extend]
│   ├── Parsing/
│   │   └── ConfigurationLoaderTests.cs       [EXISTING - extend with variable scenarios]
│   └── Fixtures/
│       └── variable-*.yaml                   [CREATE - test YAML fixtures with variables]
├── Dottie.Cli.Tests/
│   └── (no changes expected)
└── integration/
    └── (extend existing integration tests with variable scenarios)
```

**Structure Decision**: Follows existing single-project CLI + Configuration structure. New files are placed in existing `Utilities/` folder alongside the related `ArchitectureDetector.cs`. No new projects or structural changes needed.

## Complexity Tracking

No constitution violations. All new code uses existing infrastructure and follows established patterns.

## Constitution Re-Check (Post-Design)

*Re-evaluated after Phase 1 design (data-model.md, contracts/, quickstart.md).*

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| Safety & Explicitness | ✅ PASS | All resolution is read-only. Errors surface before installation. No new write operations. |
| Idempotency | ✅ PASS | Same inputs → same resolved configuration. No state mutations. |
| Security | ✅ PASS | Only reads `/etc/os-release` (standard system file). No new network calls. Inputs validated via regex. |
| Security values | ✅ PASS | No secrets involved. OS metadata only. |
| Compatibility | ✅ PASS | `RuntimeInformation` is cross-platform. `OsReleaseParser.TryReadFromSystem` handles missing file gracefully. |
| Observability | ✅ PASS | Error messages include profile name, entry identifier, field name, and variable name. |
| Decision rules | ✅ PASS | Simplest approach: regex replace. No template engine, no recursive expansion. |
| Guiding principles | ✅ PASS | Three small composable static utilities. Matches existing `ArchitectureDetector`/`PathExpander` patterns. |
| Testing philosophy | ✅ PASS | All components accept string/dictionary inputs — fully deterministic, no mocking needed for core logic. |

**Post-Design Gate Status**: ✅ PASSED — Ready for Phase 2 task generation
