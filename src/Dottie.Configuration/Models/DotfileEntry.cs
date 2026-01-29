// -----------------------------------------------------------------------
// <copyright file="DotfileEntry.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Models;

/// <summary>
/// A mapping from a source path (in the repository) to a target path (on the filesystem).
/// </summary>
public sealed record DotfileEntry
{
    /// <summary>
    /// Gets the path to the source file or directory, relative to repository root.
    /// </summary>
    /// <value>
    /// The path to the source file or directory, relative to repository root.
    /// </value>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the destination path where the dotfile should be linked.
    /// Supports tilde (~) expansion for home directory.
    /// </summary>
    /// <value>
    /// The destination path where the dotfile should be linked.
    /// Supports tilde (~) expansion for home directory.
    /// </value>
    public required string Target { get; init; }
}
