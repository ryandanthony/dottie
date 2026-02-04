// -----------------------------------------------------------------------
// <copyright file="IApplyProgressRenderer.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Models;
using Dottie.Configuration.Inheritance;

namespace Dottie.Cli.Output;

/// <summary>
/// Renders progress and summary output for the apply command.
/// </summary>
public interface IApplyProgressRenderer
{
    /// <summary>
    /// Renders the dry-run preview for the apply operation.
    /// </summary>
    /// <param name="profile">The resolved profile being previewed.</param>
    /// <param name="repoRoot">The repository root path.</param>
    void RenderDryRunPreview(ResolvedProfile profile, string repoRoot);

    /// <summary>
    /// Renders the verbose summary of all apply operations.
    /// </summary>
    /// <param name="result">The aggregated apply result.</param>
    /// <param name="profileName">The name of the applied profile.</param>
    void RenderVerboseSummary(ApplyResult result, string profileName);

    /// <summary>
    /// Renders an error message.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    void RenderError(string message);
}
