// -----------------------------------------------------------------------
// <copyright file="SudoChecker.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Dottie.Configuration.Models.InstallBlocks;

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
    public virtual bool IsSudoAvailable()
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
    /// Determines whether the install block contains any items that need root
    /// privileges: APT packages, APT repositories, or snap packages.
    /// </summary>
    /// <param name="installBlock">The resolved install block, or null.</param>
    /// <returns>True if any privileged install item is present.</returns>
    public static bool RequiresSudo(InstallBlock? installBlock)
    {
        if (installBlock is null)
        {
            return false;
        }

        return installBlock.Apt.Count > 0
            || installBlock.AptRepos.Count > 0
            || installBlock.Snaps.Count > 0;
    }

    /// <summary>
    /// Checks whether sudo already has valid cached credentials (so no prompt
    /// would appear). Uses <c>sudo -n true</c>, which never prompts.
    /// </summary>
    /// <returns>True if credentials are cached; otherwise, false.</returns>
    public virtual bool HasCachedCredentials()
    {
        if (!IsUnixLike())
        {
            return false;
        }

        try
        {
            var result = _processRunner.Run("sudo", "-n true", timeoutMilliseconds: 2000);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Interactively primes the sudo credential cache by running <c>sudo -v</c>
    /// with inherited stdio, so the password (or fingerprint) prompt is shown
    /// directly to the user. Intended to be called before any full-screen UI
    /// takes over the terminal.
    /// </summary>
    /// <returns>True if sudo credentials are now cached; otherwise, false.</returns>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Spawns an interactive sudo process with inherited stdio; exercised via manual/integration runs, not unit tests.")]
    public virtual bool PrimeInteractive()
    {
        if (!IsUnixLike())
        {
            return false;
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "-v",

                    // Inherit the real terminal so the prompt is visible and can
                    // read the password / drive the PAM fingerprint conversation.
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                    CreateNoWindow = false,
                },
            };

            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
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
