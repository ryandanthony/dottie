# Implementation Plan: CLI Command `apply`

**Branch**: `009-cli-apply` | **Date**: February 3, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-cli-apply/spec.md`

## Summary

The `apply` command unifies `link` and `install` into a single command that:
1. Resolves profile inheritance
2. Creates dotfile symlinks (delegating to `LinkingOrchestrator`)
3. Installs software in priority order (delegating to existing installers)
4. Reports a verbose summary of all operations with status

Flags: `--profile <name>`, `--force`, `--dry-run`

## Technical Context

**Language/Version**: C# 13.0 / .NET 10.0  
**Primary Dependencies**: Spectre.Console, Spectre.Console.Cli  
**Storage**: N/A (filesystem operations only)  
**Testing**: xUnit, NSubstitute  
**Target Platform**: Linux (Ubuntu-first)
**Project Type**: CLI application (single project)  
**Performance Goals**: N/A (one-time setup operations)  
**Constraints**: Must be idempotent; fail-soft on installation errors  
**Scale/Scope**: Single-user local machine setup

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Pre-Design Check**: ✅ PASSED  
**Post-Design Check**: ✅ PASSED (re-evaluated February 3, 2026)

- ✅ Safety & Explicitness: default to dry-run/preview; no silent overwrites/deletes — `--dry-run` previews all operations; `--force` required to overwrite conflicts with backup
- ✅ Idempotency: re-running converges; non-interactive mode where possible — existing symlinks skipped; already-installed software skipped
- ✅ Security: prefer package managers; avoid unsafe installers; do not leak secrets — delegates to existing secure installers
- ✅ Security values: no secrets in repo; fail closed; validate inputs; minimize retention — validates configuration before operations
- ✅ Compatibility: Ubuntu-first; handle distro-specific behavior explicitly — leverages existing distro-aware installers
- ✅ Observability: actionable errors; verbose logs suitable for bug reports — verbose summary of every operation with status
- ✅ Decision rules: simplest acceptable approach; clarity-first; measure before optimizing — orchestrates existing commands, minimal new code
- ✅ Guiding principles: simplicity; maintainability; explicitness; composable units; avoid over-abstraction — composes existing `LinkingOrchestrator` and installers
- ✅ Testing philosophy: behavior-focused tests; integration tests for workflows; deterministic + isolated — TDD per constitution
- ✅ TDD: Test-first development per constitution requirement
- ✅ Code Standards: FestinaLente.CodeStandards already referenced in Directory.Build.props

## Project Structure

### Documentation (this feature)

```text
specs/009-cli-apply/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
src/
├── Dottie.Cli/
│   ├── Commands/
│   │   ├── ApplyCommand.cs           # NEW: Main apply command
│   │   ├── ApplyCommandSettings.cs   # NEW: Command settings
│   │   ├── LinkCommand.cs            # EXISTING
│   │   ├── InstallCommand.cs         # EXISTING
│   │   └── ProfileAwareSettings.cs   # EXISTING (base class)
│   └── Output/
│       ├── ApplyProgressRenderer.cs  # NEW: Verbose summary renderer
│       └── InstallProgressRenderer.cs # EXISTING
└── Dottie.Configuration/
    ├── Linking/
    │   └── LinkingOrchestrator.cs    # EXISTING (reuse)
    └── Installing/
        └── InstallOrchestrator.cs    # EXISTING (reuse)

tests/
├── Dottie.Cli.Tests/
│   └── Commands/
│       ├── ApplyCommandTests.cs      # NEW
│       └── ApplyCommandSettingsTests.cs # NEW
└── integration/
    └── apply/                        # NEW: Integration test scenarios
```

**Structure Decision**: Follows existing single-project CLI structure. The `ApplyCommand` orchestrates existing `LinkingOrchestrator` and `InstallOrchestrator` components.

## Complexity Tracking

No constitution violations. The implementation composes existing components.

| Component | Justification |
|-----------|---------------|
| ApplyCommand | New command that orchestrates existing link + install functionality |
| ApplyProgressRenderer | New renderer for verbose summary per FR-010 requirement |
