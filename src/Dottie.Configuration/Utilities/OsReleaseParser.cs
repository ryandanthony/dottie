// -----------------------------------------------------------------------
// <copyright file="OsReleaseParser.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Utilities;

/// <summary>
/// Parses <c>/etc/os-release</c> content into a variable dictionary.
/// Follows the freedesktop.org os-release specification.
/// </summary>
public static class OsReleaseParser
{
    private const string DefaultOsReleasePath = "/etc/os-release";

    /// <summary>
    /// Parses the content of an os-release file into key-value pairs.
    /// </summary>
    /// <param name="content">The raw text content of the os-release file.</param>
    /// <returns>A read-only dictionary of key-value pairs parsed from the content.</returns>
    public static IReadOnlyDictionary<string, string> Parse(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(content))
        {
            return result;
        }

        var lines = content.Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
            {
                continue;
            }

            // Split on first '=' only
            var equalsIndex = line.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex < 0)
            {
                continue;
            }

            var key = line[..equalsIndex];
            var value = line[(equalsIndex + 1)..];

            // Strip surrounding quotes (double or single)
            value = StripQuotes(value);

            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Attempts to read and parse the os-release file from the filesystem.
    /// </summary>
    /// <param name="filePath">The path to the os-release file. Defaults to <c>/etc/os-release</c>.</param>
    /// <returns>
    /// A tuple containing the parsed variables and a boolean indicating whether the file was successfully read.
    /// </returns>
    public static (IReadOnlyDictionary<string, string> variables, bool isAvailable) TryReadFromSystem(string filePath = DefaultOsReleasePath)
    {
        if (!File.Exists(filePath))
        {
            return (new Dictionary<string, string>(StringComparer.Ordinal), false);
        }

        var content = File.ReadAllText(filePath);
        var variables = Parse(content);
        return (variables, true);
    }

    private static string StripQuotes(string value)
    {
        if (value.Length < 2)
        {
            return value;
        }

        var isDoubleQuoted = value.StartsWith('"') && value.EndsWith('"');
        var isSingleQuoted = value.StartsWith('\'') && value.EndsWith('\'');

        return isDoubleQuoted || isSingleQuoted ? value[1..^1] : value;
    }
}
