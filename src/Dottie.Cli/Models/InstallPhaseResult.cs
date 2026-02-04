// -----------------------------------------------------------------------
// <copyright file="InstallPhaseResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing;

namespace Dottie.Cli.Models;

/// <summary>
/// Result of the install phase of the apply command.
/// </summary>
public sealed class InstallPhaseResult
{
    /// <summary>
    /// Gets a value indicating whether the install phase was executed.
    /// </summary>
    /// <value>
    /// <c>true</c> if an install block was configured and installation was attempted; otherwise, <c>false</c>.
    /// </value>
    public bool WasExecuted { get; init; }

    /// <summary>
    /// Gets the individual installation results.
    /// </summary>
    /// <value>
    /// The list of installation results, empty if not executed.
    /// </value>
    public IReadOnlyList<InstallResult> Results { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether any installation failed.
    /// </summary>
    /// <value>
    /// <c>true</c> if any installation failed; otherwise, <c>false</c>.
    /// </value>
    public bool HasFailures => Results.Any(r => r.Status == InstallStatus.Failed);

    /// <summary>
    /// Creates a result for when no install block is configured in the profile.
    /// </summary>
    /// <returns>A result indicating the install phase was not executed.</returns>
    public static InstallPhaseResult NotExecuted() => new() { WasExecuted = false };

    /// <summary>
    /// Creates a result with the given installation results.
    /// </summary>
    /// <param name="results">The list of installation results.</param>
    /// <returns>A result containing the installation outcomes.</returns>
    public static InstallPhaseResult Executed(IReadOnlyList<InstallResult> results) =>
        new() { WasExecuted = true, Results = results };
}
