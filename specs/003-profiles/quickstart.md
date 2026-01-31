# Quickstart: Configuration Profiles

**Feature**: 003-profiles  
**Date**: 2026-01-30  
**Purpose**: Development setup and test instructions for profile enhancements

---

## Prerequisites

- Existing dottie solution from 001-yaml-configuration
- .NET 10 SDK
- All existing tests passing

## Verify Current State

```bash
# From repository root
cd c:\code\ryandanthony\dottie

# Ensure all existing tests pass
dotnet test

# Verify current profile functionality
dotnet run --project src/Dottie.Cli -- validate
```

---

## Development Tasks Overview

This feature extends existing infrastructure. No new projects are needed.

### Files to Modify

| File | Change |
|------|--------|
| `src/Dottie.Configuration/Inheritance/ProfileMerger.cs` | Update `MergeDotfiles()` for deduplication |
| `src/Dottie.Configuration/ProfileResolver.cs` | Add implicit default profile support |
| `src/Dottie.Configuration/Validation/ConfigurationValidator.cs` | Add profile name validation |
| `src/Dottie.Cli/Commands/ValidateCommandSettings.cs` | Convert positional arg to `--profile` option |

### Files to Create

| File | Purpose |
|------|---------|
| `src/Dottie.Configuration/ProfileInfo.cs` | Summary info for enhanced listing |
| `src/Dottie.Cli/Commands/ProfileAwareSettings.cs` | Base class with `--profile` option |

### Test Files to Create/Modify

| File | Purpose |
|------|---------|
| `tests/Dottie.Configuration.Tests/Inheritance/ProfileMergerTests.cs` | Add deduplication tests |
| `tests/Dottie.Configuration.Tests/ProfileResolverTests.cs` | Add implicit default tests |
| `tests/Dottie.Configuration.Tests/Validation/ProfileNameValidatorTests.cs` | New - profile name validation |
| `tests/Dottie.Configuration.Tests/Fixtures/profile-invalid-name.yaml` | Test fixture |
| `tests/Dottie.Configuration.Tests/Fixtures/profile-dedup.yaml` | Test fixture |

---

## TDD Workflow

### Step 1: Add Test Fixtures

```bash
# Create new test fixtures
# tests/Dottie.Configuration.Tests/Fixtures/profile-invalid-name.yaml
# tests/Dottie.Configuration.Tests/Fixtures/profile-dedup.yaml
```

### Step 2: Write Failing Tests (RED)

```bash
# Run tests - should fail initially
dotnet test tests/Dottie.Configuration.Tests
```

### Step 3: Implement (GREEN)

```bash
# Implement minimum code to pass tests
dotnet test tests/Dottie.Configuration.Tests
```

### Step 4: Verify Coverage

```bash
# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults

# Generate report
reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html
```

---

## Build and Test Commands

```bash
# Build solution
dotnet build

# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~ProfileMergerTests"

# Run with verbose output
dotnet test --verbosity detailed

# Run CLI manually
dotnet run --project src/Dottie.Cli -- validate --profile default
dotnet run --project src/Dottie.Cli -- validate --profile work
```

---

## Validation Checklist

Before completing this feature:

- [ ] All existing tests still pass
- [ ] New tests achieve ≥90% line coverage, ≥80% branch coverage
- [ ] `--profile` flag works on validate command
- [ ] Implicit default profile works when not defined
- [ ] Profile name validation rejects invalid characters
- [ ] Dotfile deduplication by target works correctly
- [ ] Profile listing shows inheritance relationships
