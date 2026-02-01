# Implementation Plan: Installation Sources

**Branch**: `006-install-sources` | **Date**: January 31, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-install-sources/spec.md`

## Summary

Implement the `dottie install` command that installs tools from 6 different sources in priority order: GitHub Releases, APT Packages, Private APT Repositories, Shell Scripts, Fonts, and Snap Packages. The models and configuration parsing already exist; this feature adds the command and installation orchestration services.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: Spectre.Console 0.50.0, Spectre.Console.Cli 0.50.0, YamlDotNet 16.3.0  
**Storage**: Filesystem (`~/bin/` for binaries, `~/.local/share/fonts/` for fonts)  
**Testing**: xUnit, FluentAssertions (existing patterns), Docker-based integration tests  
**Target Platform**: Linux (Ubuntu-first)  
**Project Type**: Single CLI application with library  
**Performance Goals**: Complete installation from all sources in under 10 minutes (SC-006)  
**Constraints**: Requires sudo for APT/snap operations; graceful degradation when unavailable  
**Scale/Scope**: Personal dotfile repositories (typically 5-20 install items per source)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Safety & Explicitness | ✅ PASS | --dry-run previews with full validation; sudo check upfront with clear warnings |
| Idempotency | ✅ PASS | Skip already-installed items where detectable; no state file needed |
| Security | ✅ PASS | Scripts must exist in repo (no external URLs); GPG keys verified |
| Security values | ✅ PASS | No secrets in config; GitHub auth deferred to FEATURE-12 |
| Compatibility | ✅ PASS | Ubuntu-first; sudo-required sources skipped gracefully on non-root |
| Observability | ✅ PASS | Source-level progress; summary with success/warning/failure counts |
| Decision rules | ✅ PASS | Reuse existing models; minimal new code per source |
| Guiding principles | ✅ PASS | Composable installer services; one service per source type |
| Testing philosophy | ✅ PASS | TDD for all services; integration tests for end-to-end workflows |

## Project Structure

### Documentation (this feature)

```text
specs/006-install-sources/
├── plan.md              # This file
├── research.md          # Phase 0 output - technology analysis
├── data-model.md        # Phase 1 output - entity documentation
├── quickstart.md        # Phase 1 output - developer guide
├── contracts/           # Phase 1 output - interface contracts
│   ├── cli-interface.md
│   └── installer-services.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Dottie.Cli/
│   ├── Commands/
│   │   ├── InstallCommand.cs           # [CREATE] Main install command
│   │   ├── InstallCommandSettings.cs   # [CREATE] Command settings with --dry-run
│   │   ├── LinkCommand.cs              # [REFERENCE] Pattern example
│   │   └── ProfileAwareSettings.cs     # [EXISTING] Base settings class
│   ├── Output/
│   │   ├── InstallProgressRenderer.cs  # [CREATE] Progress display
│   │   └── ErrorFormatter.cs           # [EXISTING] Error output pattern
│   ├── Utilities/
│   │   └── RepoRootFinder.cs           # [EXISTING] Utility
│   ├── Program.cs                      # [MODIFY] Register install command
│   └── Dottie.Cli.csproj
└── Dottie.Configuration/
    ├── Installing/                     # [CREATE] New namespace
    │   ├── IInstallSource.cs           # [CREATE] Common interface
    │   ├── InstallContext.cs           # [CREATE] Shared context (paths, sudo, dry-run)
    │   ├── InstallResult.cs            # [CREATE] Result type
    │   ├── InstallOrchestrator.cs      # [CREATE] Orchestrates all sources
    │   ├── GithubReleaseInstaller.cs   # [CREATE] GitHub release downloads
    │   ├── AptPackageInstaller.cs      # [CREATE] apt-get operations
    │   ├── AptRepoInstaller.cs         # [CREATE] Private repo setup + packages
    │   ├── ScriptRunner.cs             # [CREATE] Shell script execution
    │   ├── FontInstaller.cs            # [CREATE] Font downloads + fc-cache
    │   ├── SnapPackageInstaller.cs     # [CREATE] snap operations
    │   └── Utilities/
    │       ├── ArchiveExtractor.cs     # [CREATE] .tar.gz, .zip, .tgz extraction
    │       ├── HttpDownloader.cs       # [CREATE] Downloads with retry logic
    │       └── SudoChecker.cs          # [CREATE] Sudo availability detection
    ├── Models/
    │   └── InstallBlocks/
    │       ├── AptRepoItem.cs          # [EXISTING] Model
    │       ├── FontItem.cs             # [EXISTING] Model
    │       ├── GithubReleaseItem.cs    # [EXISTING] Model
    │       ├── InstallBlock.cs         # [EXISTING] Model
    │       └── SnapItem.cs             # [EXISTING] Model
    └── Dottie.Configuration.csproj

