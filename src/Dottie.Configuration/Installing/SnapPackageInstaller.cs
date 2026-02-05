// -----------------------------------------------------------------------
// <copyright file="SnapPackageInstaller.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for snap packages on Ubuntu systems.
/// Installs snap packages with optional classic confinement.
/// </summary>
public class SnapPackageInstaller : IInstallSource
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapPackageInstaller"/> class.
    /// Creates a new instance of <see cref="SnapPackageInstaller"/>.
    /// </summary>
    /// <param name="processRunner">Process runner for executing system commands. If null, a default instance is created.</param>
    public SnapPackageInstaller(IProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new ProcessRunner();
    }

    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.SnapPackage;

    /// <inheritdoc/>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallBlock installBlock, InstallContext context, Action? onItemComplete, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installBlock);

        ArgumentNullException.ThrowIfNull(context);

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
                onItemComplete?.Invoke();
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

                var processResult = await _processRunner.RunAsync("sudo", arguments, cancellationToken: cancellationToken);

                results.Add(processResult.Success ? InstallResult.Success(snap.Name, SourceType) : InstallResult.Failed(snap.Name, SourceType, $"snap install failed with exit code {processResult.ExitCode}"));
            }
            catch (Exception ex)
            {
                results.Add(InstallResult.Failed(snap.Name, SourceType, $"Exception during installation: {ex.Message}"));
            }

            onItemComplete?.Invoke();
        }

        return results;
    }
}
