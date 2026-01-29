// -----------------------------------------------------------------------
// <copyright file="FontItem.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Models.InstallBlocks;

/// <summary>
/// Specification for downloading and installing a font.
/// </summary>
public sealed record FontItem
{
    /// <summary>
    /// Gets the display name of the font.
    /// </summary>
    /// <value>
    /// The display name of the font.
    /// </value>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the URL to download the font archive (zip file).
    /// </summary>
    /// <value>
    /// The URL to download the font archive (zip file).
    /// </value>
    public required string Url { get; init; }

    /// <summary>
    /// Gets the unique identifier for merging during profile inheritance.
    /// </summary>
    internal string MergeKey => Name;
}
