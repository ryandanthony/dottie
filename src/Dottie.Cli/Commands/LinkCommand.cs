// -----------------------------------------------------------------------
// <copyright file="LinkCommand.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Output;
using Dottie.Cli.Utilities;
using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Linking;
using Dottie.Configuration.Parsing;
using Dottie.Configuration.Validation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Command to create symbolic links for dotfiles.
/// </summary>
public sealed class LinkCommand : Command<LinkCommandSettings>
{
    /// <inheritdoc/>
    public override int Execute(CommandContext context, LinkCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
        {
            return 1;
        }

        var profile = LoadAndResolveProfile(settings, repoRoot, out var exitCode);
        if (profile is null)
        {
            return exitCode;
        }

        if (settings.DryRun)
        {
            return ExecuteDryRun(profile, repoRoot);
        }

        return ExecuteLinking(profile, repoRoot, settings.Force);
    }

    private static string? FindRepoRoot()
    {
        var repoRoot = RepoRootFinder.Find();
        if (repoRoot is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not find git repository root.");
            AnsiConsole.MarkupLine("[yellow]Hint:[/] Make sure you're running from within a git repository.");
        }

        return repoRoot;
    }

    private static ResolvedProfile? LoadAndResolveProfile(LinkCommandSettings settings, string repoRoot, out int exitCode)
    {
        var configPath = settings.ConfigPath ?? Path.Combine(repoRoot, "dottie.yaml");
        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not find dottie.yaml in the repository root.");
            exitCode = 1;
            return null;
        }

        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(configPath);

        if (!loadResult.IsSuccess)
        {
            ErrorFormatter.WriteErrors(loadResult.Errors.ToList());
            exitCode = 1;
            return null;
        }

        var validator = new ConfigurationValidator();
        var validationResult = validator.Validate(loadResult.Configuration!);

        if (!validationResult.IsValid)
        {
            ErrorFormatter.WriteErrors(validationResult.Errors.ToList());
            exitCode = 1;
            return null;
        }

        var profileName = settings.ProfileName ?? "default";
        var merger = new ProfileMerger(loadResult.Configuration!);
        var mergeResult = merger.Resolve(profileName);

        if (!mergeResult.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {mergeResult.Error}");
            exitCode = 1;
            return null;
        }

        if (mergeResult.Profile!.Dotfiles.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Profile '{profileName}' has no dotfiles to link.");
            exitCode = 0;
            return null;
        }

        exitCode = 0;
        return mergeResult.Profile;
    }

    private static int ExecuteDryRun(ResolvedProfile profile, string repoRoot)
    {
        var conflictDetector = new ConflictDetector();
        var dotfiles = profile.Dotfiles.ToList().AsReadOnly();
        var conflictResult = conflictDetector.DetectConflicts(dotfiles, repoRoot);

        ConflictFormatter.WriteDryRunPreview(
            conflictResult.SafeEntries.Select(e => e).ToList(),
            conflictResult.AlreadyLinked.Select(e => e).ToList(),
            conflictResult.Conflicts.ToList(),
            repoRoot);

        return 0;
    }

    private static int ExecuteLinking(ResolvedProfile profile, string repoRoot, bool force)
    {
        LinkExecutionResult? result = null;
        var dotfileCount = profile.Dotfiles.Count;

        try
        {
            AnsiConsole.Progress()
                .AutoClear(true)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .Start(ctx =>
                {
                    var task = ctx.AddTask("[green]Linking dotfiles[/]", maxValue: dotfileCount);

                    var orchestrator = new LinkingOrchestrator();
                    result = orchestrator.ExecuteLink(profile, repoRoot, force);

                    // Update progress to 100% once linking is complete
                    task.Value = dotfileCount;
                });
        }
        catch (InvalidOperationException)
        {
            // Progress display not allowed (e.g., another interactive operation is running or in test environment)
            // Fall back to running without progress bar
            var orchestrator = new LinkingOrchestrator();
            result = orchestrator.ExecuteLink(profile, repoRoot, force);
        }

        if (result!.IsBlocked)
        {
            ConflictFormatter.WriteConflicts(result.ConflictResult!.Conflicts.ToList());
            return 1;
        }

        if (result.BackupResults.Count > 0)
        {
            ConflictFormatter.WriteBackupResults(result.BackupResults.ToList());
        }

        ConflictFormatter.WriteLinkResults(result.LinkResult!);

        return result.LinkResult!.IsSuccess ? 0 : 1;
    }
}
