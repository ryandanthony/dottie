// -----------------------------------------------------------------------
// <copyright file="LiveInstallRenderer.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Dottie.Cli.Commands;
using Dottie.Configuration.Installing;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Dottie.Cli.Output;

/// <summary>
/// Hosts a single <see cref="AnsiConsole.Live"/> session for the install phase:
/// a fixed region of progress bars on top and a scrollable, growing list of
/// per-item results below (mirroring the final summary line format).
/// <para>
/// Spectre's <c>AnsiConsole.Progress()</c> owns its own live region and cannot
/// be nested inside a <c>Live</c> layout, so the bars here are rendered manually
/// from tracked counts. The observer callbacks feed both regions and are
/// thread-safe (installers run plans in parallel).
/// </para>
/// </summary>
[SuppressMessage(
    "Major Code Smell",
    "S1200:Classes should not be coupled to too many other classes (Single Responsibility Principle)",
    Justification = "A live renderer inherently composes several Spectre widgets with the install progress model.")]
internal sealed class LiveInstallRenderer : IInstallProgressObserver
{
    private const int BarWidth = 30;
    private const int StatusLabelWidth = 9;
    private const int PercentLabelWidth = 3;
    private const int PanelChromeLines = 2;
    private const int MinResultsWindow = 5;
    private const int FallbackResultsWindow = 15;

    private readonly Lock _sync = new();
    private readonly List<InstallResult> _results = [];
    private readonly Dictionary<string, PlanProgress> _plans = [];
    private readonly List<string> _planOrder = [];

    // Items currently in flight, keyed by display name, in start order. Rendered
    // as "running" lines beneath the results until each one completes.
    private readonly List<RunningItem> _running = [];
    private readonly IAnsiConsole _console;

    private LiveDisplayContext? _ctx;
    private int _overallCompleted;
    private int _overallTotal;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveInstallRenderer"/> class.
    /// </summary>
    /// <param name="console">Console to render into; defaults to <see cref="AnsiConsole.Console"/>. Injected for testing.</param>
    internal LiveInstallRenderer(IAnsiConsole? console = null)
    {
        _console = console ?? AnsiConsole.Console;
    }

    /// <summary>
    /// The install work to run inside the live session, given this renderer as observer.
    /// </summary>
    /// <param name="observer">The progress observer to report to.</param>
    /// <returns>The install results.</returns>
    internal delegate Task<List<InstallResult>> InstallWork(IInstallProgressObserver observer);

