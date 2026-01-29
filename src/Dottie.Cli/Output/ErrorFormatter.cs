// -----------------------------------------------------------------------
// <copyright file="ErrorFormatter.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Validation;
using Spectre.Console;

namespace Dottie.Cli.Output;

/// <summary>
/// Formats validation errors for console output.
/// </summary>
public static class ErrorFormatter
{
    /// <summary>
    /// Writes validation errors to the console.
    /// </summary>
    /// <param name="errors">The errors to display.</param>
    public static void WriteErrors(IReadOnlyList<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        AnsiConsole.MarkupLine("[red]Configuration validation failed:[/]");
        AnsiConsole.WriteLine();

        foreach (var error in errors)
        {
            WriteError(error);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[red]Found {errors.Count} error(s).[/]");
    }

    private static void WriteError(ValidationError error)
    {
        var locationPart = FormatLocation(error);
        AnsiConsole.MarkupLine($"  [red]•[/] {Markup.Escape(error.Message)}");

        if (!string.IsNullOrEmpty(locationPart))
        {
            AnsiConsole.MarkupLine($"    [dim]{Markup.Escape(locationPart)}[/]");
        }
    }

    private static string FormatLocation(ValidationError error)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(error.Path))
        {
            parts.Add(error.Path);
        }

        if (error.Line.HasValue)
        {
            parts.Add($"line {error.Line}");
            if (error.Column.HasValue)
            {
                parts.Add($"column {error.Column}");
            }
        }

        return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
    }
}
