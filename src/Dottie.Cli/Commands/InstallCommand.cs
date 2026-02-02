// -----------------------------------------------------------------------
// <copyright file="InstallCommand.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Output;
using Dottie.Cli.Utilities;
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

        var profile = GetProfile(loadResult.Configuration, settings.ProfileName ?? "default");
        if (profile?.Install is null)
        {
            RenderError($"No install block found in profile '{settings.ProfileName ?? "default"}'.");
            return 1;
        }

        var contextInfo = CreateInstallContext(repoRoot, settings);

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[yellow]Dry Run Mode:[/] Previewing installation without making changes");
        }

        var results = await RunInstallersAsync(profile.Install, contextInfo);

        return RenderResultsAndGetExitCode(results);
    }

    private static ConfigProfile? GetProfile(DottieConfiguration? config, string profileName)
    {
        return config?.Profiles?.ContainsKey(profileName) == true
            ? config.Profiles[profileName]
            : null;
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

    private static async Task<List<InstallResult>> RunInstallersAsync(InstallBlock installBlock, InstallContext context)
    {
        var installers = new List<IInstallSource>
        {
            new GithubReleaseInstaller(),
            new AptPackageInstaller(),
            new AptRepoInstaller(),
            new ScriptRunner(),
            new FontInstaller(),
            new SnapPackageInstaller(),
        };

        var results = new List<InstallResult>();

        foreach (var installer in installers)
        {
            var installerResults = await ExecuteInstallerAsync(installer, installBlock, context);
            results.AddRange(installerResults);
        }

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
