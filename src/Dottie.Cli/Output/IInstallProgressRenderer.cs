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
}
