// -----------------------------------------------------------------------
// <copyright file="ConflictFormatter.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Linking;
using Spectre.Console;

namespace Dottie.Cli.Output;

/// <summary>
/// Formats conflict and backup information for console output.
/// </summary>
public static class ConflictFormatter
{
    /// <summary>
    /// Writes conflicts to the console.
    /// </summary>
    /// <param name="conflicts">The conflicts to display.</param>
    public static void WriteConflicts(IReadOnlyList<Conflict> conflicts)
    {
        ArgumentNullException.ThrowIfNull(conflicts);

        AnsiConsole.MarkupLine("[red]Error:[/] Conflicting files detected. Use --force to backup and overwrite.");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[yellow]Conflicts:[/]");
        foreach (var conflict in conflicts)
        {
            WriteConflict(conflict);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[red]Found {conflicts.Count} conflict(s).[/]");
    }

    /// <summary>
    /// Writes backup results to the console.
    /// </summary>
    /// <param name="results">The backup results to display.</param>
    public static void WriteBackupResults(IReadOnlyList<BackupResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var successful = results.Where(r => r.IsSuccess).ToList();
        var failed = results.Where(r => !r.IsSuccess).ToList();

        if (successful.Count > 0)
        {
            AnsiConsole.MarkupLine($"[green]Backed up {successful.Count} file(s):[/]");
            foreach (var backup in successful)
            {
                AnsiConsole.MarkupLine($"  [dim]•[/] {Markup.Escape(backup.OriginalPath)} → {Markup.Escape(backup.BackupPath!)}");
            }

            AnsiConsole.WriteLine();
        }

        if (failed.Count > 0)
        {
            AnsiConsole.MarkupLine($"[red]Failed to backup {failed.Count} file(s):[/]");
            foreach (var backup in failed)
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {Markup.Escape(backup.OriginalPath)}: {Markup.Escape(backup.Error!)}");
            }

            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Writes link operation results to the console.
    /// </summary>
    /// <param name="result">The link operation result to display.</param>
    public static void WriteLinkResults(LinkOperationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.SuccessfulLinks.Count > 0)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Created {result.SuccessfulLinks.Count} symlink(s).");
        }

        if (result.SkippedLinks.Count > 0)
        {
            AnsiConsole.MarkupLine($"[dim]Skipped {result.SkippedLinks.Count} file(s) (already linked).[/]");
        }

        if (result.FailedLinks.Count > 0)
        {
            AnsiConsole.MarkupLine($"[red]Failed to link {result.FailedLinks.Count} file(s):[/]");
            foreach (var link in result.FailedLinks)
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {Markup.Escape(link.ExpandedTargetPath)}: {Markup.Escape(link.Error!)}");
            }
        }
    }

    private static void WriteConflict(Conflict conflict)
    {
        var typeLabel = conflict.Type switch
        {
            ConflictType.File => "file",
            ConflictType.Directory => "directory",
            ConflictType.MismatchedSymlink => $"symlink → {Markup.Escape(conflict.ExistingTarget ?? "unknown")}",
            _ => "unknown",
        };

        AnsiConsole.MarkupLine($"  [yellow]•[/] {Markup.Escape(conflict.TargetPath)} [dim]({typeLabel})[/]");
    }
}
