// -----------------------------------------------------------------------
// <copyright file="RepoRootFinder.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Cli.Utilities;

/// <summary>
/// Finds the root of a git repository by searching for .git directory.
/// </summary>
public static class RepoRootFinder
{
    /// <summary>
    /// Finds the repository root by searching for a .git directory.
    /// </summary>
    /// <param name="startPath">The directory to start searching from (defaults to current directory).</param>
    /// <returns>The repository root path, or null if not found.</returns>
    public static string? Find(string? startPath = null)
    {
        var currentDir = startPath ?? Directory.GetCurrentDirectory();

        while (currentDir is not null)
        {
            var gitDir = Path.Combine(currentDir, ".git");
            if (Directory.Exists(gitDir) || File.Exists(gitDir))
            {
                return currentDir;
            }

            var parent = Directory.GetParent(currentDir);
            currentDir = parent?.FullName;
        }

        return null;
    }
}
