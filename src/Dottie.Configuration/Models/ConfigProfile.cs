// -----------------------------------------------------------------------
// <copyright file="ConfigProfile.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Models;

/// <summary>
/// A named configuration set containing dotfile mappings and install specifications.
/// May extend another profile via inheritance.
/// </summary>
public sealed record ConfigProfile
{
    /// <summary>
    /// Gets the name of another profile to inherit from.
    /// When set, this profile's settings are merged with the parent.
    /// </summary>
    /// <value>
    /// The name of another profile to inherit from.
    /// When set, this profile's settings are merged with the parent.
    /// </value>
    public string? Extends { get; init; }

    /// <summary>
    /// Gets the list of dotfile source-to-target mappings.
    /// </summary>
    /// <value>
    /// The list of dotfile source-to-target mappings.
    /// </value>
    public IList<DotfileEntry> Dotfiles { get; init; } = [];

    /// <summary>
    /// Gets the software installation specifications organized by install method.
    /// </summary>
    /// <value>
    /// The software installation specifications organized by install method.
    /// </value>
    public InstallBlock? Install { get; init; }
}
