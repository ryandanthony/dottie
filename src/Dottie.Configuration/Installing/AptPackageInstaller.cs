// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models.InstallBlocks;
using System.Diagnostics;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for APT packages on Ubuntu systems.
/// Installs standard Ubuntu packages via apt-get.
/// </summary>
public class AptPackageInstaller : IInstallSource
{
    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.AptPackage;

    /// <inheritdoc/>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallContext context, CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // This method implements the interface. The actual work is done in InstallAsync with InstallBlock.
        // For MVP, return empty to allow the orchestrator to work.
        return new List<InstallResult>();
    }

    /// <summary>
    /// Installs APT packages from the provided install block.
    /// </summary>
    /// <param name="installBlock">The install block containing APT package specifications.</param>
    /// <param name="context">The installation context with paths and configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Installation results for each package.</returns>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallBlock installBlock, InstallContext context, CancellationToken cancellationToken = default)
    {
        if (installBlock == null)
        {
            throw new ArgumentNullException(nameof(installBlock));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var results = new List<InstallResult>();

        // Check if there are any APT packages to install
        if (installBlock.Apt == null || installBlock.Apt.Count == 0)
        {
            return results;
        }

        // Skip installation if dry-run is enabled
        if (context.DryRun)
        {
            return results;
        }

        // If sudo is not available, return warning results
        if (!context.HasSudo)
        {
            foreach (var package in installBlock.Apt)
            {
                results.Add(InstallResult.Warning(package, SourceType, "Sudo required to install APT packages"));
            }
            return results;
        }

        // Install packages
        try
        {
            // Run apt-get update first
            var updateProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "apt-get update",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            updateProcess.Start();
            await updateProcess.WaitForExitAsync(cancellationToken);

            if (updateProcess.ExitCode != 0)
            {
                // Continue anyway, package installation might still work
            }

            // Install each package
            foreach (var package in installBlock.Apt)
            {
                var installProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sudo",
                        Arguments = $"apt-get install -y {package}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                installProcess.Start();
                await installProcess.WaitForExitAsync(cancellationToken);

                if (installProcess.ExitCode == 0)
                {
                    results.Add(InstallResult.Success(package, SourceType));
                }
                else
                {
                    results.Add(InstallResult.Failed(package, SourceType, $"apt-get install failed with exit code {installProcess.ExitCode}"));
                }
            }
        }
        catch (Exception ex)
        {
            // Add failed results for all packages if an exception occurs
            foreach (var package in installBlock.Apt)
            {
                results.Add(InstallResult.Failed(package, SourceType, $"Exception during installation: {ex.Message}"));
            }
        }

        return results;
    }
}
