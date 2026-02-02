// -----------------------------------------------------------------------
// <copyright file="SudoChecker.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Installing.Utilities;

/// <summary>
/// Utility to detect whether sudo is available on the system.
/// </summary>
public class SudoChecker
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SudoChecker"/> class.
    /// Creates a new instance of <see cref="SudoChecker"/>.
    /// </summary>
    /// <param name="processRunner">Process runner for executing system commands. If null, a default instance is created.</param>
    public SudoChecker(IProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new ProcessRunner();
    }

    /// <summary>
    /// Checks if sudo is available on the current system.
    /// </summary>
    /// <returns>True if sudo is available; otherwise, false.</returns>
    public bool IsSudoAvailable()
    {
        // Only relevant on Unix-like systems
        if (!IsUnixLike())
        {
            return false;
        }

        try
        {
            var result = _processRunner.Run("which", "sudo", timeoutMilliseconds: 1000);
            return result.Success;
        }
        catch
        {
            // If we can't check, assume it's not available
            return false;
        }
    }

    /// <summary>
    /// Checks if the current system is Unix-like (Linux, macOS, etc.).
    /// </summary>
    /// <returns>True if the system is Unix-like; otherwise, false.</returns>
    private static bool IsUnixLike()
    {
        return OperatingSystem.IsLinux() ||
               OperatingSystem.IsMacOS();
    }
}
