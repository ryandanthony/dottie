# Implementation Plan: GitHub Release Asset Type

**Branch**: `012-github-release-type` | **Date**: 2026-02-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/012-github-release-type/spec.md`

## Summary

Add an optional `type` field to `GithubReleaseItem` that controls the installation pathway for downloaded assets. The default value `binary` preserves all current behavior. A new value `deb` routes downloaded `.deb` assets through `dpkg -i` with automatic dependency resolution via `apt-get install -f`, following the existing sudo-gated pattern established by `AptRepoInstaller` and `AptPackageInstaller`. The implementation extends the existing `GithubReleaseInstaller` with a strategy-based dispatch on the `type` field, adds conditional validation (binary field required only for `type: binary`), and includes full idempotency, dry-run, and error handling support.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (SDK 10.0.100)
**Primary Dependencies**: YamlDotNet (config deserialization), Flurl.Http (HTTP client), Spectre.Console (CLI), FestinaLente.CodeStandards (analyzers)
**Storage**: N/A (filesystem operations only — temp files for downloads, `dpkg` for installation)
**Testing**: xUnit 2.9.3 + FluentAssertions 8.4.0 + Moq 4.20.70 + Flurl.Http.Testing + FakeProcessRunner
**Target Platform**: Ubuntu Linux (primary), .NET 10 cross-platform CLI tool
**Project Type**: Single solution — `src/dottie` (CLI exe) + `src/Dottie.Configuration` (library)
**Performance Goals**: N/A — installation is I/O-bound (network download, dpkg execution)
**Constraints**: Requires `dpkg` and `sudo` for `type: deb`; must not affect `type: binary` performance
**Scale/Scope**: ~5 modified files, ~2 new files, ~200-300 lines of production code, ~400-600 lines of tests

## Constitution Check *(Pre-Phase 0)*

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **Safety & Explicitness** | ✅ PASS | Dry-run supported for `type: deb`; no silent overwrites; dpkg installation is explicit user intent via `type: deb` config |
| **Idempotency** | ✅ PASS | `dpkg -s <package>` check before install; skips already-installed packages; converges to same state on re-run |
| **Security** | ✅ PASS | Uses OS package manager (`dpkg`/`apt-get`) — the preferred approach per constitution; no ad-hoc installers; no secrets logged; privilege escalation explained via `HasSudo` pattern |
| **Security values** | ✅ PASS | No secrets in repo; validates inputs (asset extension, type field enum); fails closed on unknown type; temp files cleaned up to minimize retention |
| **Compatibility** | ✅ PASS | Ubuntu-first (`dpkg`/`apt-get` are native); explicit error when `dpkg` unavailable on non-Debian systems; `type` defaults to `binary` for zero-impact backward compat |
| **Observability** | ✅ PASS | Actionable error messages for all failure modes; `InstallResult` with status/message for progress tracking; consistent with existing installer logging patterns |
| **Decision rules** | ✅ PASS | Strategy dispatch in existing installer is the simplest approach; no new projects, interfaces, or abstractions needed; extends existing patterns |
| **Guiding principles** | ✅ PASS | Small, composable change; extends `GithubReleaseInstaller` rather than creating a separate installer; no over-abstraction |
| **TDD** | ✅ PASS | All changes will follow TDD workflow; tests first for each behavior |
| **Code Standards** | ✅ PASS | FestinaLente.CodeStandards already referenced; `TreatWarningsAsErrors` enabled |

**Gate result**: ✅ All principles satisfied. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/012-github-release-type/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── yaml-schema.md   # YAML configuration contract for type field
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (files touched by this feature)

```text
src/Dottie.Configuration/
├── Models/InstallBlocks/
│   ├── GithubReleaseItem.cs          # MODIFY: Add optional Type property
│   └── GithubReleaseAssetType.cs     # NEW: Enum for asset type (Binary, Deb)
├── Installing/
│   └── GithubReleaseInstaller.cs     # MODIFY: Strategy dispatch on Type; new deb install path
└── Validation/
    └── InstallBlockValidator.cs      # MODIFY: Conditional binary validation based on Type

tests/Dottie.Configuration.Tests/
├── Installing/
│   └── GithubReleaseInstallerTests.cs  # MODIFY: Add deb install, idempotency, dry-run, error tests
├── Models/InstallBlocks/
│   └── GithubReleaseItemTests.cs       # MODIFY: Add Type property tests
├── Validation/
│   └── InstallBlockValidatorTests.cs   # MODIFY: Add conditional validation tests
└── Fixtures/
    └── valid-github-deb-type.yaml      # NEW: Test fixture with type: deb entries
```

**Structure Decision**: Follows existing single-project layout. No new projects or interfaces needed. The `GithubReleaseInstaller` already handles the full GitHub release lifecycle; the `type` field adds a branch point after download. This is consistent with how the installer already branches between archive extraction and standalone binary handling.

## Complexity Tracking

> No constitution violations detected. No complexity justifications needed.

## Constitution Check *(Post-Phase 1 Re-evaluation)*

| Principle | Status | Post-Design Notes |
|-----------|--------|-------------------|
| Safety & Explicitness | ✅ PASS | Dry-run validated in data model. `type: deb` is explicit user intent. Sudo check before any action. |
| Idempotency | ✅ PASS | `dpkg -s` check documented. Two-phase approach (download → check → install). Skip path defined. |
| Security | ✅ PASS | Uses `dpkg`/`apt-get` (OS package manager). Sudo limited to `dpkg -i` and `apt-get install -f`. Temp files cleaned. |
| Security values | ✅ PASS | All inputs validated (type enum, asset extension). Fails closed on unknown type. Temp cleanup minimizes retention. |
| Compatibility | ✅ PASS | Ubuntu-first (dpkg/apt-get native). Explicit error on non-Debian. Default `binary` = zero impact. |
| Observability | ✅ PASS | 8 distinct InstallResult patterns for all outcomes. Actionable errors for every failure mode. |
| Decision rules | ✅ PASS | Strategy dispatch in existing installer. No new interfaces or projects. |
| TDD | ✅ PASS | Data model decision matrix produces clear test scenarios for each cell. |
| Code Standards | ✅ PASS | Existing FestinaLente.CodeStandards covers all changes. No new projects needed. |

**Post-design gate result**: ✅ All principles re-confirmed. No violations.
