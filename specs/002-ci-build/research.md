# Research: CI/CD Build Pipeline

**Feature**: 002-ci-build  
**Date**: January 28, 2026  
**Purpose**: Resolve technical unknowns and document best practices before implementation

## Research Tasks

### 1. GitVersion Configuration for .NET 10

**Decision**: Use GitVersion with Mainline mode starting at 0.1.0

**Rationale**:
- Mainline mode is simpler than GitFlow for projects without release branches
- Commit message conventions (`+semver: major/minor/feature/breaking`) provide explicit control
- Starting version 0.1.0 indicates pre-1.0 development phase

**Alternatives Considered**:
- Manual versioning: Rejected - error-prone, requires human intervention
- Nerdbank.GitVersioning: Rejected - more complex configuration for similar results
- MinVer: Rejected - less control over version bumping conventions

**Configuration**:
```yaml
# GitVersion.yml
mode: Mainline
next-version: 0.1.0
tag-prefix: v
major-version-bump-message: '\+semver:\s?(major|breaking)'
minor-version-bump-message: '\+semver:\s?(minor|feature)'
```

---

### 2. GitHub Actions Workflow Best Practices

**Decision**: Single workflow file with sequential steps in one job

**Rationale**:
- Simpler debugging (all steps share same runner context)
- No artifact transfer overhead between jobs
- Full git history available throughout (needed for GitVersion)
- Easier to maintain than split workflows

**Alternatives Considered**:
- Reusable workflows: Rejected - overkill for single repository
- Parallel jobs: Rejected - requires artifact transfer, complicates version passing
- Matrix builds: Rejected - not needed (we build sequentially for both platforms)

**Best Practices Applied**:
- Use `actions/checkout@v4` with `fetch-depth: 0` for full history
- Use `actions/setup-dotnet@v4` for .NET SDK
- Use `actions/cache@v4` for NuGet packages
- Use `actions/upload-artifact@v4` with retention-days: 7
- Use `softprops/action-gh-release@v2` for GitHub Releases

---

### 3. Code Coverage in GitHub Actions

**Decision**: Use coverlet with ReportGenerator, threshold enforcement via dotnet test

**Rationale**:
- coverlet is already in test projects (coverlet.collector package)
- ReportGenerator provides human-readable HTML and summary formats
- dotnet test supports threshold enforcement natively with coverlet
- PR comments via dedicated action (danielpalme/ReportGenerator-GitHub-Action)

**Alternatives Considered**:
- Codecov: Rejected - external service dependency, privacy concerns
- Coveralls: Rejected - external service dependency
- Custom scripts: Rejected - more maintenance than existing tools

**Configuration**:
```shell
# Test with coverage collection
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate report
reportgenerator -reports:./TestResults/**/coverage.cobertura.xml -targetdir:./coverage-report -reporttypes:Html;MarkdownSummary

# Threshold enforcement (in test step)
dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line
```

---

### 4. Docker for Integration Tests

**Decision**: Use Docker Compose for integration test environment

**Rationale**:
- Provides consistent environment across Windows/Linux development machines
- GitHub Actions ubuntu-latest has Docker pre-installed
- Allows testing against realistic file system scenarios
- Dockerfile can be reused for local development testing

**Alternatives Considered**:
- Direct test execution: Rejected - inconsistent across platforms
- WSL for Windows: Rejected - not available in CI environment
- Test containers library: Considered - may be overkill for file system tests

**Implementation Approach**:
1. Create Dockerfile for test environment (ubuntu base)
2. Mount test fixtures into container
3. Run `dotnet test` inside container for integration tests
4. Collect coverage from container execution

---

### 5. Branch Protection via gh CLI

**Decision**: PowerShell script using `gh api` for branch protection configuration

**Rationale**:
- `gh` CLI is cross-platform and commonly available
- REST API provides full control over protection settings
- PowerShell meets project scripting standard
- Script is idempotent (PUT operations replace existing settings)

**Alternatives Considered**:
- Manual configuration: Rejected - not reproducible, error-prone
- Terraform: Rejected - too heavy for single repository
- GitHub Actions workflow: Rejected - chicken-and-egg problem (protection blocks workflow changes)

**API Endpoint**:
```
PUT /repos/{owner}/{repo}/branches/{branch}/protection
```

**Required Scopes**: `repo` (for private repos) or `public_repo` (for public repos)

---

### 6. Release Asset Naming Convention

**Decision**: `dottie-{platform}[.exe]` format

**Rationale**:
- Clear platform identification
- No version in filename (version is in release tag)
- `.exe` extension only for Windows (conventional)
- Matches existing publish output structure

**Asset Names**:
- `dottie-linux-x64` (no extension)
- `dottie-win-x64.exe`

---

### 7. global.json SDK Pinning

**Decision**: Pin to .NET 10.x with rollForward policy

**Rationale**:
- Ensures CI uses same SDK version as developers
- rollForward: latestFeature allows patch updates without file changes
- Prevents breaking changes from SDK updates

**Configuration**:
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

---

## Resolved Unknowns

| Unknown | Resolution |
|---------|------------|
| Version calculation method | GitVersion Mainline mode with commit message conventions |
| Coverage tool integration | coverlet (existing) + ReportGenerator for reports |
| Integration test environment | Docker container for cross-platform consistency |
| Branch protection mechanism | PowerShell script using gh CLI |
| SDK version management | global.json with rollForward policy |
| Release naming | Version-only (v1.2.3), assets named by platform |
| Artifact retention | 7 days for workflow artifacts; releases are permanent |

## Dependencies to Install/Configure

| Dependency | Purpose | Installation |
|------------|---------|--------------|
| GitVersion | Semantic versioning | GitHub Action: gittools/actions/gitversion/setup@v3.2 |
| ReportGenerator | Coverage reports | dotnet tool: dotnet tool install -g dotnet-reportgenerator-globaltool |
| gh CLI | Branch protection script | Pre-installed on GitHub Actions runners |
| Docker | Integration tests | Pre-installed on ubuntu-latest |

## References

- [GitVersion Documentation](https://gitversion.net/docs/)
- [GitHub Actions Workflow Syntax](https://docs.github.com/en/actions/reference/workflow-syntax-for-github-actions)
- [Branch Protection API](https://docs.github.com/en/rest/branches/branch-protection)
- [coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator](https://github.com/danielpalme/ReportGenerator)
