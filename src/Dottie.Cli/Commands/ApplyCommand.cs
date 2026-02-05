// -----------------------------------------------------------------------
// <copyright file="ApplyCommand.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Models;
using Dottie.Cli.Output;
using Dottie.Cli.Utilities;
using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Linking;
using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Parsing;
using Dottie.Configuration.Validation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Command to apply profile configuration: link dotfiles and install software.
/// </summary>
/// <remarks>
/// S1200 is suppressed because CLI command classes inherently coordinate multiple components.
/// This is an orchestration class that delegates work to specialized services.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Major Code Smell",
    "S1200:Classes should not be coupled to too many other classes (Single Responsibility Principle)",
    Justification = "CLI command classes inherently orchestrate multiple components. This command coordinates linking and installation services.")]
public sealed class ApplyCommand : AsyncCommand<ApplyCommandSettings>
{
    private readonly IApplyProgressRenderer _renderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyCommand"/> class.
    /// </summary>
    /// <param name="renderer">Optional custom renderer for testing.</param>
    public ApplyCommand(IApplyProgressRenderer? renderer = null)
    {
        _renderer = renderer ?? new ApplyProgressRenderer();
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, ApplyCommandSettings settings)
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

        var profileName = settings.ProfileName ?? "default";

        if (settings.DryRun)
        {
            _renderer.RenderDryRunPreview(profile, repoRoot);
            return 0;
        }

        var result = await ExecuteApplyAsync(profile, repoRoot, settings.Force);
        _renderer.RenderVerboseSummary(result, profileName);

        return result.OverallSuccess ? 0 : 1;
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

    private ResolvedProfile? LoadAndResolveProfile(ApplyCommandSettings settings, string repoRoot, out int exitCode)
    {
        var configPath = settings.ConfigPath ?? Path.Combine(repoRoot, "dottie.yaml");
        if (!File.Exists(configPath))
        {
            _renderer.RenderError("Could not find dottie.yaml in the repository root.");
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
            _renderer.RenderError(mergeResult.Error!);
            exitCode = 1;
            return null;
        }

        // Profile must have either dotfiles or install block
        if (mergeResult.Profile!.Dotfiles.Count == 0 && mergeResult.Profile.Install is null)
        {
            _renderer.RenderError($"Profile '{profileName}' has no dotfiles or install block configured.");
            exitCode = 1;
            return null;
        }

        exitCode = 0;
        return mergeResult.Profile;
    }

    private static async Task<ApplyResult> ExecuteApplyAsync(ResolvedProfile profile, string repoRoot, bool force)
    {
        var dotfileCount = profile.Dotfiles.Count;
        var installCount = InstallerProgressHelper.GetTotalItemCount(profile.Install);

        LinkPhaseResult? linkPhase = null;
        InstallPhaseResult? installPhase = null;

        try
        {
            (linkPhase, installPhase) = await ExecuteWithProgressAsync(profile, repoRoot, force, dotfileCount, installCount);
        }
        catch (InvalidOperationException)
        {
            // Progress display not allowed (e.g., in test environment)
            (linkPhase, installPhase) = await ExecuteWithoutProgressAsync(profile, repoRoot, force, dotfileCount, installCount);
        }

        AnsiConsole.WriteLine();

        return new ApplyResult
        {
            LinkPhase = linkPhase ?? LinkPhaseResult.NotExecuted(),
            InstallPhase = installPhase ?? InstallPhaseResult.NotExecuted(),
        };
    }

