// -----------------------------------------------------------------------
// <copyright file="ResolvedProfile.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;
using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Inheritance;

/// <summary>
/// Represents a fully resolved profile with all inheritance merged.
/// </summary>
public sealed record ResolvedProfile
{
    /// <summary>
    /// Gets the name of the profile.
    /// </summary>
    /// <value>
    /// The name of the profile.
    /// </value>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the merged list of dotfile source-to-target mappings.
    /// Parent dotfiles appear before child dotfiles.
    /// </summary>
    /// <value>
    /// The merged list of dotfile source-to-target mappings.
    /// Parent dotfiles appear before child dotfiles.
    /// </value>
    public IList<DotfileEntry> Dotfiles { get; init; } = [];

    /// <summary>
    /// Gets the merged software installation specifications.
    /// </summary>
    /// <value>
    /// The merged software installation specifications.
    /// </value>
    public InstallBlock? Install { get; init; }

    /// <summary>
    /// Gets the chain of profile names that were merged (for debugging).
    /// </summary>
    /// <value>
    /// The chain of profile names that were merged (for debugging).
    /// </value>
    public IReadOnlyList<string> InheritanceChain { get; init; } = [];
}
