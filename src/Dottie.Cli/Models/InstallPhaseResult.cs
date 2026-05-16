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
    private const int FullCompletionPercentage = 100;

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
    /// Gets the total number of installation items that were processed.
    /// </summary>
    public int TotalCount => Results.Count;

    /// <summary>
    /// Gets the number of items installed or updated successfully.
    /// </summary>
    public int InstalledCount => Results.Count(r => r.Status == InstallStatus.Success);

    /// <summary>
    /// Gets the number of items already installed or otherwise skipped as complete.
    /// </summary>
    public int SkippedCount => Results.Count(r => r.Status == InstallStatus.Skipped);

    /// <summary>
    /// Gets the number of items that completed successfully, including skipped items.
    /// </summary>
    public int CompletedCount => InstalledCount + SkippedCount;

    /// <summary>
    /// Gets the number of warning results that still need attention.
    /// </summary>
    public int WarningCount => Results.Count(r => r.Status == InstallStatus.Warning);

    /// <summary>
    /// Gets the number of failed items that remain unresolved.
    /// </summary>
    public int FailedCount => Results.Count(r => r.Status == InstallStatus.Failed);

    /// <summary>
    /// Gets the number of items left unresolved after the run.
    /// </summary>
    public int RemainingCount => WarningCount + FailedCount;

    /// <summary>
    /// Gets the percentage of items completed successfully or skipped as already current.
    /// </summary>
    public int CompletionPercentage => TotalCount == 0
        ? FullCompletionPercentage
        : (int)Math.Round((double)CompletedCount * FullCompletionPercentage / TotalCount, MidpointRounding.AwayFromZero);

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
