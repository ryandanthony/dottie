// -----------------------------------------------------------------------
// <copyright file="AptPackageInstaller.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for APT packages on Ubuntu systems.
/// Installs standard Ubuntu packages via apt-get.
/// </summary>
public class AptPackageInstaller : IInstallSource
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="AptPackageInstaller"/> class.
    /// Creates a new instance of <see cref="AptPackageInstaller"/>.
    /// </summary>
    /// <param name="processRunner">Process runner for executing system commands. If null, a default instance is created.</param>
    public AptPackageInstaller(IProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new ProcessRunner();
    }

    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.AptPackage;

    /// <inheritdoc/>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallBlock installBlock, InstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installBlock);

        ArgumentNullException.ThrowIfNull(context);

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
            var updateResult = await _processRunner.RunAsync("sudo", "apt-get update", cancellationToken: cancellationToken);

            if (!updateResult.Success)
            {
                // Continue anyway, package installation might still work
            }

            // Install each package
            foreach (var package in installBlock.Apt)
            {
                var installResult = await _processRunner.RunAsync("sudo", $"apt-get install -y {package}", cancellationToken: cancellationToken);

                results.Add(installResult.Success ? InstallResult.Success(package, SourceType) : InstallResult.Failed(package, SourceType, $"apt-get install failed with exit code {installResult.ExitCode}"));
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
