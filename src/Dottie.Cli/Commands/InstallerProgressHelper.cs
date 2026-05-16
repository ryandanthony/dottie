// -----------------------------------------------------------------------
// <copyright file="InstallerProgressHelper.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Cli.Commands;

/// <summary>
/// Helper class for installer progress tracking.
/// </summary>
internal static class InstallerProgressHelper
{
    private const int GitHubStage = 0;
    private const int AptPackagesStage = 1;
    private const int AptRepositoriesStage = 2;
    private const int FinalParallelStage = 3;
    private const int GitHubSourceOrder = 0;
    private const int AptPackagesSourceOrder = 1;
    private const int AptRepositoriesSourceOrder = 2;
    private const int ScriptsSourceOrder = 3;
    private const int FontsSourceOrder = 4;
    private const int SnapPackagesSourceOrder = 5;

    /// <summary>
    /// Gets the total number of items to install in an install block.
    /// </summary>
    /// <param name="installBlock">The install block to count items from.</param>
    /// <returns>The total number of items across all installer types.</returns>
    internal static int GetTotalItemCount(InstallBlock? installBlock)
    {
        if (installBlock is null)
        {
            return 0;
        }

        return installBlock.Github.Count
            + installBlock.Apt.Count
            + installBlock.AptRepos.Count
            + installBlock.Scripts.Count
            + installBlock.Fonts.Count
            + installBlock.Snaps.Count;
    }

    /// <summary>
    /// Gets the installer configuration items in order of priority.
    /// </summary>
    /// <param name="installBlock">The install block to get installers from.</param>
    /// <returns>An array of tuples containing the installer, display name, and item count.</returns>
    internal static InstallerExecutionPlan[] GetInstallerItems(InstallBlock installBlock)
    {
        ArgumentNullException.ThrowIfNull(installBlock);

        return
        [
            new((IInstallSource)new GithubReleaseInstaller(), "GitHub releases", installBlock.Github.Count, GitHubStage, true),
            new((IInstallSource)new AptPackageInstaller(), "APT packages", installBlock.Apt.Count, AptPackagesStage, false),
            new((IInstallSource)new AptRepoInstaller(), "APT repositories", installBlock.AptRepos.Count, AptRepositoriesStage, false),
            new((IInstallSource)new ScriptRunner(), "Scripts", installBlock.Scripts.Count, FinalParallelStage, true),
            new((IInstallSource)new FontInstaller(), "Fonts", installBlock.Fonts.Count, FinalParallelStage, true),
            new((IInstallSource)new SnapPackageInstaller(), "Snap packages", installBlock.Snaps.Count, FinalParallelStage, true),
        ];
    }

    /// <summary>
    /// Gets a stable source type display name for summaries.
    /// </summary>
    internal static string GetSourceTypeName(InstallSourceType sourceType) => sourceType switch
    {
        InstallSourceType.GithubRelease => "GitHub Releases",
        InstallSourceType.AptPackage => "APT Packages",
        InstallSourceType.AptRepo => "APT Repositories",
        InstallSourceType.Script => "Shell Scripts",
        InstallSourceType.Font => "Fonts",
        InstallSourceType.SnapPackage => "Snap Packages",
        _ => "Other",
    };

    /// <summary>
    /// Gets a stable ordering key for install source types in summaries.
    /// </summary>
    internal static int GetSourceTypeOrder(InstallSourceType sourceType) => sourceType switch
    {
        InstallSourceType.GithubRelease => GitHubSourceOrder,
        InstallSourceType.AptPackage => AptPackagesSourceOrder,
        InstallSourceType.AptRepo => AptRepositoriesSourceOrder,
        InstallSourceType.Script => ScriptsSourceOrder,
        InstallSourceType.Font => FontsSourceOrder,
        InstallSourceType.SnapPackage => SnapPackagesSourceOrder,
        _ => int.MaxValue,
    };

    /// <summary>
    /// Execution metadata for a single installer workload.
    /// </summary>
    /// <param name="Installer">Installer instance to execute.</param>
    /// <param name="Name">Display name for progress output.</param>
    /// <param name="Count">Number of items in the workload.</param>
    /// <param name="Stage">Execution stage, used to preserve required ordering boundaries.</param>
    /// <param name="CanRunInParallel">Whether this workload may run alongside other workloads in the same stage.</param>
    internal sealed record InstallerExecutionPlan(
        IInstallSource Installer,
        string Name,
        int Count,
        int Stage,
        bool CanRunInParallel);

    /// <summary>
    /// Gets a flat queue of all individual item display names in processing order.
    /// This matches the order that installers process items, so names can be
    /// dequeued in the <c>onItemComplete</c> callback to show per-item progress.
    /// </summary>
    /// <param name="installBlock">The install block to extract item names from.</param>
    /// <returns>A queue of display names in installation processing order.</returns>
    internal static Queue<string> GetAllItemNames(InstallBlock installBlock)
    {
        ArgumentNullException.ThrowIfNull(installBlock);

        var names = new Queue<string>();

        // GitHub releases
        foreach (var item in installBlock.Github)
        {
            names.Enqueue(item.Binary ?? item.Repo);
        }

        // APT packages
        foreach (var item in installBlock.Apt)
        {
            names.Enqueue(item);
        }

        // APT repositories (each repo is one item)
        foreach (var item in installBlock.AptRepos)
        {
            names.Enqueue(item.Name);
        }

        // Scripts
        foreach (var item in installBlock.Scripts)
        {
            names.Enqueue(item);
        }

        // Fonts
        foreach (var item in installBlock.Fonts)
        {
            names.Enqueue(item.Name);
        }

        // Snap packages
        foreach (var item in installBlock.Snaps)
        {
            names.Enqueue(item.Name);
        }

        return names;
    }
}