    /// <summary>
    /// Runs <paramref name="work"/> inside a live session that this renderer drives.
    /// </summary>
    /// <param name="work">The install work; receives this renderer as its observer.</param>
    /// <returns>The results produced by the work.</returns>
    internal async Task<List<InstallResult>> RunAsync(InstallWork work)
    {
        ArgumentNullException.ThrowIfNull(work);

        List<InstallResult> results = [];
        await _console.Live(BuildLayout())
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                lock (_sync)
                {
                    _ctx = ctx;
                }

                results = await work(this);
                Refresh();
            });

        return results;
    }

    /// <inheritdoc/>
    void IInstallProgressObserver.OnStart(IReadOnlyList<string> planNames, IReadOnlyList<int> planTotals, int overallTotal)
    {
        ArgumentNullException.ThrowIfNull(planNames);
        ArgumentNullException.ThrowIfNull(planTotals);

        lock (_sync)
        {
            _overallTotal = overallTotal;
            for (var i = 0; i < planNames.Count; i++)
            {
                var name = planNames[i];
                if (!_plans.ContainsKey(name))
                {
                    _plans[name] = new PlanProgress(planTotals[i]);
                    _planOrder.Add(name);
                }
            }
        }

        Refresh();
    }

    /// <inheritdoc/>
    void IInstallProgressObserver.OnItemProgress(string planName, int planCompleted, int overallCompleted)
    {
        ArgumentNullException.ThrowIfNull(planName);

        lock (_sync)
        {
            if (_plans.TryGetValue(planName, out var plan))
            {
                plan.Completed = planCompleted;
            }

            _overallCompleted = overallCompleted;
        }

        Refresh();
    }

    /// <inheritdoc/>
    void IInstallProgressObserver.OnItemStarted(string itemName, InstallSourceType sourceType)
    {
        ArgumentNullException.ThrowIfNull(itemName);

        lock (_sync)
        {
            _running.Add(new RunningItem(itemName, sourceType));
        }

        Refresh();
    }

    /// <inheritdoc/>
    void IInstallProgressObserver.OnResult(InstallResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        lock (_sync)
        {
            _results.Add(result);

            // The matching item is no longer running. Remove the first entry with
            // the same name and source, so repeated names still clear one-for-one.
            var index = _running.FindIndex(r => string.Equals(r.Name, result.ItemName, StringComparison.Ordinal) && r.SourceType == result.SourceType);
            if (index >= 0)
            {
                _running.RemoveAt(index);
            }
        }

        Refresh();
    }

    private static IRenderable BuildResultRow(InstallResult result)
    {
        var (icon, color) = result.Status switch
        {
            InstallStatus.Success => ("✓", "green"),
            InstallStatus.Skipped => ("○", "yellow"),
            InstallStatus.Warning => ("⚠", "yellow"),
            InstallStatus.Failed => ("✗", "red"),
            _ => ("?", "grey"),
        };

        var statusText = result.Status switch
        {
            InstallStatus.Success => "Installed",
            InstallStatus.Skipped => "Skipped",
            InstallStatus.Warning => "Warning",
            InstallStatus.Failed => "Failed",
            _ => "Unknown",
        };

        var source = $"[grey]({InstallerProgressHelper.GetSourceTypeName(result.SourceType)})[/]";
        var message = string.IsNullOrEmpty(result.Message) ? string.Empty : $" [grey]-[/] {Markup.Escape(result.Message)}";
        return new Markup($"[{color}]{icon} {statusText,-StatusLabelWidth}[/] {Markup.Escape(result.ItemName)} {source}{message}");
    }

    private static IRenderable BuildRunningRow(RunningItem item)
    {
        const string RunningLabel = "Running";
        var source = $"[grey]({InstallerProgressHelper.GetSourceTypeName(item.SourceType)})[/]";
        return new Markup($"[blue]⟳ {RunningLabel,-StatusLabelWidth}[/] {Markup.Escape(item.Name)} {source}");
    }

    private static string RenderBar(int completed, int total, string color)
    {
        var ratio = total <= 0 ? 1d : Math.Clamp((double)completed / total, 0d, 1d);
        var filled = (int)Math.Round(ratio * BarWidth);
        var empty = BarWidth - filled;
        var percent = (int)Math.Round(ratio * 100);
        return $"[{color}]{new string('━', filled)}[/][grey]{new string('━', empty)}[/] {percent,PercentLabelWidth}%";
    }

    private Layout BuildLayout()
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Progress").Size(ProgressRegionHeight()),
                new Layout("Results"));

        layout["Progress"].Update(BuildProgressPanel());
        layout["Results"].Update(BuildResultsPanel());
        return layout;
    }

    private int ProgressRegionHeight()
    {
        // One row per plan + overall + panel borders/padding.
        var lines = _planOrder.Count + 1;
        return lines + PanelChromeLines;
    }

    private void Refresh()
    {
        lock (_sync)
        {
            if (_ctx is null)
            {
                return;
            }

            _ctx.UpdateTarget(BuildLayout());
            _ctx.Refresh();
        }
    }

    private Panel BuildProgressPanel()
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn(new GridColumn().NoWrap());

        foreach (var name in _planOrder)
        {
            var plan = _plans[name];

            // A plan can emit more result callbacks than its item count (e.g. an
            // apt repo yields key/source/update/package results for one repo), so
            // clamp the displayed count to the total to avoid "[6/2]".
            var shownCompleted = Math.Min(plan.Completed, plan.Total);
            grid.AddRow(
                new Markup($"[blue]{Markup.Escape(name)}[/] [grey][[{shownCompleted}/{plan.Total}]][/]"),
                new Markup(RenderBar(plan.Completed, plan.Total, "blue")));
        }

        var shownOverall = Math.Min(_overallCompleted, _overallTotal);
        grid.AddRow(
            new Markup($"[green]Overall[/] [grey][[{shownOverall}/{_overallTotal}]][/]"),
            new Markup(RenderBar(_overallCompleted, _overallTotal, "green")));

        return new Panel(grid)
            .Header("[bold]Installing[/]")
            .Expand()
            .BorderColor(Color.Blue);
    }

    private Panel BuildResultsPanel()
    {
        var runningRows = _running.ConvertAll(BuildRunningRow);

        // Reserve space for the running lines so they stay visible; the rest of
        // the window shows the tail of completed results.
        var resultsWindow = Math.Max(MinResultsWindow, ResultsWindow() - runningRows.Count);

        var rows = new List<IRenderable>();
        if (_results.Count == 0 && runningRows.Count == 0)
        {
            rows.Add(new Markup("[grey]Waiting for results...[/]"));
        }
        else
        {
            // Show the tail so the newest results stay visible (scroll effect).
            var shown = _results.Count > resultsWindow
                ? _results.GetRange(_results.Count - resultsWindow, resultsWindow)
                : _results;
            rows.AddRange(shown.Select(BuildResultRow));
            rows.AddRange(runningRows);
        }

        var hidden = Math.Max(0, _results.Count - resultsWindow);
        var header = hidden > 0
            ? $"[bold]Results[/] [grey]({_results.Count} total, showing last {resultsWindow})[/]"
            : $"[bold]Results[/] [grey]({_results.Count})[/]";

        return new Panel(new Rows(rows))
            .Header(header)
            .Expand()
            .BorderColor(Color.Grey);
    }

    /// <summary>
    /// Number of result rows to show, scaled to the current terminal height so a
    /// tall window shows many more items than a short one. Falls back to a fixed
    /// window when the height is unknown (e.g. redirected output).
    /// </summary>
    /// <returns>The maximum number of result rows to display.</returns>
    private int ResultsWindow()
    {
        var height = _console.Profile.Height;
        if (height <= 0)
        {
            return FallbackResultsWindow;
        }

        // Total height minus the progress panel and the results panel's own
        // border + header lines. The remainder is available for result rows.
        const int ResultsPanelChrome = 3; // top border + header, bottom border.
        var available = height - ProgressRegionHeight() - ResultsPanelChrome;
        return Math.Max(MinResultsWindow, available);
    }

    private sealed class PlanProgress(int total)
    {
        internal int Total { get; } = total;

        internal int Completed { get; set; }
    }

    /// <summary>An install item currently in flight.</summary>
    /// <param name="Name">Display name of the running item.</param>
    /// <param name="SourceType">The item's install source type.</param>
    private readonly record struct RunningItem(string Name, InstallSourceType SourceType);
}