tests/
├── Dottie.Cli.Tests/
│   └── Commands/
│       ├── InstallCommandTests.cs           # [CREATE] Command tests
│       └── InstallCommandSettingsTests.cs   # [CREATE] Settings validation
├── Dottie.Configuration.Tests/
│   └── Installing/                          # [CREATE] New test namespace
│       ├── InstallOrchestratorTests.cs      # [CREATE] Orchestration tests
│       ├── GithubReleaseInstallerTests.cs   # [CREATE] GitHub installer tests
│       ├── AptPackageInstallerTests.cs      # [CREATE] APT installer tests
│       ├── AptRepoInstallerTests.cs         # [CREATE] APT repo installer tests
│       ├── ScriptRunnerTests.cs             # [CREATE] Script runner tests
│       ├── FontInstallerTests.cs            # [CREATE] Font installer tests
│       ├── SnapPackageInstallerTests.cs     # [CREATE] Snap installer tests
│       └── Utilities/
│           ├── ArchiveExtractorTests.cs     # [CREATE] Archive extraction tests
│           ├── HttpDownloaderTests.cs       # [CREATE] Download retry tests
│           └── SudoCheckerTests.cs          # [CREATE] Sudo detection tests
└── integration/
    ├── Dockerfile                           # [MODIFY] Add install test dependencies
    ├── scenarios/
    │   ├── install-github-release/          # [CREATE] GitHub release scenario
    │   │   ├── dottie.yml
    │   │   ├── README.md
    │   │   └── validate.sh
    │   ├── install-apt-packages/            # [CREATE] APT packages scenario
    │   │   ├── dottie.yml
    │   │   ├── README.md
    │   │   └── validate.sh
    │   ├── install-scripts/                 # [CREATE] Script execution scenario
    │   │   ├── dottie.yml
    │   │   ├── scripts/
    │   │   │   └── test-script.sh
    │   │   ├── README.md
    │   │   └── validate.sh
    │   ├── install-fonts/                   # [CREATE] Font installation scenario
    │   │   ├── dottie.yml
    │   │   ├── README.md
    │   │   └── validate.sh
    │   ├── install-combined/                # [CREATE] All sources combined scenario
    │   │   ├── dottie.yml
    │   │   ├── scripts/
    │   │   │   └── setup.sh
    │   │   ├── README.md
    │   │   └── validate.sh
    │   ├── install-no-sudo/                 # [CREATE] Graceful degradation scenario
    │   │   ├── dottie.yml
    │   │   ├── README.md
    │   │   └── validate.sh
    │   └── install-dry-run/                 # [CREATE] Dry-run validation scenario
    │       ├── dottie.yml
    │       ├── README.md
    │       └── validate.sh
    └── scripts/
        └── run-scenarios.sh                 # [MODIFY] Add install command support
