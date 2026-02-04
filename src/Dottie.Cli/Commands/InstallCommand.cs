// -----------------------------------------------------------------------
// <copyright file="InstallCommand.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Output;
using Dottie.Cli.Utilities;
using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models;
using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Parsing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Command to install tools from configured sources.
/// </summary>
/// <remarks>
/// S1200 is suppressed because CLI command classes inherently coordinate multiple components.
/// This is an orchestration class that delegates work to specialized installer services.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Major Code Smell",
    "S1200:Classes should not be coupled to too many other classes (Single Responsibility Principle)",
    Justification = "CLI command classes inherently orchestrate multiple components. This command coordinates multiple installer services.")]
public sealed class InstallCommand : AsyncCommand<InstallCommandSettings>
{
    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, InstallCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
        {
            return 1;
        }

        var configPath = settings.ConfigPath ?? Path.Combine(repoRoot, "dottie.yaml");
        if (!File.Exists(configPath))
        {
            RenderError("Could not find dottie.yaml in the repository root.");
            return 1;
        }

        try
        {
            return await ExecuteInstallAsync(configPath, repoRoot, settings);
        }
        catch (Exception ex)
        {
            RenderError($"Installation failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ExecuteInstallAsync(string configPath, string repoRoot, InstallCommandSettings settings)
    {
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(configPath);

        if (!loadResult.IsSuccess)
        {
            RenderError("Failed to load configuration.");
            return 1;
        }

        var profileName = settings.ProfileName ?? "default";
        var resolveResult = ResolveProfile(loadResult.Configuration, profileName);

        if (!resolveResult.IsSuccess)
        {
            RenderError(resolveResult.Error!);
            RenderAvailableProfiles(loadResult.Configuration);
            return 1;
        }

        if (resolveResult.Profile?.Install is null)
        {
            RenderError($"No install block found in profile '{profileName}'.");
            return 1;
        }

        var contextInfo = CreateInstallContext(repoRoot, settings);

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry Run Mode:[/] Previewing installation without making changes");
        }

        var results = await RunInstallersWithProgressAsync(resolveResult.Profile.Install, contextInfo);

        return RenderResultsAndGetExitCode(results);
    }

    private static InheritanceResolveResult ResolveProfile(DottieConfiguration? config, string profileName)
    {
        if (config is null)
        {
            return InheritanceResolveResult.Failure("Configuration is null.");
        }

        var merger = new ProfileMerger(config);
        return merger.Resolve(profileName);
    }

    private static void RenderAvailableProfiles(DottieConfiguration? config)
    {
        if (config?.Profiles is null || config.Profiles.Count == 0)
        {
            return;
        }

        var profileNames = string.Join(", ", config.Profiles.Keys.Order(StringComparer.Ordinal));
        AnsiConsole.MarkupLine($"[dim]Available profiles: {profileNames}[/]");
    }

    private static InstallContext CreateInstallContext(string repoRoot, InstallCommandSettings settings)
    {
        var sudoChecker = new SudoChecker();
        return new InstallContext
        {
            RepoRoot = repoRoot,
            HasSudo = sudoChecker.IsSudoAvailable(),
            DryRun = settings.DryRun,
        };
    }

    private static int GetTotalItemCount(InstallBlock installBlock)
    {
        return installBlock.Github.Count
            + installBlock.Apt.Count
            + installBlock.AptRepos.Count
            + installBlock.Scripts.Count
            + installBlock.Fonts.Count
            + installBlock.Snaps.Count;
    }

    private static async Task<List<InstallResult>> RunInstallersWithProgressAsync(InstallBlock installBlock, InstallContext context)
    {
        var totalItems = GetTotalItemCount(installBlock);
        if (totalItems == 0)
        {
            return [];
        }

        var results = new List<InstallResult>();

        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Installing software[/]", maxValue: totalItems);

                var installerItems = new[]
                {
                    (Source: (IInstallSource)new GithubReleaseInstaller(), Name: "GitHub releases", Count: installBlock.Github.Count),
                    (Source: (IInstallSource)new AptPackageInstaller(), Name: "APT packages", Count: installBlock.Apt.Count),
                    (Source: (IInstallSource)new AptRepoInstaller(), Name: "APT repositories", Count: installBlock.AptRepos.Count),
                    (Source: (IInstallSource)new ScriptRunner(), Name: "Scripts", Count: installBlock.Scripts.Count),
                    (Source: (IInstallSource)new FontInstaller(), Name: "Fonts", Count: installBlock.Fonts.Count),
                    (Source: (IInstallSource)new SnapPackageInstaller(), Name: "Snap packages", Count: installBlock.Snaps.Count),
                };

                foreach (var item in installerItems)
                {
                    if (item.Count == 0)
                    {
                        continue;
                    }

                    task.Description = $"[green]Installing {item.Name}[/]";

                    var installerResults = await ExecuteInstallerAsync(item.Source, installBlock, context);
                    results.AddRange(installerResults);

                    task.Increment(item.Count);
                }

                task.Description = "[green]Installation complete[/]";
            });

        AnsiConsole.WriteLine();

        return results;
    }

    private static async Task<IEnumerable<InstallResult>> ExecuteInstallerAsync(IInstallSource installer, InstallBlock installBlock, InstallContext context)
    {
        try
        {
            return installer.SourceType switch
            {
                InstallSourceType.GithubRelease => await ((GithubReleaseInstaller)installer).InstallAsync(installBlock, context),
                InstallSourceType.AptPackage => await ((AptPackageInstaller)installer).InstallAsync(installBlock, context),
                InstallSourceType.AptRepo => await ((AptRepoInstaller)installer).InstallAsync(installBlock, context),
                InstallSourceType.Script => await ((ScriptRunner)installer).InstallAsync(installBlock, context),
                InstallSourceType.Font => await ((FontInstaller)installer).InstallAsync(installBlock, context),
                InstallSourceType.SnapPackage => await ((SnapPackageInstaller)installer).InstallAsync(installBlock, context),
                _ => [],
            };
        }
        catch (Exception ex)
        {
            return [InstallResult.Failed("unknown", InstallSourceType.GithubRelease, $"Installer error: {ex.Message}")];
        }
    }

    private static int RenderResultsAndGetExitCode(List<InstallResult> results)
    {
        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] No tools configured to install.");
            return 0;
        }

        var renderer = new InstallProgressRenderer();
        renderer.RenderSummary(results);
        renderer.RenderGroupedFailures(results);

        return results.Exists(r => r.Status == InstallStatus.Failed) ? 1 : 0;
    }

    private static void RenderError(string message)
    {
        var renderer = new InstallProgressRenderer();
        renderer.RenderError(message);
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
}
