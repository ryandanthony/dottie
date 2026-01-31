// -----------------------------------------------------------------------
// <copyright file="ConflictDetector.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration.Linking;

/// <summary>
/// Detects conflicts for dotfile linking operations.
/// </summary>
public sealed class ConflictDetector
{
    /// <summary>
    /// Detects conflicts for the given dotfile entries.
    /// </summary>
    /// <param name="dotfiles">The dotfile entries to check.</param>
    /// <param name="repoRoot">The repository root path.</param>
    /// <returns>The conflict detection result.</returns>
    public ConflictResult DetectConflicts(IReadOnlyList<DotfileEntry> dotfiles, string repoRoot)
    {
        ArgumentNullException.ThrowIfNull(dotfiles);
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);

        var conflicts = new List<Conflict>();
        var safeEntries = new List<DotfileEntry>();
        var alreadyLinked = new List<DotfileEntry>();

        foreach (var entry in dotfiles)
        {
            var targetPath = ExpandPath(entry.Target);
            var sourcePath = Path.Combine(repoRoot, entry.Source);
            var normalizedSourcePath = Path.GetFullPath(sourcePath);

            var conflictType = DetectConflictType(targetPath, normalizedSourcePath, out var existingTarget);

            switch (conflictType)
            {
                case ConflictType.None:
                {
                    if (IsSymlink(targetPath))
                    {
                        alreadyLinked.Add(entry);
                    }
                    else
                    {
                        safeEntries.Add(entry);
                    }

                    break;
                }

                case ConflictType.File:
                case ConflictType.Directory:
                case ConflictType.MismatchedSymlink:
                {
                    conflicts.Add(new Conflict
                    {
                        Entry = entry,
                        TargetPath = targetPath,
                        Type = conflictType,
                        ExistingTarget = existingTarget,
                    });
                    break;
                }

                default:
                    throw new InvalidOperationException($"Unexpected conflict type: {conflictType}");
            }
        }

        return conflicts.Count > 0
            ? ConflictResult.WithConflicts(conflicts, safeEntries, alreadyLinked)
            : ConflictResult.NoConflicts(safeEntries, alreadyLinked);
    }

    private static ConflictType DetectConflictType(string targetPath, string expectedSourcePath, out string? existingTarget)
    {
        existingTarget = null;

        if (!Path.Exists(targetPath))
        {
            return ConflictType.None;
        }

        var fileInfo = new FileInfo(targetPath);
        var dirInfo = new DirectoryInfo(targetPath);

        // Check if it's a symlink
        if (fileInfo.Exists && fileInfo.LinkTarget != null)
        {
            var actualTarget = Path.GetFullPath(fileInfo.LinkTarget, Path.GetDirectoryName(targetPath)!);
            if (string.Equals(actualTarget, expectedSourcePath, StringComparison.Ordinal))
            {
                return ConflictType.None; // Already correctly linked
            }

            existingTarget = fileInfo.LinkTarget;
            return ConflictType.MismatchedSymlink;
        }

        if (dirInfo.Exists && dirInfo.LinkTarget != null)
        {
            var actualTarget = Path.GetFullPath(dirInfo.LinkTarget, Path.GetDirectoryName(targetPath)!);
            if (string.Equals(actualTarget, expectedSourcePath, StringComparison.Ordinal))
            {
                return ConflictType.None; // Already correctly linked
            }

            existingTarget = dirInfo.LinkTarget;
            return ConflictType.MismatchedSymlink;
        }

        // Regular file or directory
        if (Directory.Exists(targetPath))
        {
            return ConflictType.Directory;
        }

        if (File.Exists(targetPath))
        {
            return ConflictType.File;
        }

        return ConflictType.None;
    }

    private static bool IsSymlink(string path)
    {
        if (!Path.Exists(path))
        {
            return false;
        }

        var fileInfo = new FileInfo(path);
        var dirInfo = new DirectoryInfo(path);

        return (fileInfo.Exists && fileInfo.LinkTarget != null) ||
               (dirInfo.Exists && dirInfo.LinkTarget != null);
    }

    private static string ExpandPath(string path)
    {
        if (path.StartsWith('~'))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path[1..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        return Path.GetFullPath(path);
    }
}
