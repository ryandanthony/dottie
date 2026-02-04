// -----------------------------------------------------------------------
// <copyright file="StatusCommand.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Output;
using Dottie.Cli.Utilities;
using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Parsing;
using Dottie.Configuration.Status;
using Dottie.Configuration.Validation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Command to display status of dotfiles and software installations.
/// </summary>
public sealed class StatusCommand : AsyncCommand<StatusCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, StatusCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
        {
            return 1;
        }

        var result = LoadAndResolveProfile(settings, repoRoot, out var exitCode, out var inheritanceChain);
        if (result is null)
        {
            return exitCode;
        }

        var report = await BuildStatusReportAsync(result, repoRoot, inheritanceChain).ConfigureAwait(false);
        StatusFormatter.WriteStatusReport(report);

        return 0;
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

    private static ResolvedProfile? LoadAndResolveProfile(
        StatusCommandSettings settings,
        string repoRoot,
        out int exitCode,
        out IReadOnlyList<string> inheritanceChain)
    {
        inheritanceChain = [];

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

        inheritanceChain = mergeResult.Profile?.InheritanceChain ?? [profileName];
        exitCode = 0;
        return mergeResult.Profile;
    }

    private static async Task<StatusReport> BuildStatusReportAsync(
        ResolvedProfile profile,
        string repoRoot,
        IReadOnlyList<string> inheritanceChain)
    {
        // Check dotfile status
        var dotfileChecker = new DotfileStatusChecker();
        var dotfileStatuses = dotfileChecker.CheckStatus(profile.Dotfiles.ToList(), repoRoot);

        // Check software status
        var softwareStatuses = await CheckSoftwareStatusAsync(profile, repoRoot).ConfigureAwait(false);

        return new StatusReport(
            profile.Name ?? "default",
            inheritanceChain,
            dotfileStatuses,
            softwareStatuses);
    }

    private static async Task<IReadOnlyList<SoftwareStatusEntry>> CheckSoftwareStatusAsync(
        ResolvedProfile profile,
        string repoRoot)
    {
        if (profile.Install is null)
        {
            return [];
        }

        var context = new InstallContext
        {
            RepoRoot = repoRoot,
            BinDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "bin"),
            FontDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "share",
                "fonts"),
            DryRun = true, // We're just checking status, not installing
        };

        var checker = new SoftwareStatusChecker();
        return await checker.CheckStatusAsync(profile.Install, context, CancellationToken.None).ConfigureAwait(false);
    }
}
