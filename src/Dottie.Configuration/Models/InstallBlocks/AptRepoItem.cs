// -----------------------------------------------------------------------
// <copyright file="AptRepoItem.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using YamlDotNet.Serialization;

namespace Dottie.Configuration.Models.InstallBlocks;

/// <summary>
/// Specification for adding a third-party apt repository.
/// </summary>
public sealed record AptRepoItem
{
    /// <summary>
    /// Gets the identifier name for this repository configuration.
    /// </summary>
    /// <value>
    /// The identifier name for this repository configuration.
    /// </value>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the URL to the GPG key for repository verification.
    /// </summary>
    /// <value>
    /// The URL to the GPG key for repository verification.
    /// </value>
    [YamlMember(Alias = "key_url", ApplyNamingConventions = false)]
    public required string KeyUrl { get; init; }

    /// <summary>
    /// Gets the apt repository line (e.g., "deb [arch=amd64] https://... stable main").
    /// </summary>
    /// <value>
    /// The apt repository line (e.g., "deb [arch=amd64] https://... stable main").
    /// </value>
    public required string Repo { get; init; }

    /// <summary>
    /// Gets the packages to install from this repository.
    /// </summary>
    /// <value>
    /// The packages to install from this repository.
    /// </value>
    public required IList<string> Packages { get; init; }

    /// <summary>
    /// Gets the unique identifier for merging during profile inheritance.
    /// </summary>
    internal string MergeKey => Name;
}
