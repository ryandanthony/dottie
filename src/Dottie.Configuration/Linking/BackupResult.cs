// -----------------------------------------------------------------------
// <copyright file="BackupResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Linking;

/// <summary>
/// The result of a backup operation.
/// </summary>
public sealed record BackupResult
{
    /// <summary>
    /// Gets a value indicating whether the backup succeeded.
    /// </summary>
    /// <value>
    /// <placeholder>A value indicating whether the backup succeeded.</placeholder>
    /// </value>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the original path that was backed up.
    /// </summary>
    /// <value>
    /// <placeholder>The original path that was backed up.</placeholder>
    /// </value>
    public required string OriginalPath { get; init; }

    /// <summary>
    /// Gets the backup path where the original was moved.
    /// </summary>
    /// <remarks>
    /// Null if the backup failed.
    /// </remarks>
    /// <value>
    /// <placeholder>The backup path where the original was moved.</placeholder>
    /// </value>
    public string? BackupPath { get; init; }

    /// <summary>
    /// Gets the error message if the backup failed.
    /// </summary>
    /// <value>
    /// <placeholder>The error message if the backup failed.</placeholder>
    /// </value>
    public string? Error { get; init; }

    /// <summary>
    /// Gets the timestamp when the backup was created.
    /// </summary>
    /// <value>
    /// <placeholder>The timestamp when the backup was created.</placeholder>
    /// </value>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Creates a successful backup result.
    /// </summary>
    /// <param name="originalPath">The original path that was backed up.</param>
    /// <param name="backupPath">The path where the backup was created.</param>
    /// <param name="timestamp">The timestamp of the backup.</param>
    /// <returns>A successful backup result.</returns>
    public static BackupResult Success(string originalPath, string backupPath, DateTimeOffset timestamp) =>
        new()
        {
            IsSuccess = true,
            OriginalPath = originalPath,
            BackupPath = backupPath,
            Timestamp = timestamp,
        };

    /// <summary>
    /// Creates a failed backup result.
    /// </summary>
    /// <param name="originalPath">The original path that failed to back up.</param>
    /// <param name="error">The error message.</param>
    /// <param name="timeProvider">The time provider for testable time.</param>
    /// <returns>A failed backup result.</returns>
    public static BackupResult Failure(string originalPath, string error, TimeProvider? timeProvider = null) =>
        new()
        {
            IsSuccess = false,
            OriginalPath = originalPath,
            Error = error,
            Timestamp = (timeProvider ?? TimeProvider.System).GetUtcNow(),
        };
}
