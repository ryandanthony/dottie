// -----------------------------------------------------------------------
// <copyright file="GithubReleaseAssetType.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Models.InstallBlocks;

/// <summary>
/// Controls the installation pathway for a downloaded GitHub release asset.
/// </summary>
public enum GithubReleaseAssetType
{
    /// <summary>
    /// Extract or copy binary to ~/bin/, chmod +x. This is the default behavior.
    /// </summary>
    Binary = 0,

    /// <summary>
    /// Install .deb package via dpkg -i with automatic dependency resolution via apt-get install -f.
    /// </summary>
    Deb = 1,
}
