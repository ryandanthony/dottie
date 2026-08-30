// -----------------------------------------------------------------------
// <copyright file="InstallExecutionCoordinator.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;
using Spectre.Console;

namespace Dottie.Cli.Commands;

/// <summary>
/// Coordinates install execution, including staged parallel execution and progress rendering.
/// </summary>
[SuppressMessage(
    "Major Code Smell",
    "S1200:Classes should not be coupled to too many other classes (Single Responsibility Principle)",
    Justification = "This coordinator intentionally composes the CLI progress system with installer implementations.")]
internal static class InstallExecutionCoordinator
{
    private static readonly Lock ProgressUpdateLock = new();

    internal static async Task<List<InstallResult>> RunAsync(InstallBlock installBlock, InstallContext context)
    {
        ArgumentNullException.ThrowIfNull(installBlock);
        ArgumentNullException.ThrowIfNull(context);

        var executionPlans = InstallerProgressHelper.GetInstallerItems(installBlock)
            .Where(plan => plan.Count > 0)
            .ToArray();

        if (executionPlans.Length == 0)
        {
            return [];
        }

        var results = new ConcurrentBag<InstallResult>();

        try
        {
            await ExecuteWithProgressAsync(installBlock, context, executionPlans, results);
        }
        catch (InvalidOperationException)
        {
            await ExecuteWithoutProgressAsync(installBlock, context, executionPlans, results);
        }

        return OrderResults(results);
    }

    /// <summary>
    /// Runs installation while reporting progress and results to an observer,
    /// with no direct console output. The observer (e.g. a live renderer) owns
    /// all rendering. Falls back to reporting nothing extra when there is no work.
    /// </summary>
    /// <param name="installBlock">The resolved install block.</param>
    /// <param name="context">The shared install context.</param>
    /// <param name="observer">Observer notified of start, per-item progress, and per-plan results.</param>
    /// <returns>The ordered install results.</returns>
    internal static async Task<List<InstallResult>> RunWithObserverAsync(
        InstallBlock installBlock,
        InstallContext context,
        IInstallProgressObserver observer)
    {
        ArgumentNullException.ThrowIfNull(installBlock);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(observer);

        var executionPlans = InstallerProgressHelper.GetInstallerItems(installBlock)
            .Where(plan => plan.Count > 0)
            .ToArray();

        if (executionPlans.Length == 0)
        {
            observer.OnStart([], [], 0);
            return [];
        }

        var totalItems = executionPlans.Sum(plan => plan.Count);
        observer.OnStart(
            executionPlans.Select(plan => plan.Name).ToArray(),
            executionPlans.Select(plan => plan.Count).ToArray(),
            totalItems);

        var results = new ConcurrentBag<InstallResult>();
        var overallCompleted = 0;

        foreach (var stage in executionPlans.GroupBy(plan => plan.Stage).OrderBy(group => group.Key))
        {
            await ExecuteStageWithObserverAsync(installBlock, context, stage.ToArray(), results, observer, () => overallCompleted, next => overallCompleted = next);
        }

        return OrderResults(results);
    }

    private static async Task ExecuteStageWithObserverAsync(
        InstallBlock installBlock,
        InstallContext context,
        IReadOnlyList<InstallerProgressHelper.InstallerExecutionPlan> stagePlans,
        ConcurrentBag<InstallResult> results,
        IInstallProgressObserver observer,
        Func<int> readOverall,
        Action<int> writeOverall)
    {
        var parallelPlans = stagePlans.Where(plan => plan.CanRunInParallel).ToArray();
        var serialPlans = stagePlans.Where(plan => !plan.CanRunInParallel).ToArray();

        if (parallelPlans.Length > 0)
        {
            await Task.WhenAll(parallelPlans.Select(plan =>
                ExecutePlanWithObserverAsync(installBlock, context, plan, results, observer, readOverall, writeOverall)));
        }

        foreach (var plan in serialPlans)
        {
            await ExecutePlanWithObserverAsync(installBlock, context, plan, results, observer, readOverall, writeOverall);
        }
    }

    private static async Task ExecutePlanWithObserverAsync(
        InstallBlock installBlock,
        InstallContext context,
        InstallerProgressHelper.InstallerExecutionPlan plan,
        ConcurrentBag<InstallResult> results,
        IInstallProgressObserver observer,
        Func<int> readOverall,
        Action<int> writeOverall)
    {
        var completed = 0;

        var planResults = await ExecuteInstallerAsync(plan.Installer, installBlock, context, () =>
        {
            lock (ProgressUpdateLock)
            {
                completed++;
                var overall = readOverall() + 1;
                writeOverall(overall);
                observer.OnItemProgress(plan.Name, completed, overall);
            }
        });

        var planResultList = planResults.ToList();
        foreach (var result in planResultList)
        {
            results.Add(result);
        }

        observer.OnResults(planResultList);
    }

