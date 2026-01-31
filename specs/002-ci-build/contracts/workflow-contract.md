# Contract: GitHub Actions Workflow

**Feature**: 002-ci-build  
**Date**: January 28, 2026  
**Purpose**: Define the expected structure of the build workflow

## Workflow File

**Path**: `.github/workflows/build.yml`

## Triggers

```yaml
on:
  pull_request:
    branches: [main]
  push:
    branches: [main]
```

## Job: build

**Runs-on**: `ubuntu-latest`

### Required Steps (in order)

| # | Step Name | Action/Command | Required Outputs |
|---|-----------|----------------|------------------|
| 1 | Checkout | actions/checkout@v4 | Repository with full history |
| 2 | Setup .NET | actions/setup-dotnet@v4 | .NET SDK available |
| 3 | Cache NuGet | actions/cache@v4 | Cached packages restored |
| 4 | GitVersion | gittools/actions/gitversion/execute@v3.2 | Version environment variables |
| 5 | Restore | dotnet restore | Dependencies restored |
| 6 | Build | dotnet build | Compiled binaries |
| 7 | Unit Tests | dotnet test (unit projects) | Test results, coverage XML |
| 8 | Integration Tests | Docker + dotnet test | Test results, coverage XML |
| 9 | Coverage Report | reportgenerator | HTML report, markdown summary |
| 10 | Coverage Check | threshold validation | Pass/fail status |
| 11 | Publish linux-x64 | dotnet publish -r linux-x64 | Self-contained binary |
| 12 | Publish win-x64 | dotnet publish -r win-x64 | Self-contained binary |
| 13 | Upload Artifacts | actions/upload-artifact@v4 | Downloadable artifacts |
| 14 | Coverage Comment | marocchino/sticky-pull-request-comment@v2 | PR comment (PR only) |
| 15 | Create Release | softprops/action-gh-release@v2 | GitHub Release (main only) |

## Permissions

```yaml
permissions:
  contents: write      # For creating tags and releases
  pull-requests: write # For posting coverage comments
```

## Conditionals

| Step | Condition |
|------|-----------|
| Coverage Comment | `github.event_name == 'pull_request'` |
| Create Release | `github.ref == 'refs/heads/main'` |

## Outputs

### Version (from GitVersion step)

```yaml
outputs:
  semVer: ${{ steps.gitversion.outputs.semVer }}
  fullSemVer: ${{ steps.gitversion.outputs.fullSemVer }}
```

## Artifacts

| Name | Contents | Retention |
|------|----------|-----------|
| binaries | publish/linux-x64/dottie, publish/win-x64/dottie.exe | 7 days |
| coverage-report | coverage-report/** | 7 days |

## Release Assets

| Asset Name | Source Path |
|------------|-------------|
| dottie-linux-x64 | publish/linux-x64/dottie |
| dottie-win-x64.exe | publish/win-x64/dottie.exe |
