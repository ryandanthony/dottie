// -----------------------------------------------------------------------
// <copyright file="IInstallProgressRenderer.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing;

namespace Dottie.Cli.Output;

/// <summary>
/// Renders installation progress and results to the console.
/// </summary>
public interface IInstallProgressRenderer
{
    /// <summary>
    /// Renders a single installation result.
    /// </summary>
    void RenderProgress(InstallResult result);

    /// <summary>
    /// Renders a summary of all installation results.
    /// </summary>
    void RenderSummary(IEnumerable<InstallResult> results);

    /// <summary>
    /// Renders an error message.
    /// </summary>
    void RenderError(string message);

    /// <summary>
    /// Renders a grouped summary of failed installations.
    /// Failures are grouped by source type for easier troubleshooting.
    /// </summary>
    /// <param name="results">All installation results (will filter to failures).</param>
    void RenderGroupedFailures(IEnumerable<InstallResult> results);
}
