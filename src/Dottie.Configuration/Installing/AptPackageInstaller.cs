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
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallBlock installBlock, InstallContext context, IInstallItemReporter? reporter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installBlock);
        ArgumentNullException.ThrowIfNull(context);

        if (installBlock.Apt == null || installBlock.Apt.Count == 0 || context.DryRun)
        {
            return [];
        }

        if (!context.HasSudo)
        {
            return CreateSudoRequiredWarnings(installBlock.Apt.AsReadOnly(), reporter);
        }

        return await ExecutePackageInstallationAsync(installBlock.Apt.AsReadOnly(), reporter, cancellationToken);
    }

    private List<InstallResult> CreateSudoRequiredWarnings(IReadOnlyList<string> packages, IInstallItemReporter? reporter)
    {
        var results = new List<InstallResult>();
        foreach (var package in packages)
        {
            reporter?.ItemStarted(package, SourceType);
            var result = InstallResult.Warning(package, SourceType, "Sudo required to install APT packages");
            results.Add(result);
            reporter?.ItemCompleted(result);
        }

        return results;
    }

    private async Task<List<InstallResult>> ExecutePackageInstallationAsync(IReadOnlyList<string> packages, IInstallItemReporter? reporter, CancellationToken cancellationToken)
    {
        var results = new List<InstallResult>();

        try
        {
            // Run apt-get update first (ignore failure, package installation might still work)
            await _processRunner.RunAsync("sudo", "apt-get update", cancellationToken: cancellationToken);

            foreach (var package in packages)
            {
                reporter?.ItemStarted(package, SourceType);
                var installResult = await _processRunner.RunAsync("sudo", $"apt-get install -y {package}", cancellationToken: cancellationToken);
                var result = installResult.Success
                    ? InstallResult.Success(package, SourceType)
                    : InstallResult.Failed(package, SourceType, $"apt-get install failed with exit code {installResult.ExitCode}");
                results.Add(result);
                reporter?.ItemCompleted(result);
            }
        }
        catch (Exception ex)
        {
            foreach (var package in packages)
            {
                var result = InstallResult.Failed(package, SourceType, $"Exception during installation: {ex.Message}");
                results.Add(result);
                reporter?.ItemCompleted(result);
            }
        }

        return results;
    }
}
