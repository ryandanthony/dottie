// -----------------------------------------------------------------------
// <copyright file="BackupService.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;

namespace Dottie.Configuration.Linking;

/// <summary>
/// Creates backups of files and directories.
/// </summary>
public sealed class BackupService
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class.
    /// </summary>
    /// <param name="timeProvider">Optional time provider for testable time. Uses system time if not provided.</param>
    public BackupService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Creates a backup of the specified path.
    /// </summary>
    /// <param name="path">The path to backup.</param>
    /// <returns>The backup result.</returns>
    public BackupResult Backup(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            if (!Path.Exists(path))
            {
                return BackupResult.Failure(path, $"Path does not exist: {path}", _timeProvider);
            }

            var timestamp = _timeProvider.GetUtcNow();
            var backupPath = GenerateBackupPath(path, timestamp);

            if (Directory.Exists(path) && !new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                // It's a real directory, not a symlink
                Directory.Move(path, backupPath);
            }
            else
            {
                // It's a file or symlink
                File.Move(path, backupPath);
            }

            return BackupResult.Success(path, backupPath, timestamp);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return BackupResult.Failure(path, ex.Message, _timeProvider);
        }
    }

    private static string GenerateBackupPath(string originalPath, DateTimeOffset timestamp)
    {
        // Format: yyyyMMdd-HHmmss (e.g., 20260130-143022)
        // Per spec FR-022: Use .dottie-backup-YYYYMMDD-HHMMSS naming convention
        var year = timestamp.Year.ToString("D4", CultureInfo.InvariantCulture);
        var month = timestamp.Month.ToString("D2", CultureInfo.InvariantCulture);
        var day = timestamp.Day.ToString("D2", CultureInfo.InvariantCulture);
        var hour = timestamp.Hour.ToString("D2", CultureInfo.InvariantCulture);
        var minute = timestamp.Minute.ToString("D2", CultureInfo.InvariantCulture);
        var second = timestamp.Second.ToString("D2", CultureInfo.InvariantCulture);
        var timestampStr = $"{year}{month}{day}-{hour}{minute}{second}";
        var basePath = $"{originalPath}.dottie-backup-{timestampStr}";

        if (!Path.Exists(basePath))
        {
            return basePath;
        }

        // Add numeric suffix if collision
        var counter = 1;
        string numberedPath;
        do
        {
            numberedPath = $"{basePath}.{counter}";
            counter++;
        }
        while (Path.Exists(numberedPath));

        return numberedPath;
    }
}
