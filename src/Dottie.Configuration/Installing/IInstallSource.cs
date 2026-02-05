// -----------------------------------------------------------------------
// <copyright file="IInstallSource.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Common interface for all installation source implementations.
/// </summary>
public interface IInstallSource
{
    /// <summary>
    /// Gets the source type this installer handles.
    /// </summary>
    /// <value>
    /// <placeholder>The source type this installer handles.</placeholder>
    /// </value>
    InstallSourceType SourceType { get; }

    /// <summary>
    /// Installs items from this source and returns results for each item.
    /// </summary>
    /// <param name="installBlock">The install block containing items to install.</param>
    /// <param name="context">The shared installation context.</param>
    /// <param name="onItemComplete">Optional callback invoked after each item is processed, for progress tracking. Can be null.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of installation results for each item processed.</returns>
    Task<IEnumerable<InstallResult>> InstallAsync(InstallBlock installBlock, InstallContext context, Action? onItemComplete, CancellationToken cancellationToken = default);
}
