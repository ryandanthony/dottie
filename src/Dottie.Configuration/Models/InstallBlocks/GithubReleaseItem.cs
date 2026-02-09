// -----------------------------------------------------------------------
// <copyright file="GithubReleaseItem.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Models.InstallBlocks;

/// <summary>
/// Specification for downloading a binary from a GitHub release.
/// </summary>
public sealed record GithubReleaseItem
{
    /// <summary>
    /// Gets the repository in owner/name format (e.g., "junegunn/fzf").
    /// </summary>
    /// <value>
    /// The repository in owner/name format (e.g., "junegunn/fzf").
    /// </value>
    public required string Repo { get; init; }

    /// <summary>
    /// Gets the glob pattern to match release asset filename (e.g., "fzf-*-linux_amd64.tar.gz").
    /// </summary>
    /// <value>
    /// The glob pattern to match release asset filename (e.g., "fzf-*-linux_amd64.tar.gz").
    /// </value>
    public required string Asset { get; init; }

    /// <summary>
    /// Gets the name of the binary to extract from the archive.
    /// Required when <see cref="Type"/> is <see cref="GithubReleaseAssetType.Binary"/>; optional otherwise.
    /// </summary>
    /// <value>
    /// The name of the binary to extract from the archive.
    /// </value>
    public string? Binary { get; init; }

    /// <summary>
    /// Gets the installation pathway for the downloaded asset.
    /// Defaults to <see cref="GithubReleaseAssetType.Binary"/> for backward compatibility.
    /// </summary>
    /// <value>
    /// The asset type controlling the installation method.
    /// </value>
    public GithubReleaseAssetType Type { get; init; } = GithubReleaseAssetType.Binary;

    /// <summary>
    /// Gets the specific version tag to download. Defaults to latest release if not specified.
    /// </summary>
    /// <value>
    /// The specific version tag to download. Defaults to latest release if not specified.
    /// </value>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the unique identifier for merging during profile inheritance.
    /// </summary>
    internal string MergeKey => Repo;
}
