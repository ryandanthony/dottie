// -----------------------------------------------------------------------
// <copyright file="SymlinkService.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Linking;

/// <summary>
/// Creates and verifies symbolic links.
/// </summary>
public sealed class SymlinkService
{
    /// <summary>
    /// Gets the error message from the last failed operation.
    /// </summary>
    /// <value>
    /// <placeholder>The error message from the last failed operation.</placeholder>
    /// </value>
    public string? LastError { get; private set; }

    /// <summary>
    /// Gets the Windows-specific symlink permission error message.
    /// </summary>
    public static string WindowsSymlinkErrorMessage { get; } = """
        Error: Unable to create symbolic link - insufficient permissions.

        On Windows, symbolic links require either:
          • Run dottie as Administrator, OR
          • Enable Developer Mode in Windows Settings > Update & Security > For developers
        """;

    /// <summary>
    /// Creates a symbolic link.
    /// </summary>
    /// <param name="linkPath">The path where the symlink will be created.</param>
    /// <param name="targetPath">The path the symlink will point to.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool CreateSymlink(string linkPath, string targetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(linkPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        LastError = null;

        try
        {
            // Create parent directory if it doesn't exist
            var parentDir = Path.GetDirectoryName(linkPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            // Determine if target is a directory
            if (Directory.Exists(targetPath))
            {
                Directory.CreateSymbolicLink(linkPath, targetPath);
            }
            else
            {
                File.CreateSymbolicLink(linkPath, targetPath);
            }

            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            // Check if this is a Windows symlink permission error
            LastError = OperatingSystem.IsWindows() ? WindowsSymlinkErrorMessage : $"Permission denied: {ex.Message}";

            return false;
        }
        catch (IOException ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Checks if the path is a symlink pointing to the expected target.
    /// </summary>
    /// <param name="linkPath">The path to check.</param>
    /// <param name="expectedTarget">The expected target path.</param>
    /// <returns>True if the path is a symlink pointing to the expected target.</returns>
    public bool IsCorrectSymlink(string linkPath, string expectedTarget)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(linkPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedTarget);

        if (!Path.Exists(linkPath))
        {
            return false;
        }

        var fileInfo = new FileInfo(linkPath);
        var dirInfo = new DirectoryInfo(linkPath);

        string? actualTarget = null;

        if (fileInfo.Exists && fileInfo.LinkTarget != null)
        {
            actualTarget = fileInfo.LinkTarget;
        }
        else if (dirInfo.Exists && dirInfo.LinkTarget != null)
        {
            actualTarget = dirInfo.LinkTarget;
        }
        else
        {
            // Not a symlink
            return false;
        }

        if (actualTarget == null)
        {
            return false;
        }

        // Normalize paths for comparison
        var normalizedActual = Path.GetFullPath(actualTarget, Path.GetDirectoryName(linkPath)!);
        var normalizedExpected = Path.GetFullPath(expectedTarget);

        return string.Equals(normalizedActual, normalizedExpected, StringComparison.Ordinal);
    }
}
