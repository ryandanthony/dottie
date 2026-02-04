// -----------------------------------------------------------------------
// <copyright file="StatusFormatter.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Status;
using Spectre.Console;

namespace Dottie.Cli.Output;

/// <summary>
/// Formats status output for console display.
/// </summary>
public static class StatusFormatter
{
    /// <summary>
    /// Writes the full status report to the console.
    /// </summary>
    /// <param name="report">The status report to display.</param>
    public static void WriteStatusReport(StatusReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        WriteProfileHeader(report);
        WriteDotfileSection(report.DotfileStatuses, report.HasDotfiles);
        WriteSoftwareSection(report.SoftwareStatuses, report.HasSoftware);
        WriteSummary(report);
    }

    /// <summary>
    /// Writes the profile header showing name and inheritance chain.
    /// </summary>
    /// <param name="report">The status report.</param>
    public static void WriteProfileHeader(StatusReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.InheritanceChain.Count > 1)
        {
            // Chain is [ancestor, ..., descendant], show parents (all except last)
            var parents = report.InheritanceChain.Take(report.InheritanceChain.Count - 1);
            var inheritedFrom = string.Join(" → ", parents);
            AnsiConsole.MarkupLine($"Profile: [bold]{report.ProfileName}[/] (inherited from: {inheritedFrom})");
        }
        else
        {
            AnsiConsole.MarkupLine($"Profile: [bold]{report.ProfileName}[/]");
        }

        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Writes the dotfiles section with status table.
    /// </summary>
    /// <param name="statuses">The dotfile status entries.</param>
    /// <param name="hasItems">Whether there are any dotfile items configured.</param>
    public static void WriteDotfileSection(IReadOnlyList<DotfileStatusEntry> statuses, bool hasItems)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        AnsiConsole.MarkupLine("[bold]═══ Dotfiles ═══[/]");

        if (!hasItems)
        {
            AnsiConsole.MarkupLine("[dim]No dotfiles configured for this profile.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("Source").NoWrap())
            .AddColumn(new TableColumn("Status").Centered())
            .AddColumn(new TableColumn("Target"));

        foreach (var entry in statuses)
        {
            var statusText = FormatDotfileStatus(entry.State);
            table.AddRow(
                entry.Entry.Source,
                statusText,
                FormatTarget(entry.ExpandedTarget, entry.Message));
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Writes the software section with status table.
    /// </summary>
    /// <param name="statuses">The software status entries.</param>
    /// <param name="hasItems">Whether there are any software items configured.</param>
    public static void WriteSoftwareSection(IReadOnlyList<SoftwareStatusEntry> statuses, bool hasItems)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        AnsiConsole.MarkupLine("[bold]═══ Software ═══[/]");

        if (!hasItems)
        {
            AnsiConsole.MarkupLine("[dim]No software configured for this profile.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("Item").NoWrap())
            .AddColumn(new TableColumn("Status").Centered())
            .AddColumn(new TableColumn("Details"));

        foreach (var entry in statuses)
        {
            var itemLabel = $"[[{entry.SourceType}]] {entry.ItemName}";
            var statusText = FormatSoftwareStatus(entry.State);
            var details = FormatSoftwareDetails(entry);
            table.AddRow(itemLabel, statusText, details);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Writes the summary line.
    /// </summary>
    /// <param name="report">The status report.</param>
    public static void WriteSummary(StatusReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var dotfileSummary = BuildDotfileSummary(report);
        var softwareSummary = BuildSoftwareSummary(report);

        if (!string.IsNullOrEmpty(dotfileSummary) && !string.IsNullOrEmpty(softwareSummary))
        {
            AnsiConsole.MarkupLine($"Summary: {dotfileSummary} | {softwareSummary}");
        }
        else if (!string.IsNullOrEmpty(dotfileSummary))
        {
            AnsiConsole.MarkupLine($"Summary: {dotfileSummary}");
        }
        else if (!string.IsNullOrEmpty(softwareSummary))
        {
            AnsiConsole.MarkupLine($"Summary: {softwareSummary}");
        }
        else
        {
            // No items to summarize
        }
    }

    private static string FormatDotfileStatus(DotfileLinkState state)
    {
        return state switch
        {
            DotfileLinkState.Linked => "[green]✓ Linked[/]",
            DotfileLinkState.Missing => "[yellow]○ Missing[/]",
            DotfileLinkState.Broken => "[red]⚠ Broken[/]",
            DotfileLinkState.Conflicting => "[red]✗ Conflict[/]",
            DotfileLinkState.Unknown => "[dim]? Unknown[/]",
            _ => "[dim]?[/]",
        };
    }

    private static string FormatTarget(string expandedTarget, string? message)
    {
        var shortTarget = ShortenPath(expandedTarget);

        if (!string.IsNullOrEmpty(message))
        {
            return $"{shortTarget} ({Markup.Escape(message)})";
        }

        return shortTarget;
    }

    private static string FormatSoftwareStatus(SoftwareInstallState state)
    {
        return state switch
        {
            SoftwareInstallState.Installed => "[green]✓ Installed[/]",
            SoftwareInstallState.Missing => "[yellow]○ Missing[/]",
            SoftwareInstallState.Outdated => "[blue]⬆ Outdated[/]",
            SoftwareInstallState.Unknown => "[dim]? Unknown[/]",
            _ => "[dim]?[/]",
        };
    }

    private static string FormatSoftwareDetails(SoftwareStatusEntry entry)
    {
        if (entry.State == SoftwareInstallState.Outdated &&
            !string.IsNullOrEmpty(entry.InstalledVersion) &&
            !string.IsNullOrEmpty(entry.TargetVersion))
        {
            return $"{entry.InstalledVersion} → {entry.TargetVersion}";
        }

        if (!string.IsNullOrEmpty(entry.InstalledPath))
        {
            return ShortenPath(entry.InstalledPath);
        }

        if (!string.IsNullOrEmpty(entry.Message))
        {
            return Markup.Escape(entry.Message);
        }

        return string.Empty;
    }

    private static string BuildDotfileSummary(StatusReport report)
    {
        if (!report.HasDotfiles)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (report.LinkedCount > 0)
        {
            parts.Add($"[green]{report.LinkedCount} linked[/]");
        }

        if (report.MissingDotfilesCount > 0)
        {
            parts.Add($"[yellow]{report.MissingDotfilesCount} missing[/]");
        }

        if (report.ConflictingCount > 0)
        {
            parts.Add($"[red]{report.ConflictingCount} conflict[/]");
        }

        if (report.BrokenCount > 0)
        {
            parts.Add($"[red]{report.BrokenCount} broken[/]");
        }

        return string.Join(", ", parts);
    }

    private static string BuildSoftwareSummary(StatusReport report)
    {
        if (!report.HasSoftware)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (report.InstalledCount > 0)
        {
            parts.Add($"[green]{report.InstalledCount} installed[/]");
        }

        if (report.MissingSoftwareCount > 0)
        {
            parts.Add($"[yellow]{report.MissingSoftwareCount} missing[/]");
        }

        if (report.OutdatedCount > 0)
        {
            parts.Add($"[blue]{report.OutdatedCount} outdated[/]");
        }

        return string.Join(", ", parts);
    }

    private static string ShortenPath(string path)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (path.StartsWith(home, StringComparison.OrdinalIgnoreCase))
        {
            return "~" + path[home.Length..];
        }

        return path;
    }
}
