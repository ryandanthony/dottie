# Quickstart: Conflict Handling for Dotfile Linking

**Feature**: 004-conflict-handling  
**Date**: 2026-01-30  
**Purpose**: Development setup and TDD workflow instructions

---

## Prerequisites

- Existing dottie solution from 001-yaml-configuration and 003-profiles
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

This feature creates new infrastructure for linking operations while extending existing CLI patterns.

### Files to Create

| File | Purpose |
|------|---------|
| `src/Dottie.Configuration/Linking/ConflictType.cs` | Enum for conflict types |
| `src/Dottie.Configuration/Linking/Conflict.cs` | Single conflict entity |
| `src/Dottie.Configuration/Linking/ConflictResult.cs` | Aggregate conflict result |
| `src/Dottie.Configuration/Linking/ConflictDetector.cs` | Conflict detection service |
| `src/Dottie.Configuration/Linking/BackupResult.cs` | Backup operation result |
| `src/Dottie.Configuration/Linking/BackupService.cs` | Backup service |
| `src/Dottie.Configuration/Linking/LinkResult.cs` | Link operation result |
| `src/Dottie.Configuration/Linking/LinkOperationResult.cs` | Aggregate link result |
| `src/Dottie.Configuration/Linking/SymlinkService.cs` | Symlink creation service |
| `src/Dottie.Cli/Commands/LinkCommand.cs` | dottie link command |
| `src/Dottie.Cli/Commands/LinkCommandSettings.cs` | Link command settings |
| `src/Dottie.Cli/Output/ConflictFormatter.cs` | Conflict output formatting |

### Files to Modify

| File | Change |
|------|--------|
| `src/Dottie.Cli/Commands/ProfileAwareSettings.cs` | Add `--force` flag |
| `src/Dottie.Cli/Program.cs` | Register `link` command |

### Test Files to Create

| File | Purpose |
|------|---------|
| `tests/Dottie.Configuration.Tests/Linking/ConflictDetectorTests.cs` | Conflict detection tests |
| `tests/Dottie.Configuration.Tests/Linking/BackupServiceTests.cs` | Backup service tests |
| `tests/Dottie.Configuration.Tests/Linking/SymlinkServiceTests.cs` | Symlink service tests |
| `tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs` | Link command tests |
| `tests/Dottie.Cli.Tests/Output/ConflictFormatterTests.cs` | Output formatter tests |

### Integration Test Files

| File | Purpose |
|------|---------|
| `tests/integration/scenarios/conflict-handling/dottie.yml` | Integration test config |
| `tests/integration/scenarios/conflict-handling/validate.sh` | Integration validation |
| `tests/integration/scenarios/conflict-handling/README.md` | Scenario documentation |

---

## TDD Workflow

### Phase 1: Conflict Detection (Priority P1)

#### Step 1: Create Conflict Types (Tests First)

```bash
# Create test file first
# tests/Dottie.Configuration.Tests/Linking/ConflictDetectorTests.cs
```

**Test scenarios to implement:**
1. `DetectConflicts_WhenNoTargetsExist_ReturnsNoConflicts`
2. `DetectConflicts_WhenTargetFileExists_ReturnsFileConflict`
3. `DetectConflicts_WhenTargetDirectoryExists_ReturnsDirectoryConflict`
4. `DetectConflicts_WhenTargetIsCorrectSymlink_ReturnsNoConflict`
5. `DetectConflicts_WhenTargetIsMismatchedSymlink_ReturnsMismatchedSymlinkConflict`
6. `DetectConflicts_WithMultipleConflicts_ReturnsAllConflicts`

```bash
# Run tests - should fail initially (RED)
dotnet test tests/Dottie.Configuration.Tests --filter "ConflictDetector"
```

#### Step 2: Implement Conflict Detection (GREEN)

Create types in order:
1. `ConflictType.cs`
2. `Conflict.cs`
3. `ConflictResult.cs`
4. `ConflictDetector.cs`

```bash
# Run tests - should pass (GREEN)
dotnet test tests/Dottie.Configuration.Tests --filter "ConflictDetector"
```

### Phase 2: Backup Service (Priority P2)

#### Step 1: Create Backup Tests

**Test scenarios:**
1. `Backup_WhenFileExists_CreatesBackupWithTimestamp`
2. `Backup_WhenDirectoryExists_CreatesBackupWithTimestamp`
3. `Backup_WhenBackupNameExists_AppendsNumericSuffix`
4. `Backup_WhenPermissionDenied_ReturnsFailure`
5. `Backup_PreservesOriginalContent`

