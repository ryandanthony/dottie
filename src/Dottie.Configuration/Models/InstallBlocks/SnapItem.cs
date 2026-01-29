// -----------------------------------------------------------------------
// <copyright file="SnapItem.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Models.InstallBlocks;

/// <summary>
/// Specification for a snap package to install.
/// </summary>
public sealed record SnapItem
{
    /// <summary>
    /// Gets the name of the snap package.
    /// </summary>
    /// <value>
    /// The name of the snap package.
    /// </value>
    public required string Name { get; init; }

    /// <summary>
    /// Gets a value indicating whether to install with --classic confinement.
    /// </summary>
    /// <value>
    /// A value indicating whether to install with --classic confinement.
    /// </value>
    public bool Classic { get; init; }

    /// <summary>
    /// Gets the unique identifier for merging during profile inheritance.
    /// </summary>
    internal string MergeKey => Name;
}
