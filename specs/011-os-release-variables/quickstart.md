# Quickstart: OS Release Variable Substitution

**Feature**: 011-os-release-variables
**Date**: February 7, 2026

## What This Feature Does

Adds `${VARIABLE_NAME}` substitution to dottie configuration files. Variables come from three sources:

1. **OS Release** — All keys from `/etc/os-release` (e.g., `${VERSION_CODENAME}`, `${ID}`, `${VERSION_ID}`)
2. **Architecture** — `${ARCH}` (raw, e.g., `x86_64`) and `${MS_ARCH}` (Microsoft-style, e.g., `amd64`)
3. **GitHub Release Version** — `${RELEASE_VERSION}` (only in `github` entries)

## Developer Setup

No new dependencies or tools are required. This feature adds new files to the existing `Dottie.Configuration` project.

### New Files to Create

| File | Purpose |
|------|---------|
| `src/Dottie.Configuration/Utilities/OsReleaseParser.cs` | Parses `/etc/os-release` content |
| `src/Dottie.Configuration/Utilities/VariableResolver.cs` | Regex-based `${...}` substitution engine |
| `tests/Dottie.Configuration.Tests/Utilities/OsReleaseParserTests.cs` | Tests for OS release parsing |
| `tests/Dottie.Configuration.Tests/Utilities/VariableResolverTests.cs` | Tests for variable resolution |

### Existing Files to Modify

| File | Change |
|------|--------|
| `src/Dottie.Configuration/Utilities/ArchitectureDetector.cs` | Add `RawArchitecture` property |
| `src/Dottie.Configuration/Parsing/ConfigurationLoader.cs` | Integrate variable resolution post-parse |
| `src/Dottie.Configuration/Installing/GithubReleaseInstaller.cs` | Resolve `${RELEASE_VERSION}` per-item |

## Build & Test

```bash
# Build
dotnet build Dottie.slnx --warnaserror

# Run all unit tests
dotnet test Dottie.slnx

# Run integration tests (Linux only)
./tests/run-integration-tests.ps1
```

## TDD Workflow

Follow the mandatory TDD cycle for each component:

### Step 1: `OsReleaseParser` (test-first)

1. Write `OsReleaseParserTests.cs` — test `Parse(content)` with:
   - Standard Ubuntu `/etc/os-release` content
   - Quoted values, unquoted values, single-quoted values
   - Comments and blank lines
   - Empty content
   - Missing `=` (malformed lines)
2. Implement `OsReleaseParser.Parse(string content)` to pass tests.
3. Add `TryReadFromSystem` tests and implementation.

### Step 2: `ArchitectureDetector` extension (test-first)

1. Add tests for `RawArchitecture` in existing `ArchitectureDetectorTests.cs`.
2. Add the `RawArchitecture` property.

### Step 3: `VariableResolver` (test-first)

1. Write `VariableResolverTests.cs` — test `ResolveString` with:
   - String with no variables → unchanged
   - String with single variable → substituted
   - String with multiple variables → all substituted
   - String with unknown variable → error reported
   - String with deferred variable → left as-is, no error
   - Mixed: some resolved, some deferred, some unresolved
2. Implement `VariableResolver.ResolveString`.
3. Write tests for `ResolveConfiguration` → walks full config.
4. Implement `ResolveConfiguration`.

### Step 4: Integration into `ConfigurationLoader` (test-first)

1. Add YAML fixtures with variables (`tests/.../Fixtures/variable-*.yaml`).
2. Write loader tests that verify variables in YAML are resolved after loading.
3. Modify `ConfigurationLoader.LoadFromString` to call `VariableResolver`.

### Step 5: `GithubReleaseInstaller` `${RELEASE_VERSION}` (test-first)

1. Write tests for asset pattern resolution with `${RELEASE_VERSION}`.
2. Modify `GithubReleaseInstaller` to resolve `${RELEASE_VERSION}` after version determination.

## Key Design Decisions

- **Global variables resolved at load time**: All OS release and architecture variables are substituted during `ConfigurationLoader.LoadFromString`, before any installer runs.
- **`${RELEASE_VERSION}` deferred**: Resolved per-github-item at install time since the version is item-specific (may require GitHub API).
- **No escape mechanism**: `${...}` is always treated as a variable reference. If users need literal `${...}`, this will be addressed in a future iteration.
- **Fail on unresolvable**: Unresolvable non-deferred variables produce errors, not silent pass-through (per FR-011).
- **Immutable models**: All model records use `init` properties. Resolution creates new instances via `with` expressions.
