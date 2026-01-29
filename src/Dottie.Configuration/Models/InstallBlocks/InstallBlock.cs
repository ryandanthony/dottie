// -----------------------------------------------------------------------
// <copyright file="InstallBlock.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using YamlDotNet.Serialization;

namespace Dottie.Configuration.Models.InstallBlocks;

/// <summary>
/// Categorized collection of software to install, organized by installation method.
/// </summary>
public sealed record InstallBlock
{
    /// <summary>
    /// Gets the binaries to download from GitHub releases.
    /// </summary>
    /// <value>The binaries to download from GitHub releases.</value>
    public IList<GithubReleaseItem> Github { get; init; } = [];

    /// <summary>
    /// Gets the package names to install via apt.
    /// </summary>
    /// <value>The package names to install via apt.</value>
    public IList<string> Apt { get; init; } = [];

    /// <summary>
    /// Gets the third-party apt repositories to add before installing packages.
    /// </summary>
    /// <value>The third-party apt repositories to add before installing packages.</value>
    [YamlMember(Alias = "apt-repos")]
    public IList<AptRepoItem> AptRepos { get; init; } = [];

    /// <summary>
    /// Gets the script paths (relative to repo root) to execute.
    /// </summary>
    /// <value>The script paths (relative to repo root) to execute.</value>
    public IList<string> Scripts { get; init; } = [];

    /// <summary>
    /// Gets the fonts to download and install.
    /// </summary>
    /// <value>The fonts to download and install.</value>
    public IList<FontItem> Fonts { get; init; } = [];

    /// <summary>
    /// Gets the snap packages to install.
    /// </summary>
    /// <value>The snap packages to install.</value>
    [YamlMember(Alias = "snap")]
    public IList<SnapItem> Snaps { get; init; } = [];
}
