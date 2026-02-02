// -----------------------------------------------------------------------
// <copyright file="LinkExecutionResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Linking;

/// <summary>
/// The result of executing a link operation.
/// </summary>
public sealed record LinkExecutionResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was blocked due to conflicts.
    /// </summary>
    /// <value>
    /// <placeholder>A value indicating whether the operation was blocked due to conflicts.</placeholder>
    /// </value>
    public bool IsBlocked { get; init; }

    /// <summary>
    /// Gets the conflict result if the operation was blocked.
    /// </summary>
    /// <value>
    /// <placeholder>The conflict result if the operation was blocked.</placeholder>
    /// </value>
    public ConflictResult? ConflictResult { get; init; }

    /// <summary>
    /// Gets the link operation result if the operation completed.
    /// </summary>
    /// <value>
    /// <placeholder>The link operation result if the operation completed.</placeholder>
    /// </value>
    public LinkOperationResult? LinkResult { get; init; }

    /// <summary>
    /// Gets the backup results from the operation.
    /// </summary>
    /// <value>
    /// <placeholder>The backup results from the operation.</placeholder>
    /// </value>
    public IReadOnlyList<BackupResult> BackupResults { get; init; } = [];

    /// <summary>
    /// Creates a blocked result due to conflicts.
    /// </summary>
    /// <param name="conflictResult">The conflict result.</param>
    /// <returns>A blocked link execution result.</returns>
    public static LinkExecutionResult Blocked(ConflictResult conflictResult) =>
        new()
        {
            IsBlocked = true,
            ConflictResult = conflictResult,
        };

    /// <summary>
    /// Creates a completed result.
    /// </summary>
    /// <param name="linkResult">The link operation result.</param>
    /// <param name="backupResults">The backup results.</param>
    /// <returns>A completed link execution result.</returns>
    public static LinkExecutionResult Completed(
        LinkOperationResult linkResult,
        IReadOnlyList<BackupResult> backupResults) =>
        new()
        {
            IsBlocked = false,
            LinkResult = linkResult,
            BackupResults = backupResults,
        };
}
