# Quickstart: CLI Link Command Implementation

**Feature**: 005-cli-link  
**Date**: January 31, 2026

## Prerequisites

- .NET 10.0 SDK installed
- Repository cloned and building (`dotnet build` succeeds)
- Familiarity with Spectre.Console.Cli command pattern

## Development Environment Setup

```powershell
# Clone and build
git checkout 005-cli-link
dotnet restore
dotnet build

# Run tests
dotnet test

# Run the CLI
dotnet run --project src/Dottie.Cli -- link --help
```

## Key Files to Modify

| File | Purpose | Changes Needed |
|------|---------|----------------|
| `src/Dottie.Cli/Commands/LinkCommandSettings.cs` | CLI settings | Add `DryRun`, validation |
| `src/Dottie.Cli/Commands/LinkCommand.cs` | Command logic | Add dry-run, progress bar |
| `src/Dottie.Cli/Output/ConflictFormatter.cs` | Output formatting | Add dry-run preview methods |
| `src/Dottie.Configuration/Linking/BackupService.cs` | Backup logic | Update naming convention |
| `src/Dottie.Configuration/Linking/SymlinkService.cs` | Symlink creation | Add Windows error handling |

## TDD Workflow

### Step 1: Write Settings Validation Test

```csharp
// tests/Dottie.Cli.Tests/Commands/LinkCommandSettingsTests.cs

[Fact]
public void Validate_WithDryRunAndForce_ReturnsError()
{
    // Arrange
    var settings = new LinkCommandSettings
    {
        DryRun = true,
        Force = true
    };

    // Act
    var result = settings.Validate();

    // Assert
    result.Successful.Should().BeFalse();
    result.Message.Should().Contain("cannot be used together");
}
```

### Step 2: Implement Settings Validation

```csharp
// src/Dottie.Cli/Commands/LinkCommandSettings.cs

public sealed class LinkCommandSettings : ProfileAwareSettings
{
    [Description("Preview without making changes")]
    [CommandOption("-d|--dry-run")]
    public bool DryRun { get; set; }
    
    [Description("Force linking by backing up conflicting files")]
    [CommandOption("-f|--force")]
    public bool Force { get; set; }
    
    public override ValidationResult Validate()
    {
        if (DryRun && Force)
        {
            return ValidationResult.Error(
                "--dry-run and --force cannot be used together.");
        }
        return ValidationResult.Success();
    }
}
```

### Step 3: Write Dry-Run Test

```csharp
// tests/Dottie.Cli.Tests/Commands/LinkCommandTests.cs

[Fact]
public void Execute_WithDryRun_DoesNotCreateSymlinks()
{
    // Arrange
    var settings = new LinkCommandSettings { DryRun = true };
    // ... setup mock orchestrator or test filesystem
    
    // Act
    var result = command.Execute(context, settings);
    
    // Assert
    // Verify no symlinks created
    // Verify preview output displayed
}
```

### Step 4: Implement Dry-Run Logic

The dry-run implementation should:
1. Load configuration and resolve profile (same as normal)
2. Run conflict detection (same as normal)
3. Display preview output instead of executing
4. Return 0 (no filesystem changes = success)

## Progress Bar Implementation

```csharp
// In LinkCommand.cs

private static void ExecuteWithProgress(
    ResolvedProfile profile, 
    string repoRoot, 
    bool force)
{
    AnsiConsole.Progress()
        .Columns(
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new SpinnerColumn())
        .Start(ctx =>
        {
            var task = ctx.AddTask("[green]Linking dotfiles[/]");
            task.MaxValue = profile.Dotfiles.Count;
            
            // Process each dotfile...
            foreach (var dotfile in profile.Dotfiles)
            {
                // Link operation
                task.Increment(1);
            }
        });
}
```

## Backup Naming Convention Update

```csharp
// In BackupService.cs - change GenerateBackupPath method

private static string GenerateBackupPath(string originalPath, DateTimeOffset timestamp)
{
    var timestampStr = timestamp.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
    // Changed from: $"{originalPath}.backup.{timestampStr}"
    var basePath = $"{originalPath}.dottie-backup-{timestampStr}";
    // ... rest of method unchanged
}
```

## Windows Symlink Error Handling

```csharp
// In SymlinkService.cs - enhance CreateSymlink error handling

public bool CreateSymlink(string linkPath, string targetPath)
{
    try
    {
        // ... existing code
    }
    catch (UnauthorizedAccessException) when (OperatingSystem.IsWindows())
    {
        // Store error for later reporting
        LastError = "Unable to create symbolic link - insufficient permissions.\n\n" +
                   "On Windows, symbolic links require either:\n" +
                   "  • Run dottie as Administrator, OR\n" +
                   "  • Enable Developer Mode in Windows Settings";
        return false;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        LastError = ex.Message;
        return false;
    }
}
```

## Running Tests

```powershell
# Run all tests
dotnet test

# Run specific test file
dotnet test --filter "FullyQualifiedName~LinkCommandSettingsTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Verification Checklist

Before PR:

- [ ] All new code has tests written first (TDD)
- [ ] `dotnet test` passes
- [ ] Coverage ≥ 90% line, ≥ 80% branch
- [ ] `dotnet build` with no warnings
- [ ] Manual test: `dottie link --dry-run`
- [ ] Manual test: `dottie link --force`
- [ ] Manual test: `dottie link --dry-run --force` shows error

## Common Issues

### Spectre.Console in Tests

Use `AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) })` to capture output in tests.

### Windows Symlink Testing

Tests that create symlinks may fail on Windows without admin privileges. Use `[SkippableFact]` with platform detection.

### Backup Path Collisions

The BackupService handles collisions by adding numeric suffixes. Ensure tests verify this behavior.
