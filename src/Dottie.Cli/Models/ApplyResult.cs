// -----------------------------------------------------------------------
// <copyright file="ApplyResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Cli.Models;

/// <summary>
/// Aggregated result of the apply operation, combining link and install phases.
/// </summary>
public sealed class ApplyResult
{
    /// <summary>
    /// Gets the result of the link phase.
    /// </summary>
    /// <value>
    /// The link phase result.
    /// </value>
    public required LinkPhaseResult LinkPhase { get; init; }

    /// <summary>
    /// Gets the result of the install phase.
    /// </summary>
    /// <value>
    /// The install phase result.
    /// </value>
    public required InstallPhaseResult InstallPhase { get; init; }

    /// <summary>
    /// Gets a value indicating whether the overall apply operation succeeded.
    /// </summary>
    /// <value>
    /// <c>true</c> if no phases were blocked and no operations failed; otherwise, <c>false</c>.
    /// </value>
    public bool OverallSuccess =>
        !LinkPhase.WasBlocked &&
        !LinkPhase.HasFailures &&
        !InstallPhase.HasFailures;
}
