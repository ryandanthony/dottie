// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Spectre.Console;

namespace Dottie.Cli.Output;

/// <summary>
/// Renders installation progress and results to the console.
/// </summary>
public interface IInstallProgressRenderer
{
    /// <summary>
    /// Renders a single installation result.
    /// </summary>
    void RenderProgress(InstallResult result);

    /// <summary>
    /// Renders a summary of all installation results.
    /// </summary>
    void RenderSummary(IEnumerable<InstallResult> results);

    /// <summary>
    /// Renders an error message.
    /// </summary>
    void RenderError(string message);
}

/// <summary>
/// Implementation of install progress renderer using Spectre.Console.
/// </summary>
public sealed class InstallProgressRenderer : IInstallProgressRenderer
{
    /// <inheritdoc/>
    public void RenderProgress(InstallResult result)
    {
        var icon = result.Status switch
        {
            InstallStatus.Success => "[green]✓[/]",
            InstallStatus.Failed => "[red]✗[/]",
            InstallStatus.Skipped => "[yellow]⊘[/]",
            InstallStatus.Warning => "[yellow]⚠[/]",
            _ => "[dim]?[/]"
        };

        var statusText = result.Status switch
        {
            InstallStatus.Success => $"[green]{result.Status}[/]",
            InstallStatus.Failed => $"[red]{result.Status}[/]",
            InstallStatus.Skipped => $"[yellow]{result.Status}[/]",
            InstallStatus.Warning => $"[yellow]{result.Status}[/]",
            _ => result.Status.ToString()
        };

        var sourceType = $"[dim]({result.SourceType})[/]";
        var message = string.IsNullOrEmpty(result.Message) ? string.Empty : $" - {result.Message}";

        AnsiConsole.MarkupLine($"{icon} {result.ItemName} {statusText} {sourceType}{message}");
    }

    /// <inheritdoc/>
    public void RenderSummary(IEnumerable<InstallResult> results)
    {
        var resultList = results.ToList();
        if (!resultList.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No items to install.[/]");
            return;
        }

        var succeeded = resultList.Count(r => r.Status == InstallStatus.Success);
        var failed = resultList.Count(r => r.Status == InstallStatus.Failed);
        var skipped = resultList.Count(r => r.Status == InstallStatus.Skipped);
        var warnings = resultList.Count(r => r.Status == InstallStatus.Warning);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Installation Summary:[/]");
        if (succeeded > 0) AnsiConsole.MarkupLine($"  [green]✓ Succeeded:[/] {succeeded}");
        if (failed > 0) AnsiConsole.MarkupLine($"  [red]✗ Failed:[/] {failed}");
        if (skipped > 0) AnsiConsole.MarkupLine($"  [yellow]⊘ Skipped:[/] {skipped}");
        if (warnings > 0) AnsiConsole.MarkupLine($"  [yellow]⚠ Warnings:[/] {warnings}");
    }

    /// <inheritdoc/>
    public void RenderError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {message}");
    }
}
