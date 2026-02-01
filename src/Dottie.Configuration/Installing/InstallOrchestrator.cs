// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Orchestrates installation across all configured sources in priority order.
/// </summary>
public class InstallOrchestrator
{
    private readonly IEnumerable<IInstallSource> _installers;

    /// <summary>
    /// Creates a new instance of <see cref="InstallOrchestrator"/>.
    /// </summary>
    /// <param name="installers">Collection of installer services, in priority order.</param>
    public InstallOrchestrator(IEnumerable<IInstallSource> installers)
    {
        _installers = installers ?? throw new ArgumentNullException(nameof(installers));
    }

    /// <summary>
    /// Orchestrates installation from all configured sources.
    /// </summary>
    /// <param name="context">The installation context.</param>
    /// <param name="installBlock">The installation block containing configurations for all sources.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of installation results from all sources.</returns>
    public async Task<IEnumerable<InstallResult>> InstallAsync(
        InstallContext context,
        InstallBlock installBlock,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (installBlock == null)
        {
            throw new ArgumentNullException(nameof(installBlock));
        }

        var allResults = new List<InstallResult>();

        // Process each installer and collect results
        // If one installer throws, continue with others but log the error
        foreach (var installer in _installers)
        {
            try
            {
                var results = await installer.InstallAsync(context, cancellationToken);
                allResults.AddRange(results);
            }
            catch (Exception ex)
            {
                // In production, this would be logged properly
                System.Diagnostics.Debug.WriteLine($"Installer {installer.SourceType} failed: {ex.Message}");
                // Continue with next installer
            }
        }

        return allResults;
    }
}
