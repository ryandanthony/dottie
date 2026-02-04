// -----------------------------------------------------------------------
// <copyright file="DotfileStatusChecker.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration.Status;

/// <summary>
/// Service that checks the status of dotfile links.
/// </summary>
public sealed class DotfileStatusChecker
{
    /// <summary>
    /// Checks the status of all dotfile entries.
    /// </summary>
    /// <param name="dotfiles">The dotfile entries to check.</param>
    /// <param name="repoRoot">The repository root path.</param>
    /// <returns>A list of status entries for each dotfile.</returns>
    public IReadOnlyList<DotfileStatusEntry> CheckStatus(IReadOnlyList<DotfileEntry> dotfiles, string repoRoot)
    {
        ArgumentNullException.ThrowIfNull(dotfiles);
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);

        var results = new List<DotfileStatusEntry>(dotfiles.Count);

        foreach (var entry in dotfiles)
        {
            var statusEntry = CheckSingleEntry(entry, repoRoot);
            results.Add(statusEntry);
        }

        return results;
    }

    private static DotfileStatusEntry CheckSingleEntry(DotfileEntry entry, string repoRoot)
    {
        var expandedTarget = ExpandPath(entry.Target);
        var sourcePath = Path.Combine(repoRoot, entry.Source);
        var normalizedSourcePath = Path.GetFullPath(sourcePath);

        try
        {
            return DetermineState(entry, expandedTarget, normalizedSourcePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new DotfileStatusEntry(entry, DotfileLinkState.Unknown, $"Permission denied: {ex.Message}", expandedTarget);
        }
        catch (IOException ex)
        {
            return new DotfileStatusEntry(entry, DotfileLinkState.Unknown, $"IO error: {ex.Message}", expandedTarget);
        }
    }

    private static DotfileStatusEntry DetermineState(DotfileEntry entry, string expandedTarget, string expectedSourcePath)
    {
        // Check if target path exists at all
        if (!Path.Exists(expandedTarget))
        {
            return new DotfileStatusEntry(entry, DotfileLinkState.Missing, null, expandedTarget);
        }

        var fileInfo = new FileInfo(expandedTarget);
        var dirInfo = new DirectoryInfo(expandedTarget);

        // Check if it's a symlink (file or directory)
        if (fileInfo.Exists && fileInfo.LinkTarget != null)
        {
            return CheckSymlinkState(entry, expandedTarget, expectedSourcePath, fileInfo.LinkTarget);
        }

        if (dirInfo.Exists && dirInfo.LinkTarget != null)
        {
            return CheckSymlinkState(entry, expandedTarget, expectedSourcePath, dirInfo.LinkTarget);
        }

        // It's a regular file or directory - conflict
        if (Directory.Exists(expandedTarget))
        {
            return new DotfileStatusEntry(entry, DotfileLinkState.Conflicting, "Existing directory blocks target", expandedTarget);
        }

        return new DotfileStatusEntry(entry, DotfileLinkState.Conflicting, "Existing file blocks target", expandedTarget);
    }

    private static DotfileStatusEntry CheckSymlinkState(DotfileEntry entry, string expandedTarget, string expectedSourcePath, string linkTarget)
    {
        // Check if the symlink target exists
        var resolvedTarget = Path.GetFullPath(linkTarget, Path.GetDirectoryName(expandedTarget)!);

        if (!Path.Exists(resolvedTarget))
        {
            return new DotfileStatusEntry(entry, DotfileLinkState.Broken, $"Symlink target does not exist: {linkTarget}", expandedTarget);
        }

        // Check if it points to the expected source
        if (string.Equals(resolvedTarget, expectedSourcePath, StringComparison.Ordinal))
        {
            return new DotfileStatusEntry(entry, DotfileLinkState.Linked, null, expandedTarget);
        }

        // Symlink exists but points to wrong location
        return new DotfileStatusEntry(entry, DotfileLinkState.Conflicting, $"Symlink points to wrong target: {linkTarget}", expandedTarget);
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
