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
#pragma warning disable SA1316 // Tuple element names should use correct casing
    internal static (IInstallSource Installer, string Name, int Count)[] GetInstallerItems(InstallBlock installBlock)
#pragma warning restore SA1316 // Tuple element names should use correct casing
    {
        ArgumentNullException.ThrowIfNull(installBlock);

        return
        [
            (Installer: (IInstallSource)new GithubReleaseInstaller(), Name: "GitHub releases", Count: installBlock.Github.Count),
            (Installer: (IInstallSource)new AptPackageInstaller(), Name: "APT packages", Count: installBlock.Apt.Count),
            (Installer: (IInstallSource)new AptRepoInstaller(), Name: "APT repositories", Count: installBlock.AptRepos.Count),
            (Installer: (IInstallSource)new ScriptRunner(), Name: "Scripts", Count: installBlock.Scripts.Count),
            (Installer: (IInstallSource)new FontInstaller(), Name: "Fonts", Count: installBlock.Fonts.Count),
            (Installer: (IInstallSource)new SnapPackageInstaller(), Name: "Snap packages", Count: installBlock.Snaps.Count),
        ];
    }

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