    private static async Task ExecuteWithProgressAsync(
        InstallBlock installBlock,
        InstallContext context,
        IReadOnlyList<InstallerProgressHelper.InstallerExecutionPlan> executionPlans,
        ConcurrentBag<InstallResult> results)
    {
        var totalItems = executionPlans.Sum(plan => plan.Count);

        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async progressContext =>
            {
                var overallTask = progressContext.AddTask(CreateOverallDescription(0, totalItems), maxValue: totalItems);
                var planTasks = executionPlans.ToDictionary(
                    plan => plan,
                    plan => progressContext.AddTask(CreatePlanDescription(plan.Name, 0, plan.Count), maxValue: plan.Count));

                foreach (var stage in executionPlans.GroupBy(plan => plan.Stage).OrderBy(group => group.Key))
                {
                    await ExecuteStageAsync(installBlock, context, stage.ToArray(), results, overallTask, planTasks);
                }

                overallTask.Description = CreateOverallDescription(totalItems, totalItems);
            });
    }

    private static async Task ExecuteWithoutProgressAsync(
        InstallBlock installBlock,
        InstallContext context,
        IReadOnlyList<InstallerProgressHelper.InstallerExecutionPlan> executionPlans,
        ConcurrentBag<InstallResult> results)
    {
        foreach (var stage in executionPlans.GroupBy(plan => plan.Stage).OrderBy(group => group.Key))
        {
            await ExecuteStageWithoutProgressAsync(installBlock, context, stage.ToArray(), results);
        }
    }

    private static async Task ExecuteStageAsync(
        InstallBlock installBlock,
        InstallContext context,
        IReadOnlyList<InstallerProgressHelper.InstallerExecutionPlan> stagePlans,
        ConcurrentBag<InstallResult> results,
        ProgressTask overallTask,
        IReadOnlyDictionary<InstallerProgressHelper.InstallerExecutionPlan, ProgressTask> planTasks)
    {
        var parallelPlans = stagePlans.Where(plan => plan.CanRunInParallel).ToArray();
        var serialPlans = stagePlans.Where(plan => !plan.CanRunInParallel).ToArray();

        if (parallelPlans.Length > 0)
        {
            await Task.WhenAll(parallelPlans.Select(plan =>
                ExecutePlanAsync(installBlock, context, plan, results, overallTask, planTasks[plan])));
        }

        foreach (var plan in serialPlans)
        {
            await ExecutePlanAsync(installBlock, context, plan, results, overallTask, planTasks[plan]);
        }
    }

    private static async Task ExecuteStageWithoutProgressAsync(
        InstallBlock installBlock,
        InstallContext context,
        IReadOnlyList<InstallerProgressHelper.InstallerExecutionPlan> stagePlans,
        ConcurrentBag<InstallResult> results)
    {
        var parallelPlans = stagePlans.Where(plan => plan.CanRunInParallel).ToArray();
        var serialPlans = stagePlans.Where(plan => !plan.CanRunInParallel).ToArray();

        if (parallelPlans.Length > 0)
        {
            var parallelResults = await Task.WhenAll(parallelPlans.Select(plan =>
                ExecuteInstallerAsync(plan.Installer, installBlock, context, null)));

            foreach (var resultGroup in parallelResults)
            {
                foreach (var result in resultGroup)
                {
                    results.Add(result);
                }
            }
        }

        foreach (var plan in serialPlans)
        {
            var planResults = await ExecuteInstallerAsync(plan.Installer, installBlock, context, null);
            foreach (var result in planResults)
            {
                results.Add(result);
            }
        }
    }

    private static async Task ExecutePlanAsync(
        InstallBlock installBlock,
        InstallContext context,
        InstallerProgressHelper.InstallerExecutionPlan plan,
        ConcurrentBag<InstallResult> results,
        ProgressTask overallTask,
        ProgressTask planTask)
    {
        var completed = 0;
        var total = plan.Count;

        var planResults = await ExecuteInstallerAsync(plan.Installer, installBlock, context, () =>
        {
            lock (ProgressUpdateLock)
            {
                completed++;
                planTask.Increment(1);
                planTask.Description = CreatePlanDescription(plan.Name, completed, total);
                overallTask.Increment(1);
                overallTask.Description = CreateOverallDescription((int)overallTask.Value, (int)overallTask.MaxValue);
            }
        });

        foreach (var result in planResults)
        {
            results.Add(result);
        }
    }

    private static async Task<IEnumerable<InstallResult>> ExecuteInstallerAsync(
        IInstallSource installer,
        InstallBlock installBlock,
        InstallContext context,
        Action? onItemComplete)
    {
        try
        {
            return installer.SourceType switch
            {
                InstallSourceType.GithubRelease => await ((GithubReleaseInstaller)installer).InstallAsync(installBlock, context, onItemComplete),
                InstallSourceType.AptPackage => await ((AptPackageInstaller)installer).InstallAsync(installBlock, context, onItemComplete),
                InstallSourceType.AptRepo => await ((AptRepoInstaller)installer).InstallAsync(installBlock, context, onItemComplete),
                InstallSourceType.Script => await ((ScriptRunner)installer).InstallAsync(installBlock, context, onItemComplete),
                InstallSourceType.Font => await ((FontInstaller)installer).InstallAsync(installBlock, context, onItemComplete),
                InstallSourceType.SnapPackage => await ((SnapPackageInstaller)installer).InstallAsync(installBlock, context, onItemComplete),
                _ => [],
            };
        }
        catch (Exception ex)
        {
            return [InstallResult.Failed(installer.SourceType.ToString(), installer.SourceType, $"Installer error: {ex.Message}")];
        }
    }

    private static List<InstallResult> OrderResults(IEnumerable<InstallResult> results) =>
        results
            .OrderBy(result => InstallerProgressHelper.GetSourceTypeOrder(result.SourceType))
            .ThenBy(result => result.ItemName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string CreatePlanDescription(string planName, int completed, int total)
        => $"[blue]{Markup.Escape(planName)}[/] [dim][[{completed}/{total}]][/]";

    private static string CreateOverallDescription(int completed, int total)
        => $"[green]Overall progress[/] [dim][[{completed}/{total}]][/]";
}
