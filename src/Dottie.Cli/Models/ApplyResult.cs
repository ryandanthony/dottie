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
    private const int FullCompletionPercentage = 100;

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

    /// <summary>
    /// Gets the total number of operations attempted across link and install phases.
    /// </summary>
    public int TotalOperationCount
    {
        get
        {
            var linkResult = LinkPhase.ExecutionResult?.LinkResult;
            var linkTotal = (linkResult?.SuccessfulLinks.Count ?? 0)
                + (linkResult?.SkippedLinks.Count ?? 0)
                + (linkResult?.FailedLinks.Count ?? 0);

            return linkTotal + InstallPhase.TotalCount;
        }
    }

    /// <summary>
    /// Gets the number of operations completed successfully or skipped as already handled.
    /// </summary>
    public int CompletedOperationCount
    {
        get
        {
            var linkResult = LinkPhase.ExecutionResult?.LinkResult;
            var linkCompleted = (linkResult?.SuccessfulLinks.Count ?? 0)
                + (linkResult?.SkippedLinks.Count ?? 0);

            return linkCompleted + InstallPhase.CompletedCount;
        }
    }

    /// <summary>
    /// Gets the number of operations still left unresolved.
    /// </summary>
    public int RemainingOperationCount
    {
        get
        {
            var linkFailed = LinkPhase.ExecutionResult?.LinkResult?.FailedLinks.Count ?? 0;
            return linkFailed + InstallPhase.RemainingCount;
        }
    }

    /// <summary>
    /// Gets the overall completion percentage across link and install phases.
    /// </summary>
    public int CompletionPercentage => TotalOperationCount == 0
        ? FullCompletionPercentage
        : (int)Math.Round((double)CompletedOperationCount * FullCompletionPercentage / TotalOperationCount, MidpointRounding.AwayFromZero);
}
