// -----------------------------------------------------------------------
// <copyright file="IInstallProgressObserver.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing;

namespace Dottie.Cli.Commands;

/// <summary>
/// Observes live install execution so a renderer can show progress bars and a
/// running results list as work proceeds. All members may be called from
/// multiple threads; implementations must be thread-safe.
/// </summary>
internal interface IInstallProgressObserver
{
    /// <summary>
    /// Called once before execution with the set of plans and the overall total.
    /// </summary>
    /// <param name="planNames">Display names of the plans, in execution order.</param>
    /// <param name="planTotals">Item count for each plan, aligned with <paramref name="planNames"/>.</param>
    /// <param name="overallTotal">Total number of items across all plans.</param>
    void OnStart(IReadOnlyList<string> planNames, IReadOnlyList<int> planTotals, int overallTotal);

    /// <summary>
    /// Called after each item of a plan completes, to advance progress bars.
    /// </summary>
    /// <param name="planName">The plan whose item just completed.</param>
    /// <param name="planCompleted">Completed count for that plan.</param>
    /// <param name="overallCompleted">Completed count across all plans.</param>
    void OnItemProgress(string planName, int planCompleted, int overallCompleted);

    /// <summary>
    /// Called as soon as each individual item's result is produced, to append it
    /// to the running results list in real time.
    /// </summary>
    /// <param name="result">The result just produced.</param>
    void OnResult(InstallResult result);
}
