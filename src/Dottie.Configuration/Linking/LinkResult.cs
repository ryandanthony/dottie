// -----------------------------------------------------------------------
// <copyright file="LinkResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration.Linking;

/// <summary>
/// The result of linking a single dotfile entry.
/// </summary>
public sealed record LinkResult
{
    /// <summary>
    /// Gets a value indicating whether the link operation succeeded.
    /// </summary>
    /// <value>
    /// <placeholder>A value indicating whether the link operation succeeded.</placeholder>
    /// </value>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the dotfile entry that was linked.
    /// </summary>
    /// <value>
    /// <placeholder>The dotfile entry that was linked.</placeholder>
    /// </value>
    public required DotfileEntry Entry { get; init; }

    /// <summary>
    /// Gets the expanded target path where the symlink was created.
    /// </summary>
    /// <value>
    /// <placeholder>The expanded target path where the symlink was created.</placeholder>
    /// </value>
    public required string ExpandedTargetPath { get; init; }

    /// <summary>
    /// Gets the backup result if a conflict was resolved.
    /// </summary>
    /// <remarks>
    /// Only populated when --force was used and a conflict existed.
    /// </remarks>
    /// <value>
    /// <placeholder>The backup result if a conflict was resolved.</placeholder>
    /// </value>
    public BackupResult? BackupResult { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    /// <value>
    /// <placeholder>The error message if the operation failed.</placeholder>
    /// </value>
    public string? Error { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entry was skipped because it was already correctly linked.
    /// </summary>
    /// <value>
    /// <placeholder>A value indicating whether this entry was skipped because it was already correctly linked.</placeholder>
    /// </value>
    public bool WasSkipped { get; init; }

    /// <summary>
    /// Creates a successful link result.
    /// </summary>
    /// <param name="entry">The dotfile entry that was linked.</param>
    /// <param name="expandedTargetPath">The expanded target path.</param>
    /// <param name="backupResult">Optional backup result if a conflict was resolved.</param>
    /// <returns>A successful link result.</returns>
    public static LinkResult Success(
        DotfileEntry entry,
        string expandedTargetPath,
        BackupResult? backupResult = null) =>
        new()
        {
            IsSuccess = true,
            Entry = entry,
            ExpandedTargetPath = expandedTargetPath,
            BackupResult = backupResult,
        };

    /// <summary>
    /// Creates a skipped result (already linked).
    /// </summary>
    /// <param name="entry">The dotfile entry that was skipped.</param>
    /// <param name="expandedTargetPath">The expanded target path.</param>
    /// <returns>A skipped link result.</returns>
    public static LinkResult Skipped(DotfileEntry entry, string expandedTargetPath) =>
        new()
        {
            IsSuccess = true,
            Entry = entry,
            ExpandedTargetPath = expandedTargetPath,
            WasSkipped = true,
        };

    /// <summary>
    /// Creates a failed link result.
    /// </summary>
    /// <param name="entry">The dotfile entry that failed to link.</param>
    /// <param name="expandedTargetPath">The expanded target path.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A failed link result.</returns>
    public static LinkResult Failure(DotfileEntry entry, string expandedTargetPath, string error) =>
        new()
        {
            IsSuccess = false,
            Entry = entry,
            ExpandedTargetPath = expandedTargetPath,
            Error = error,
        };
}
