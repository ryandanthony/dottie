// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dottie.Configuration.Installing.Utilities;

/// <summary>
/// Utility to detect whether sudo is available on the system.
/// </summary>
public static class SudoChecker
{
    /// <summary>
    /// Checks if sudo is available on the current system.
    /// </summary>
    /// <returns>True if sudo is available; otherwise, false.</returns>
    public static bool IsSudoAvailable()
    {
        // Only relevant on Unix-like systems
        if (!IsUnixLike())
        {
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "sudo",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(1000); // Timeout after 1 second

            return process?.ExitCode == 0;
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
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
