# API Contract: ApplyCommand

**Feature**: 009-cli-apply  
**Date**: February 3, 2026

## Command Registration

```csharp
// In Program.cs
config.AddCommand<ApplyCommand>("apply")
    .WithDescription("Apply profile: create symlinks and install software")
    .WithExample("apply")
    .WithExample("apply", "--profile", "work")
    .WithExample("apply", "--dry-run")
    .WithExample("apply", "--force")
    .WithExample("apply", "-p", "work", "-f", "--dry-run");
```

## ApplyCommandSettings

```csharp
/// <summary>
/// Settings for the apply command.
/// </summary>
public sealed class ApplyCommandSettings : ProfileAwareSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to force apply by backing up conflicts.
    /// </summary>
    [Description("Force apply by backing up conflicting files")]
    [CommandOption("-f|--force")]
    public bool Force { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to perform a dry run.
    /// </summary>
    [Description("Preview changes without making them")]
    [CommandOption("-d|--dry-run")]
    public bool DryRun { get; set; }
}
```

## ApplyCommand

```csharp
/// <summary>
/// Command to apply profile configuration: link dotfiles and install software.
/// </summary>
public sealed class ApplyCommand : AsyncCommand<ApplyCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ApplyCommandSettings settings);
}
```

### Execution Flow

```csharp
public override async Task<int> ExecuteAsync(CommandContext context, ApplyCommandSettings settings)
{
    // 1. Validate inputs
    ArgumentNullException.ThrowIfNull(settings);

    // 2. Find repository root
    var repoRoot = FindRepoRoot();
    if (repoRoot is null) return 1;

    // 3. Load and validate configuration
    var config = LoadConfiguration(settings, repoRoot);
    if (config is null) return 1;

    // 4. Resolve profile (including inheritance)
    var profile = ResolveProfile(config, settings.ProfileName ?? "default");
    if (profile is null) return 1;

    // 5. Execute dry-run or actual apply
    if (settings.DryRun)
    {
        return ExecuteDryRun(profile, repoRoot);
    }

    return await ExecuteApplyAsync(profile, repoRoot, settings.Force);
}
```

### Exit Codes

| Code | Condition |
|------|-----------|
| 0 | All operations completed successfully |
| 1 | Configuration error, profile error, blocked by conflicts, or any operation failed |

## Result Models

### ApplyResult

```csharp
/// <summary>
/// Aggregated result of the apply operation.
/// </summary>
public sealed class ApplyResult
{
    /// <summary>
    /// Gets the link phase result.
    /// </summary>
    public required LinkPhaseResult LinkPhase { get; init; }

    /// <summary>
    /// Gets the install phase result.
    /// </summary>
    public required InstallPhaseResult InstallPhase { get; init; }

    /// <summary>
    /// Gets a value indicating whether the overall apply succeeded.
    /// </summary>
    public bool OverallSuccess => !LinkPhase.WasBlocked 
        && !LinkPhase.HasFailures 
        && !InstallPhase.HasFailures;
}
```

### LinkPhaseResult

```csharp
/// <summary>
/// Result of the link phase of apply.
/// </summary>
public sealed class LinkPhaseResult
{
    /// <summary>
    /// Gets a value indicating whether the link phase was executed.
    /// </summary>
    public bool WasExecuted { get; init; }

    /// <summary>
    /// Gets a value indicating whether linking was blocked by conflicts.
    /// </summary>
    public bool WasBlocked { get; init; }

    /// <summary>
    /// Gets a value indicating whether any link operation failed.
    /// </summary>
    public bool HasFailures => ExecutionResult?.LinkResult?.FailedLinks.Count > 0;

    /// <summary>
    /// Gets the detailed execution result.
    /// </summary>
    public LinkExecutionResult? ExecutionResult { get; init; }

    /// <summary>
    /// Creates a result for when no dotfiles are configured.
    /// </summary>
    public static LinkPhaseResult NotExecuted() => new() { WasExecuted = false };

    /// <summary>
    /// Creates a result for when linking was blocked by conflicts.
    /// </summary>
    public static LinkPhaseResult Blocked(LinkExecutionResult result) => 
        new() { WasExecuted = true, WasBlocked = true, ExecutionResult = result };

    /// <summary>
    /// Creates a result for successful execution.
    /// </summary>
    public static LinkPhaseResult Executed(LinkExecutionResult result) => 
        new() { WasExecuted = true, WasBlocked = false, ExecutionResult = result };
}
```

### InstallPhaseResult

```csharp
/// <summary>
/// Result of the install phase of apply.
/// </summary>
public sealed class InstallPhaseResult
{
    /// <summary>
    /// Gets a value indicating whether the install phase was executed.
    /// </summary>
    public bool WasExecuted { get; init; }

    /// <summary>
    /// Gets the individual install results.
    /// </summary>
    public IReadOnlyList<InstallResult> Results { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether any installation failed.
    /// </summary>
    public bool HasFailures => Results.Any(r => r.Status == InstallStatus.Failed);

    /// <summary>
    /// Creates a result for when no install block is configured.
    /// </summary>
    public static InstallPhaseResult NotExecuted() => new() { WasExecuted = false };

    /// <summary>
    /// Creates a result with the given install results.
    /// </summary>
    public static InstallPhaseResult Executed(IReadOnlyList<InstallResult> results) => 
        new() { WasExecuted = true, Results = results };
}
```
