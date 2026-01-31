// -----------------------------------------------------------------------
// <copyright file="ProfileInfo.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration;

/// <summary>
/// Summary information about a profile for display purposes.
/// </summary>
public sealed record ProfileInfo
{
    /// <summary>
    /// Gets the profile name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the name of the profile this extends, if any.
    /// </summary>
    public string? Extends { get; init; }

    /// <summary>
    /// Gets the count of dotfile entries defined directly in this profile.
    /// </summary>
    public int DotfileCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether this profile has an install block.
    /// </summary>
    public bool HasInstallBlock { get; init; }
}
