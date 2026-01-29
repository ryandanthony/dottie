// -----------------------------------------------------------------------
// <copyright file="PathExpander.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Utilities;

/// <summary>
/// Expands paths containing ~ to the user's home directory.
/// </summary>
public static class PathExpander
{
    /// <summary>
    /// The index where the path portion starts after the tilde prefix (e.g., "~/").
    /// </summary>
    private const int TildePrefixLength = 2;

    /// <summary>
    /// Expands a path, replacing ~ with the user's home directory.
    /// </summary>
    /// <param name="path">The path to expand.</param>
    /// <returns>The expanded path.</returns>
    public static string Expand(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!path.StartsWith('~'))
        {
            return path;
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.Equals(path, "~", StringComparison.Ordinal))
        {
            return homeDir;
        }

        if (path.StartsWith("~/", StringComparison.Ordinal) || path.StartsWith("~\\", StringComparison.Ordinal))
        {
            return Path.Combine(homeDir, path[TildePrefixLength..]);
        }

        // ~username syntax is not supported, return unchanged
        return path;
    }
}
