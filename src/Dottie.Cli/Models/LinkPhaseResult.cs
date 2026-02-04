// -----------------------------------------------------------------------
// <copyright file="LinkPhaseResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Linking;

namespace Dottie.Cli.Models;

/// <summary>
/// Result of the link phase of the apply command.
/// </summary>
public sealed class LinkPhaseResult
{
    /// <summary>
    /// Gets a value indicating whether the link phase was executed.
    /// </summary>
    /// <value>
    /// <c>true</c> if dotfiles were configured and linking was attempted; otherwise, <c>false</c>.
    /// </value>
    public bool WasExecuted { get; init; }

    /// <summary>
    /// Gets a value indicating whether linking was blocked by conflicts.
    /// </summary>
    /// <value>
    /// <c>true</c> if conflicts prevented linking without --force; otherwise, <c>false</c>.
    /// </value>
    public bool WasBlocked { get; init; }

    /// <summary>
    /// Gets the detailed execution result from the LinkingOrchestrator.
    /// </summary>
    /// <value>
    /// The link execution result, or <c>null</c> if not executed.
    /// </value>
    public LinkExecutionResult? ExecutionResult { get; init; }

    /// <summary>
    /// Gets a value indicating whether any link operation failed.
    /// </summary>
    /// <value>
    /// <c>true</c> if any link operation failed; otherwise, <c>false</c>.
    /// </value>
    public bool HasFailures => ExecutionResult?.LinkResult?.FailedLinks.Count > 0;

    /// <summary>
    /// Creates a result for when no dotfiles are configured in the profile.
    /// </summary>
    /// <returns>A result indicating the link phase was not executed.</returns>
    public static LinkPhaseResult NotExecuted() => new() { WasExecuted = false };

    /// <summary>
    /// Creates a result for when linking was blocked by conflicts.
    /// </summary>
    /// <param name="result">The execution result containing conflict information.</param>
    /// <returns>A result indicating the link phase was blocked.</returns>
    public static LinkPhaseResult Blocked(LinkExecutionResult result) =>
        new() { WasExecuted = true, WasBlocked = true, ExecutionResult = result };

    /// <summary>
    /// Creates a result for successful link phase execution.
    /// </summary>
    /// <param name="result">The execution result containing link operation details.</param>
    /// <returns>A result indicating the link phase completed.</returns>
    public static LinkPhaseResult Executed(LinkExecutionResult result) =>
        new() { WasExecuted = true, WasBlocked = false, ExecutionResult = result };
}