#### Step 2: Implement Backup Service

Create:
1. `BackupResult.cs`
2. `BackupService.cs`

### Phase 3: Symlink Service

#### Step 1: Create Symlink Tests

**Test scenarios:**
1. `CreateSymlink_WhenTargetDoesNotExist_CreatesSymlink`
2. `CreateSymlink_WhenParentDirectoryMissing_CreatesDirectoryAndSymlink`
3. `IsCorrectSymlink_WhenPointsToExpectedTarget_ReturnsTrue`
4. `IsCorrectSymlink_WhenPointsToDifferentTarget_ReturnsFalse`

#### Step 2: Implement Symlink Service

Create:
1. `SymlinkService.cs`
2. `LinkResult.cs`
3. `LinkOperationResult.cs`

### Phase 4: CLI Integration

#### Step 1: Add --force flag

Modify `ProfileAwareSettings.cs`

#### Step 2: Create Link Command Tests

**Test scenarios:**
1. `Execute_WhenNoConflicts_CreatesSymlinks`
2. `Execute_WhenConflictsExist_ReturnsErrorWithConflictList`
3. `Execute_WhenConflictsExistWithForce_BacksUpAndLinks`
4. `Execute_WhenAlreadyLinked_SkipsWithoutError`

#### Step 3: Implement Link Command

Create:
1. `LinkCommandSettings.cs`
2. `LinkCommand.cs`
3. `ConflictFormatter.cs`
4. Update `Program.cs`

---

## Build and Test Commands

```bash
# Build entire solution
dotnet build

# Run all unit tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~ConflictDetectorTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults

# Generate coverage report
reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:Html

# Verify coverage meets threshold (90% line, 80% branch)
# Check coverage-report/index.html
```

---

## Integration Testing

```bash
# Build for Linux
dotnet publish src/Dottie.Cli -c Release -r linux-x64 --self-contained -o ./publish/linux-x64

# Build Docker image
docker build -t dottie-integration-test -f tests/integration/Dockerfile .

# Run integration tests
docker run --rm dottie-integration-test
```

---

## Manual Testing Workflow

```bash
# Create test directory structure
mkdir -p ~/test-dottie/repo/shell
mkdir -p ~/test-dottie/repo/git
echo "# test bashrc" > ~/test-dottie/repo/shell/bashrc
echo "# test gitconfig" > ~/test-dottie/repo/git/gitconfig

# Create existing files (conflicts)
echo "existing bashrc" > ~/.bashrc.test
echo "existing gitconfig" > ~/.gitconfig.test

# Create test config
cat > ~/test-dottie/repo/dottie.yaml << 'EOF'
profiles:
  default:
    dotfiles:
      - source: shell/bashrc
        target: ~/.bashrc.test
      - source: git/gitconfig
        target: ~/.gitconfig.test
EOF

# Test conflict detection (should fail with conflict list)
cd ~/test-dottie/repo
dotnet run --project /path/to/src/Dottie.Cli -- link

# Test force backup (should create backups and symlinks)
dotnet run --project /path/to/src/Dottie.Cli -- link --force

# Verify backups exist
ls -la ~/.bashrc.test.backup.*
ls -la ~/.gitconfig.test.backup.*

# Verify symlinks point to correct targets
ls -la ~/.bashrc.test
ls -la ~/.gitconfig.test

# Cleanup
rm -rf ~/test-dottie
rm -f ~/.bashrc.test* ~/.gitconfig.test*
```

---

## Expected CLI Output

### Conflict Detection (no --force)

```
Error: Conflicting files detected. Use --force to backup and overwrite.

Conflicts:
  • ~/.bashrc (file)
  • ~/.gitconfig (file)

Found 2 conflict(s).
```

### Successful Force Backup

```
Backed up and linked 2 file(s):
  • ~/.bashrc → ~/.bashrc.backup.20260130-143022
  • ~/.gitconfig → ~/.gitconfig.backup.20260130-143022

Created 2 symlink(s).
```

### Already Linked (skipped)

```
Link operation complete:
  • 0 file(s) linked
  • 2 file(s) skipped (already linked)
```

---

## Verification Checklist

- [ ] All unit tests pass
- [ ] Coverage ≥90% line, ≥80% branch
- [ ] Integration tests pass in Docker
- [ ] Manual testing confirms expected output
- [ ] No analyzer warnings (FestinaLente.CodeStandards)
- [ ] All new files have XML documentation
- [ ] Copyright headers present on all files