    private static async Task<(LinkPhaseResult link, InstallPhaseResult install)> ExecuteWithProgressAsync(
        ResolvedProfile profile,
        string repoRoot,
        bool force,
        int dotfileCount,
        int installCount)
    {
        var totalItems = dotfileCount + installCount;
        LinkPhaseResult? linkPhase = null;
        InstallPhaseResult? installPhase = null;

        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Applying configuration[/]", maxValue: totalItems);

                linkPhase = ExecuteLinkPhase(profile, repoRoot, force, dotfileCount, task);

                if (linkPhase.WasBlocked)
                {
                    task.Description = "[red]Blocked by conflicts[/]";
                    return;
                }

                installPhase = profile.Install is not null && installCount > 0
                    ? await ExecuteInstallPhaseWithProgressAsync(profile, repoRoot, task)
                    : InstallPhaseResult.NotExecuted();

                task.Description = "[green]Apply complete[/]";
            });

        return (linkPhase ?? LinkPhaseResult.NotExecuted(), installPhase ?? InstallPhaseResult.NotExecuted());
    }

    private static async Task<(LinkPhaseResult link, InstallPhaseResult install)> ExecuteWithoutProgressAsync(
        ResolvedProfile profile,
        string repoRoot,
        bool force,
        int dotfileCount,
        int installCount)
    {
        var linkPhase = dotfileCount > 0
            ? ExecuteLinkPhaseInternal(profile, repoRoot, force)
            : LinkPhaseResult.NotExecuted();

        var installPhase = !linkPhase.WasBlocked && profile.Install is not null && installCount > 0
            ? await ExecuteInstallPhaseWithoutProgressAsync(profile)
            : InstallPhaseResult.NotExecuted();

        return (linkPhase, installPhase);
    }

    private static LinkPhaseResult ExecuteLinkPhase(ResolvedProfile profile, string repoRoot, bool force, int dotfileCount, ProgressTask task)
    {
        if (dotfileCount > 0)
        {
            task.Description = "[green]Linking dotfiles[/]";
            var result = ExecuteLinkPhaseInternal(profile, repoRoot, force);
            task.Increment(dotfileCount);
            return result;
        }

        return LinkPhaseResult.NotExecuted();
    }

    private static LinkPhaseResult ExecuteLinkPhaseInternal(ResolvedProfile profile, string repoRoot, bool force)
    {
        var orchestrator = new LinkingOrchestrator();
        var result = orchestrator.ExecuteLink(profile, repoRoot, force);

        if (result.IsBlocked)
        {
            return LinkPhaseResult.Blocked(result);
        }

        return LinkPhaseResult.Executed(result);
    }

    private static async Task<InstallPhaseResult> ExecuteInstallPhaseWithProgressAsync(
        ResolvedProfile profile,
        string repoRoot,
        ProgressTask task)
    {
        var context = CreateInstallContext(repoRoot);
        var results = new List<InstallResult>();

        if (profile.Install is null)
        {
            return InstallPhaseResult.NotExecuted();
        }

        var installerItems = InstallerProgressHelper.GetInstallerItems(profile.Install);

        foreach (var item in installerItems)
        {
            if (item.Count == 0)
            {
                continue;
            }

            task.Description = $"[green]Installing {item.Name}[/]";

            var installerResults = await ExecuteInstallerAsync(item.Installer, profile.Install, context, () => task.Increment(1));
            results.AddRange(installerResults);
        }

        return InstallPhaseResult.Executed(results);
    }

    private static async Task<InstallPhaseResult> ExecuteInstallPhaseWithoutProgressAsync(ResolvedProfile profile)
    {
        var context = CreateInstallContext(RepoRootFinder.Find() ?? ".");
        var results = new List<InstallResult>();

        if (profile.Install is null)
        {
            return InstallPhaseResult.NotExecuted();
        }

        var installerItems = InstallerProgressHelper.GetInstallerItems(profile.Install);

        foreach (var item in installerItems)
        {
            if (item.Count == 0)
            {
                continue;
            }

            var installerResults = await ExecuteInstallerAsync(item.Installer, profile.Install, context, null);
            results.AddRange(installerResults);
        }

        return InstallPhaseResult.Executed(results);
    }

    private static InstallContext CreateInstallContext(string repoRoot)
    {
        var sudoChecker = new SudoChecker();
        return new InstallContext
        {
            RepoRoot = repoRoot,
            HasSudo = sudoChecker.IsSudoAvailable(),
            DryRun = false,
        };
    }

    private static async Task<IEnumerable<InstallResult>> ExecuteInstallerAsync(
        IInstallSource installer,
        InstallBlock installBlock,
        InstallContext context,
        Action? onItemComplete)
    {
        try
        {
            return installer.SourceType switch
            {
                InstallSourceType.GithubRelease => await ((GithubReleaseInstaller)installer).InstallAsync(installBlock, context, onItemComplete),
                InstallSourceType.AptPackage => await ((AptPackageInstaller)installer).InstallAsync(installBlock, context, onItemComplete),
                InstallSourceType.AptRepo => await ((AptRepoInstaller)installer).InstallAsync(installBlock, context, onItemComplete),
                InstallSourceType.Script => await ((ScriptRunner)installer).InstallAsync(installBlock, context, onItemComplete),
                InstallSourceType.Font => await ((FontInstaller)installer).InstallAsync(installBlock, context, onItemComplete),
                InstallSourceType.SnapPackage => await ((SnapPackageInstaller)installer).InstallAsync(installBlock, context, onItemComplete),
                _ => [],
            };
        }
        catch (Exception ex)
        {
            // Log the failure but continue with other installers (fail-soft)
            return [InstallResult.Failed(
                installer.SourceType.ToString(),
                installer.SourceType,
                $"Installer error: {ex.Message}")];
        }
    }
}
