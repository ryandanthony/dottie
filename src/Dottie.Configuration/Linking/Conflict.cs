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
    public required DotfileEntry Entry { get; init; }

    /// <summary>
    /// Gets the expanded target path where the conflict was detected.
    /// </summary>
    /// <remarks>
    /// This is the fully expanded path (e.g., ~ expanded to /home/user).
    /// </remarks>
    public required string TargetPath { get; init; }

    /// <summary>
    /// Gets the type of conflict detected.
    /// </summary>
    public required ConflictType Type { get; init; }

    /// <summary>
    /// Gets the existing symlink target, if the conflict is a mismatched symlink.
    /// </summary>
    /// <remarks>
    /// Only populated when <see cref="Type"/> is <see cref="ConflictType.MismatchedSymlink"/>.
    /// </remarks>
    public string? ExistingTarget { get; init; }
}
