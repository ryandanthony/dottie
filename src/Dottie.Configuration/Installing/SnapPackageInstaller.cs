// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models.InstallBlocks;
using System.Diagnostics;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for snap packages on Ubuntu systems.
/// Installs snap packages with optional classic confinement.
/// </summary>
public class SnapPackageInstaller : IInstallSource
{
    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.SnapPackage;

    /// <inheritdoc/>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallContext context, CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // This method implements the interface. The actual work is done in InstallAsync with InstallBlock.
        return new List<InstallResult>();
    }

    /// <summary>
    /// Installs snap packages from the provided install block.
    /// </summary>
    /// <param name="installBlock">The install block containing snap package specifications.</param>
    /// <param name="context">The installation context with paths and configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Installation results for each snap package.</returns>
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

        // Check if there are any snap packages to install
        if (installBlock.Snaps == null || installBlock.Snaps.Count == 0)
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
            foreach (var snap in installBlock.Snaps)
            {
                results.Add(InstallResult.Warning(snap.Name, SourceType, "Sudo required to install snap packages"));
            }
            return results;
        }

        // Install each snap package
        foreach (var snap in installBlock.Snaps)
        {
            try
            {
                var arguments = $"snap install {snap.Name}";
                if (snap.Classic)
                {
                    arguments += " --classic";
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sudo",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0)
                {
                    results.Add(InstallResult.Success(snap.Name, SourceType));
                }
                else
                {
                    results.Add(InstallResult.Failed(snap.Name, SourceType, $"snap install failed with exit code {process.ExitCode}"));
                }
            }
            catch (Exception ex)
            {
                results.Add(InstallResult.Failed(snap.Name, SourceType, $"Exception during installation: {ex.Message}"));
            }
        }

        return results;
    }
}
