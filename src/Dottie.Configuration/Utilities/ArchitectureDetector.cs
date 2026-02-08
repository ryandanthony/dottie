// -----------------------------------------------------------------------
// <copyright file="ArchitectureDetector.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Dottie.Configuration.Utilities;

/// <summary>
/// Detects and matches system architecture for asset selection.
/// </summary>
public static class ArchitectureDetector
{
    /// <summary>
    /// Gets the current system architecture as a Microsoft/Debian-style standardized string.
    /// Maps to the <c>${MS_ARCH}</c> variable.
    /// </summary>
    /// <value>
    /// The current system architecture as a standardized string (e.g., <c>amd64</c>, <c>arm64</c>, <c>armhf</c>).
    /// </value>
    public static string CurrentArchitecture => RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "amd64",
        Architecture.Arm64 => "arm64",
        Architecture.X86 => "x86",
        Architecture.Arm => "armhf",
        _ => "unknown",
    };

    /// <summary>
    /// Gets the raw system architecture matching <c>uname -m</c> output.
    /// Maps to the <c>${ARCH}</c> variable.
    /// </summary>
    /// <value>
    /// The raw architecture string (e.g., <c>x86_64</c>, <c>aarch64</c>, <c>armv7l</c>).
    /// </value>
    public static string RawArchitecture => RuntimeInformation.OSArchitecture switch
    {
        Architecture.X64 => "x86_64",
        Architecture.Arm64 => "aarch64",
        Architecture.Arm => "armv7l",
        _ => "unknown",
    };

    /// <summary>
    /// Gets glob patterns that match assets for the current architecture.
    /// </summary>
    /// <value>
    /// Glob patterns that match assets for the current architecture.
    /// </value>
    public static IReadOnlyList<string> CurrentArchitecturePatterns => CurrentArchitecture switch
    {
        "amd64" => ["*amd64*", "*x86_64*", "*x64*"],
        "arm64" => ["*arm64*", "*aarch64*"],
        "x86" => ["*i386*", "*i686*", "*x86*"],
        "armhf" => ["*armv7*", "*armhf*"],
        _ => ["*"],
    };

    /// <summary>
    /// Checks if a filename matches a glob pattern.
    /// </summary>
    /// <param name="filename">The filename to check.</param>
    /// <param name="pattern">The glob pattern (supports * wildcard).</param>
    /// <returns>True if the filename matches the pattern.</returns>
    public static bool MatchesPattern(string filename, string pattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filename);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        // Convert glob pattern to regex
        // Escape special regex characters except *
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal) + "$";

        return Regex.IsMatch(filename, regexPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }
}
