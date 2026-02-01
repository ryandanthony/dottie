// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Dottie.Configuration.Installing;

/// <summary>
/// Common interface for all installation source implementations.
/// </summary>
public interface IInstallSource
{
    /// <summary>
    /// Gets the source type this installer handles.
    /// </summary>
    InstallSourceType SourceType { get; }

    /// <summary>
    /// Installs items from this source and returns results for each item.
    /// </summary>
    /// <param name="context">The shared installation context.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of installation results for each item processed.</returns>
    Task<IEnumerable<InstallResult>> InstallAsync(InstallContext context, CancellationToken cancellationToken = default);
}
