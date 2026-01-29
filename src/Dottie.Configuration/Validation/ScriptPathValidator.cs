// -----------------------------------------------------------------------
// <copyright file="ScriptPathValidator.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Validation;

/// <summary>
/// Validates script paths for security issues.
/// </summary>
public class ScriptPathValidator
{
    /// <summary>
    /// Validates a script path to ensure it is safe.
    /// </summary>
    /// <param name="scriptPath">The script path to validate.</param>
    /// <param name="configPath">The path in the config for error reporting.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult Validate(string scriptPath, string configPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            errors.Add(new ValidationError(configPath, "Script path cannot be empty"));
            return new ValidationResult { Errors = errors };
        }

        // Check for absolute Unix paths
        if (scriptPath.StartsWith('/'))
        {
            errors.Add(new ValidationError(
                configPath,
                $"Script path '{scriptPath}' cannot be an absolute path - must be relative to repository root"));
            return new ValidationResult { Errors = errors };
        }

        // Check for absolute Windows paths (drive letter)
        if (scriptPath.Length >= 2 && char.IsLetter(scriptPath[0]) && scriptPath[1] == ':')
        {
            errors.Add(new ValidationError(
                configPath,
                $"Script path '{scriptPath}' cannot be an absolute path - must be relative to repository root"));
            return new ValidationResult { Errors = errors };
        }

        // Check for directory traversal
        if (ContainsPathTraversal(scriptPath))
        {
            errors.Add(new ValidationError(
                configPath,
                $"Script path '{scriptPath}' contains directory traversal - paths must stay within repository"));
            return new ValidationResult { Errors = errors };
        }

        return new ValidationResult { Errors = errors };
    }

    private static bool ContainsPathTraversal(string path)
    {
        // Normalize path separators
        var normalizedPath = path.Replace('\\', '/');

        // Check for ".." segments that could escape the repository
        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var depth = 0;

        foreach (var segment in segments)
        {
            depth = UpdateDepthForSegment(segment, depth);
            if (depth < 0)
            {
                return true; // Path escapes repository root
            }
        }

        return false;
    }

    private static int UpdateDepthForSegment(string segment, int currentDepth)
    {
        if (string.Equals(segment, "..", StringComparison.Ordinal))
        {
            return currentDepth - 1;
        }

        if (string.Equals(segment, ".", StringComparison.Ordinal))
        {
            return currentDepth;
        }

        return currentDepth + 1;
    }
}