```

**Structure Decision**: Single project structure maintained. New `Installing/` namespace for all installation logic, following the existing `Linking/` pattern. Integration tests follow existing Docker-based scenario pattern in `tests/integration/`.

## Implementation Phases

### Phase 1: Core Infrastructure (P1 - GitHub Releases)

**Goal**: Implement GitHub Release installation as the foundation.

| Task | Type | Files | Tests |
|------|------|-------|-------|
| 1.1 Create IInstallSource interface | Code | `Installing/IInstallSource.cs` | N/A (interface) |
| 1.2 Create InstallContext | Code | `Installing/InstallContext.cs` | Unit tests |
| 1.3 Create InstallResult | Code | `Installing/InstallResult.cs` | N/A (record) |
| 1.4 Create HttpDownloader with retry | Code | `Installing/Utilities/HttpDownloader.cs` | Unit tests for retry logic |
| 1.5 Create ArchiveExtractor | Code | `Installing/Utilities/ArchiveExtractor.cs` | Unit tests for each format |
| 1.6 Create GithubReleaseInstaller | Code | `Installing/GithubReleaseInstaller.cs` | Unit tests |
| 1.7 Create InstallOrchestrator (partial) | Code | `Installing/InstallOrchestrator.cs` | Unit tests |
| 1.8 Create InstallCommand | Code | `Commands/InstallCommand.cs` | Unit tests |
| 1.9 Create integration test scenario | Test | `tests/integration/scenarios/install-github-release/` | Integration test |

### Phase 2: System Package Sources (P2, P3)

**Goal**: Implement APT and Private APT Repository installation.

| Task | Type | Files | Tests |
|------|------|-------|-------|
| 2.1 Create SudoChecker | Code | `Installing/Utilities/SudoChecker.cs` | Unit tests |
| 2.2 Create AptPackageInstaller | Code | `Installing/AptPackageInstaller.cs` | Unit tests |
| 2.3 Create AptRepoInstaller | Code | `Installing/AptRepoInstaller.cs` | Unit tests |
| 2.4 Update InstallOrchestrator | Code | `Installing/InstallOrchestrator.cs` | Unit tests |
| 2.5 Create integration test scenarios | Test | `tests/integration/scenarios/install-apt-*` | Integration tests |

### Phase 3: Script & Font Sources (P4, P5)

**Goal**: Implement Script execution and Font installation.

| Task | Type | Files | Tests |
|------|------|-------|-------|
| 3.1 Create ScriptRunner | Code | `Installing/ScriptRunner.cs` | Unit tests |
| 3.2 Create FontInstaller | Code | `Installing/FontInstaller.cs` | Unit tests |
| 3.3 Update InstallOrchestrator | Code | `Installing/InstallOrchestrator.cs` | Unit tests |
| 3.4 Create integration test scenarios | Test | `tests/integration/scenarios/install-scripts/`, `install-fonts/` | Integration tests |

### Phase 4: Snap & Final Integration (P6)

**Goal**: Complete Snap installation and full integration testing.

| Task | Type | Files | Tests |
|------|------|-------|-------|
| 4.1 Create SnapPackageInstaller | Code | `Installing/SnapPackageInstaller.cs` | Unit tests |
| 4.2 Final InstallOrchestrator integration | Code | `Installing/InstallOrchestrator.cs` | Unit tests |
| 4.3 Create combined integration test | Test | `tests/integration/scenarios/install-combined/` | Integration test |
| 4.4 Create no-sudo degradation test | Test | `tests/integration/scenarios/install-no-sudo/` | Integration test |
| 4.5 Update Dockerfile for full testing | Config | `tests/integration/Dockerfile` | N/A |

## Integration Test Strategy

Integration tests are critical for this feature as installation involves real system operations. Tests run in Docker containers to ensure reproducibility and isolation.

### Test Scenarios

| Scenario | Purpose | Validates |
|----------|---------|-----------|
| `install-github-release` | GitHub binary download/extract | FR-001 through FR-005 |
| `install-apt-packages` | Standard APT installation | FR-006, FR-007 |
| `install-scripts` | Script execution from repo | FR-013 through FR-015 |
| `install-fonts` | Font download and cache refresh | FR-016 through FR-018 |
| `install-combined` | All sources in priority order | FR-021, SC-004 |
| `install-no-sudo` | Graceful degradation without sudo | Clarification #3 || `install-dry-run` | --dry-run validation without execution | FR-024 through FR-027 |
### Dockerfile Updates

The integration test Dockerfile will need:
- `fc-cache` for font cache validation
- Network access for GitHub release downloads (can use mock/local server)
- Test user with and without sudo for permission testing

## Requirement Mapping

| Requirement | Implementation | Test Coverage |
|-------------|----------------|---------------|
| FR-001: Download GitHub releases | `GithubReleaseInstaller` | Unit + Integration |
| FR-002: Extract archives | `ArchiveExtractor` | Unit tests per format |
| FR-003: Copy to ~/bin/ | `GithubReleaseInstaller` | Unit + Integration |
| FR-004: Version pinning | `GithubReleaseInstaller` | Unit tests |
| FR-005: Create ~/bin/ | `GithubReleaseInstaller` | Unit + Integration |
| FR-006: apt-get install | `AptPackageInstaller` | Unit + Integration |
| FR-007: apt-get update once | `InstallOrchestrator` | Unit tests |
| FR-008: Download GPG keys | `AptRepoInstaller` | Unit tests |
| FR-009: Add GPG keys securely | `AptRepoInstaller` | Unit tests |
| FR-010: Add repo sources | `AptRepoInstaller` | Unit tests |
| FR-011: apt-get update after repo | `AptRepoInstaller` | Unit tests |
| FR-012: Install from repo | `AptRepoInstaller` | Unit + Integration |
| FR-013: Execute scripts with bash | `ScriptRunner` | Unit + Integration |
| FR-014: Scripts in repo only | `ScriptRunner` | Unit tests |
| FR-015: Report script failures | `ScriptRunner` | Unit tests |
| FR-016: Download font archives | `FontInstaller` | Unit tests |
| FR-017: Extract to fonts dir | `FontInstaller` | Unit + Integration |
| FR-018: fc-cache refresh | `FontInstaller` | Integration test |
| FR-019: snap install | `SnapPackageInstaller` | Unit tests |
| FR-020: snap --classic | `SnapPackageInstaller` | Unit tests |
| FR-021: Priority order | `InstallOrchestrator` | Unit + Integration |
| FR-022: Progress display | `InstallProgressRenderer` | Manual verification |
| FR-023: Summary output | `InstallCommand` | Unit tests |
| FR-024: --dry-run flag | `InstallCommand` + `InstallOrchestrator` | Unit + Integration |
| FR-025: --dry-run GitHub validation | `GithubReleaseInstaller` | Unit tests |
| FR-026: --dry-run script validation | `ScriptRunner` | Unit tests |
| FR-027: --dry-run URL validation | `HttpDownloader` | Unit tests |

## Complexity Tracking

No constitution violations requiring justification. All changes follow existing patterns.

| Change | Complexity | Justification |
|--------|------------|---------------|
| New Installing/ namespace | Medium | Follows Linking/ pattern; contains domain logic |
| 6 installer services | Medium | One per source type; composable via IInstallSource |
| HttpDownloader with retry | Low | Standard retry pattern with exponential backoff |
| Integration test scenarios | Medium | Docker-based; follows existing scenario pattern |

