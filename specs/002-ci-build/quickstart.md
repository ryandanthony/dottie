# Quickstart: CI/CD Build Pipeline

**Feature**: 002-ci-build  
**Date**: January 28, 2026  
**Purpose**: Quick reference for using the CI/CD pipeline

## Overview

The CI/CD pipeline automatically builds, tests, and releases the Dottie CLI tool when code is pushed or pull requests are opened.

## Triggers

| Event | What Happens |
|-------|--------------|
| Open/update PR against `main` | Build → Test → Coverage → Artifacts |
| Merge to `main` | Build → Test → Coverage → Artifacts → **Tag + Release** |

## Version Control

Versions are calculated automatically from git history. To control version bumps, include keywords in commit messages:

| Keyword | Effect | Example |
|---------|--------|---------|
| (none) | Patch bump | 0.1.0 → 0.1.1 |
| `+semver: minor` | Minor bump | 0.1.0 → 0.2.0 |
| `+semver: feature` | Minor bump | 0.1.0 → 0.2.0 |
| `+semver: major` | Major bump | 0.1.0 → 1.0.0 |
| `+semver: breaking` | Major bump | 0.1.0 → 1.0.0 |

### Example Commit Messages

```bash
# Patch bump (default)
git commit -m "Fix null reference in config parser"

# Minor bump
git commit -m "Add profile inheritance support +semver: minor"

# Major bump
git commit -m "Change config format to YAML +semver: breaking"
```

## Downloading Artifacts

### From Pull Request

1. Go to the PR page on GitHub
2. Click "Checks" tab
3. Click on the "build" workflow run
4. Scroll to "Artifacts" section
5. Download `binaries` (contains both platforms)

### From Release

1. Go to repository "Releases" page
2. Find the version you want
3. Download:
   - `dottie-linux-x64` for Linux
   - `dottie-win-x64.exe` for Windows

## Code Coverage

- **Minimum threshold**: 80% line coverage
- Coverage report posted as PR comment
- Full HTML report available in workflow artifacts

### Viewing Coverage

1. Check PR comment for summary
2. Download `coverage-report` artifact for detailed HTML report
3. Open `index.html` in browser

## Build Status

Build status is shown on:
- Pull request status checks
- Commit status badges
- Actions tab in repository

### Build Fails If

- Compilation errors
- Any test fails
- Code coverage below 80%
- Release creation fails (main branch only)

## Branch Protection

Branch protection must be configured separately (one-time setup):

```powershell
# From repository root
./scripts/Set-BranchProtection.ps1
```

This ensures:
- CI must pass before merging
- Branch must be up-to-date with main

## Local Development

### Running the Same Build Locally

```bash
# Restore
dotnet restore

# Build
dotnet build

# Test with coverage
dotnet test --collect:"XPlat Code Coverage"

# Publish
dotnet publish src/Dottie.Cli -c Release -r linux-x64 --self-contained
dotnet publish src/Dottie.Cli -c Release -r win-x64 --self-contained
```

### Running Integration Tests Locally

```bash
# Build the CLI for Linux first
dotnet publish src/Dottie.Cli -c Release -r linux-x64 --self-contained -o ./publish/linux-x64

# Build and run the integration test container (from repo root)
docker build -t dottie-integration-test -f tests/integration/Dockerfile .
docker run --rm dottie-integration-test
```

## Troubleshooting

### Build Fails: "GitVersion failed"

**Cause**: Shallow clone without full git history  
**Fix**: Ensure `fetch-depth: 0` in checkout step (already configured)

### Build Fails: Coverage below threshold

**Cause**: New code without tests, or tests not covering edge cases  
**Fix**: Add tests to increase coverage above 80%

### Release Not Created

**Cause**: Only happens on `main` branch  
**Fix**: Merge your PR to main; release is automatic

### Coverage Comment Not Appearing

**Cause**: Only posted on pull requests, not direct pushes  
**Fix**: Create a PR instead of pushing directly to main

## Files Reference

| File | Purpose |
|------|---------|
| `.github/workflows/build.yml` | Main CI/CD workflow |
| `global.json` | .NET SDK version pinning |
| `GitVersion.yml` | Version calculation configuration |
| `scripts/Set-BranchProtection.ps1` | Branch protection setup |
| `tests/integration/Dockerfile` | Ubuntu container for integration tests |
| `tests/integration/scripts/run-scenarios.sh` | Scenario execution script |
| `tests/integration/scenarios/` | Integration test scenario definitions |
