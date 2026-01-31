// -----------------------------------------------------------------------
// <copyright file="LinkOperationResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Linking;

/// <summary>
/// The aggregate result of a link operation across all dotfile entries.
/// </summary>
public sealed record LinkOperationResult
{
    /// <summary>
    /// Gets a value indicating whether the entire operation succeeded.
    /// </summary>
    public bool IsSuccess => FailedLinks.Count == 0;

    /// <summary>
    /// Gets the list of successfully linked entries.
    /// </summary>
    public required IReadOnlyList<LinkResult> SuccessfulLinks { get; init; }

    /// <summary>
    /// Gets the list of skipped entries (already correctly linked).
    /// </summary>
    public required IReadOnlyList<LinkResult> SkippedLinks { get; init; }

    /// <summary>
    /// Gets the list of failed link attempts.
    /// </summary>
    public required IReadOnlyList<LinkResult> FailedLinks { get; init; }

    /// <summary>
    /// Gets the total number of entries processed.
    /// </summary>
    public int TotalProcessed => SuccessfulLinks.Count + SkippedLinks.Count + FailedLinks.Count;
}
