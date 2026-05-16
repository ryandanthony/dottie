// -----------------------------------------------------------------------
// <copyright file="InstallProgressRenderer.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Models;
using Dottie.Configuration.Installing;
using Spectre.Console;

namespace Dottie.Cli.Output;

/// <summary>
/// Implementation of install progress renderer using Spectre.Console.
/// </summary>
public sealed class InstallProgressRenderer : IInstallProgressRenderer
{
    /// <inheritdoc/>
    public void RenderProgress(InstallResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var icon = result.Status switch
        {
            InstallStatus.Success => "[green]✓[/]",
            InstallStatus.Failed => "[red]✗[/]",
            InstallStatus.Skipped => "[yellow]⊘[/]",
            InstallStatus.Warning => "[yellow]⚠[/]",
            _ => "[dim]?[/]",
        };

        var statusText = result.Status switch
        {
            InstallStatus.Success => $"[green]{result.Status}[/]",
            InstallStatus.Failed => $"[red]{result.Status}[/]",
            InstallStatus.Skipped => $"[yellow]{result.Status}[/]",
            InstallStatus.Warning => $"[yellow]{result.Status}[/]",
            _ => result.Status.ToString(),
        };

        var sourceType = $"[dim]({result.SourceType})[/]";
        var message = string.IsNullOrEmpty(result.Message) ? string.Empty : $" - {result.Message}";

        AnsiConsole.MarkupLine($"{icon} {result.ItemName} {statusText} {sourceType}{message}");
    }

    /// <inheritdoc/>
    public void RenderSummary(IEnumerable<InstallResult> results)
    {
        var resultList = results.ToList();
        if (resultList.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No items to install.[/]");
            return;
        }

        // Show all results first
        foreach (var result in resultList)
        {
            RenderProgress(result);
        }

        var summary = InstallPhaseResult.Executed(resultList);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Installation Summary:[/]");
        AnsiConsole.MarkupLine($"  Progress: [bold]{summary.CompletedCount}/{summary.TotalCount}[/] complete ([bold]{summary.CompletionPercentage}%[/])");
        AnsiConsole.MarkupLine($"  [green]✓ Installed/Updated:[/] {summary.InstalledCount}");
        AnsiConsole.MarkupLine($"  [yellow]⊘ Already current:[/] {summary.SkippedCount}");
        AnsiConsole.MarkupLine($"  [blue]→ Left to fix:[/] {summary.RemainingCount}");

        if (summary.FailedCount > 0)
        {
            AnsiConsole.MarkupLine($"  [red]✗ Failed:[/] {summary.FailedCount}");
        }

        if (summary.WarningCount > 0)
        {
            AnsiConsole.MarkupLine($"  [yellow]⚠ Warnings:[/] {summary.WarningCount}");
        }
    }

    /// <inheritdoc/>
    public void RenderError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {message}");
    }

    /// <inheritdoc/>
    public void RenderGroupedFailures(IEnumerable<InstallResult> results)
    {
        var failures = results.Where(r => r.Status == InstallStatus.Failed).ToList();
        if (failures.Count == 0)
        {
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold red]Failed Installations:[/]");

        // Group failures by source type
        var groupedFailures = failures.GroupBy(r => r.SourceType);

        foreach (var group in groupedFailures)
        {
            foreach (var failure in group)
            {
                var message = string.IsNullOrEmpty(failure.Message) ? "Unknown error" : failure.Message;
                AnsiConsole.MarkupLine($"  [dim][[{group.Key}]][/] {failure.ItemName}: {Markup.Escape(message)}");
            }
        }
    }
}
