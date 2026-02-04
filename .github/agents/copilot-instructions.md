# dottie Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-01-28

## Active Technologies
- .NET 10 (C# 13) + GitVersion (semantic versioning), ReportGenerator (coverage), GitHub Actions, Docker (002-ci-build)
- N/A (CI/CD infrastructure) (002-ci-build)
- [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION] + [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION] (003-profiles)
- [if applicable, e.g., PostgreSQL, CoreData, files or N/A] (003-profiles)
- Filesystem only (dotfiles and backups) (004-conflict-handling)
- C# / .NET 10.0 + Spectre.Console 0.50.0, Spectre.Console.Cli 0.50.0, YamlDotNet 16.3.0 (005-cli-link)
- Filesystem (symlinks, backups) (005-cli-link)
- Filesystem (`~/bin/` for binaries, `~/.local/share/fonts/` for fonts) (006-install-sources)
- C# 12 / .NET 9.0 + Spectre.Console (CLI/output), Flurl.Http (HTTP), YamlDotNet (config) (007-cli-install)
- N/A (filesystem operations only) (007-cli-install)
- C# 13.0 / .NET 10.0 + Spectre.Console, Spectre.Console.Cli (009-cli-apply)
- C# 12 / .NET 9.0 + Spectre.Console (CLI/output), YamlDotNet (config parsing) (010-cli-status)
- N/A (read-only filesystem inspection) (010-cli-status)

- .NET 10 (C# 13), Native AOT / trimmed self-contained deployment (001-yaml-configuration)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for .NET 10 (C# 13), Native AOT / trimmed self-contained deployment

## Code Style

.NET 10 (C# 13), Native AOT / trimmed self-contained deployment: Follow standard conventions

## Recent Changes
- 010-cli-status: Added C# 12 / .NET 9.0 + Spectre.Console (CLI/output), YamlDotNet (config parsing)
- 009-cli-apply: Added C# 13.0 / .NET 10.0 + Spectre.Console, Spectre.Console.Cli
- 007-cli-install: Added C# 12 / .NET 9.0 + Spectre.Console (CLI/output), Flurl.Http (HTTP), YamlDotNet (config)
- 006-install-sources: Added C# / .NET 10.0 + Spectre.Console 0.50.0, Spectre.Console.Cli 0.50.0, YamlDotNet 16.3.0


<!-- MANUAL ADDITIONS START -->

## Testing

### Unit Tests
Run all unit tests:
```bash
dotnet test Dottie.slnx
```

### Integration Tests
Integration tests run in Docker for cross-platform consistency. The script is located at `tests/run-integration-tests.ps1`.

**Full integration test run (build + Docker image + test):**
```powershell
cd tests
./run-integration-tests.ps1
```

**Faster iterations (skip dotnet publish):**
```powershell
./run-integration-tests.ps1 -NoBuild
```

**Just run tests (skip both build and Docker image build):**
```powershell
./run-integration-tests.ps1 -NoBuild -NoImageBuild
```

**What the script does:**
1. **Step 1:** Publishes the Dottie CLI for Linux (`publish/linux-x64/dottie`)
2. **Step 2:** Builds Docker test image from `tests/integration/Dockerfile`
3. **Step 3:** Runs all integration test scenarios inside the container

Integration test scenarios are located in `tests/integration/scenarios/`. Each scenario has:
- `dottie.yml` - Configuration file for testing
- `validate.sh` - Validation script that runs inside Docker
- `README.md` - Documentation of the test scenario

Example: `tests/integration/scenarios/install-github-release/` tests GitHub release binary installation.

<!-- MANUAL ADDITIONS END -->
