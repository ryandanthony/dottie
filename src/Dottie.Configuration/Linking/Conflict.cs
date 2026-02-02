// -----------------------------------------------------------------------
// <copyright file="Conflict.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration.Linking;

/// <summary>
/// Represents a conflict detected at a target path during dotfile linking.
/// </summary>
public sealed record Conflict
{
    /// <summary>
    /// Gets the dotfile entry that caused the conflict.
    /// </summary>
    /// <value>
    /// <placeholder>The dotfile entry that caused the conflict.</placeholder>
    /// </value>
    public required DotfileEntry Entry { get; init; }

    /// <summary>
    /// Gets the expanded target path where the conflict was detected.
    /// </summary>
    /// <remarks>
    /// This is the fully expanded path (e.g., ~ expanded to /home/user).
    /// </remarks>
    /// <value>
    /// <placeholder>The expanded target path where the conflict was detected.</placeholder>
    /// </value>
    public required string TargetPath { get; init; }

    /// <summary>
    /// Gets the type of conflict detected.
    /// </summary>
    /// <value>
    /// <placeholder>The type of conflict detected.</placeholder>
    /// </value>
    public required ConflictType Type { get; init; }

    /// <summary>
    /// Gets the existing symlink target, if the conflict is a mismatched symlink.
    /// </summary>
    /// <remarks>
    /// Only populated when <see cref="Type"/> is <see cref="ConflictType.MismatchedSymlink"/>.
    /// </remarks>
    /// <value>
    /// <placeholder>The existing symlink target, if the conflict is a mismatched symlink.</placeholder>
    /// </value>
    public string? ExistingTarget { get; init; }
}
