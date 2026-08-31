// -----------------------------------------------------------------------
// <copyright file="ApplyProgressRenderer.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Commands;
using Dottie.Cli.Models;
using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Linking;
using Spectre.Console;

namespace Dottie.Cli.Output;

/// <summary>
/// Default implementation of apply progress rendering using Spectre.Console.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Major Code Smell",
    "S1200:Classes should not be coupled to too many other classes (Single Responsibility Principle)",
    Justification = "A progress renderer inherently composes many Spectre widgets and result/phase model types.")]
public sealed class ApplyProgressRenderer : IApplyProgressRenderer
{
    /// <inheritdoc/>
    public void RenderDryRunPreview(ResolvedProfile profile, string repoRoot)
    {
        ArgumentNullException.ThrowIfNull(profile);

        AnsiConsole.MarkupLine("[yellow]Dry Run Mode:[/] Previewing apply operations\n");

        RenderDryRunLinkPreview(profile, repoRoot);
        RenderDryRunInstallPreview(profile);
    }

    /// <inheritdoc/>
    public void RenderVerboseSummary(ApplyResult result, string profileName)
    {
        ArgumentNullException.ThrowIfNull(result);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[blue]Apply Summary: {profileName}[/]"));
        AnsiConsole.WriteLine();

        if (result.LinkPhase.WasExecuted)
        {
            RenderLinkPhaseSummary(result.LinkPhase);
        }

        if (result.InstallPhase.WasExecuted)
        {
            RenderInstallPhaseSummary(result.InstallPhase);
        }

        RenderOverallSummary(result);
    }

    /// <inheritdoc/>
    public void RenderErrorsOnly(ApplyResult result, string profileName)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.OverallSuccess)
        {
            // Everything succeeded and the live dashboard already showed it.
            // Print nothing so the terminal stays clean.
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[red]Apply Failed: {profileName}[/]"));
        AnsiConsole.WriteLine();

        RenderLinkFailures(result.LinkPhase);
        RenderInstallFailures(result.InstallPhase);
    }

    private static void RenderLinkFailures(LinkPhaseResult linkPhase)
    {
        if (linkPhase.WasBlocked)
        {
            AnsiConsole.MarkupLine("[red]✗ Linking blocked by conflicts (use --force to override):[/]");
            var conflicts = linkPhase.ExecutionResult?.ConflictResult?.Conflicts;
            if (conflicts != null)
            {
                foreach (var conflict in conflicts)
                {
                    AnsiConsole.MarkupLine($"    [red]✗[/] {Markup.Escape(conflict.Entry.Target)}");
                }
            }

            AnsiConsole.WriteLine();
        }

        var failedLinks = linkPhase.ExecutionResult?.LinkResult?.FailedLinks;
        if (failedLinks is { Count: > 0 })
        {
            AnsiConsole.MarkupLine("[red]✗ Failed links:[/]");
            foreach (var fail in failedLinks)
            {
                AnsiConsole.MarkupLine($"    [red]✗[/] {Markup.Escape(fail.ExpandedTargetPath)}: {Markup.Escape(fail.Error ?? "Unknown error")}");
            }

            AnsiConsole.WriteLine();
        }
    }

    private static void RenderInstallFailures(InstallPhaseResult installPhase)
    {
        var failures = installPhase.Results.Where(r => r.Status == InstallStatus.Failed).ToList();
        if (failures.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[red]✗ Failed installs:[/]");
        foreach (var failure in failures)
        {
            var source = InstallerProgressHelper.GetSourceTypeName(failure.SourceType);
            var message = string.IsNullOrEmpty(failure.Message) ? "Unknown error" : failure.Message;
            AnsiConsole.MarkupLine($"    [red]✗[/] {Markup.Escape(failure.ItemName)} [dim]({source})[/]: {Markup.Escape(message)}");
        }

        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void RenderError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {message}");
    }

    private static void RenderDryRunLinkPreview(ResolvedProfile profile, string repoRoot)
    {
        if (profile.Dotfiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No dotfiles configured.[/]\n");
            return;
        }

        AnsiConsole.Write(new Rule("[dim]Dotfiles[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var conflictDetector = new ConflictDetector();
        var dotfiles = profile.Dotfiles.ToList().AsReadOnly();
        var conflictResult = conflictDetector.DetectConflicts(dotfiles, repoRoot);

        foreach (var entry in conflictResult.SafeEntries)
        {
            var target = entry.Target;
            var source = Path.Combine(repoRoot, entry.Source);
            AnsiConsole.MarkupLine($"  [green]✓[/] Would link: {target} → {source}");
        }

        foreach (var target in conflictResult.AlreadyLinked.Select(e => e.Target))
        {
            AnsiConsole.MarkupLine($"  [yellow]○[/] Already linked: {target}");
        }

        foreach (var conflict in conflictResult.Conflicts)
        {
            var target = conflict.Entry.Target;
            AnsiConsole.MarkupLine($"  [red]✗[/] Conflict: {target} (existing file)");
        }

        AnsiConsole.WriteLine();
    }

    private static void RenderDryRunInstallPreview(ResolvedProfile profile)
    {
        if (profile.Install is null)
        {
            AnsiConsole.MarkupLine("[dim]No software installation configured.[/]\n");
            return;
        }

        AnsiConsole.Write(new Rule("[dim]Software Installation[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var installBlock = profile.Install;

        RenderDryRunGithubReleases(installBlock);
        RenderDryRunAptPackages(installBlock);
        RenderDryRunAptRepos(installBlock);
        RenderDryRunScripts(installBlock);
        RenderDryRunFonts(installBlock);
        RenderDryRunSnapPackages(installBlock);

        AnsiConsole.WriteLine();
    }

    private static void RenderDryRunGithubReleases(Configuration.Models.InstallBlocks.InstallBlock installBlock)
    {
        if (installBlock.Github.Count > 0)
        {
            AnsiConsole.MarkupLine("  [dim]GitHub Releases:[/]");
            foreach (var item in installBlock.Github)
            {
                AnsiConsole.MarkupLine($"    • {item.Repo}");
            }
        }
    }

    private static void RenderDryRunAptPackages(Configuration.Models.InstallBlocks.InstallBlock installBlock)
    {
        if (installBlock.Apt.Count > 0)
        {
            AnsiConsole.MarkupLine("  [dim]APT Packages:[/]");
            foreach (var pkg in installBlock.Apt)
            {
                AnsiConsole.MarkupLine($"    • {pkg}");
            }
        }
    }

    private static void RenderDryRunAptRepos(Configuration.Models.InstallBlocks.InstallBlock installBlock)
    {
        if (installBlock.AptRepos.Count > 0)
        {
            AnsiConsole.MarkupLine("  [dim]APT Repositories:[/]");
            foreach (var repo in installBlock.AptRepos)
            {
                AnsiConsole.MarkupLine($"    • {repo.Name}");
            }
        }
    }

    private static void RenderDryRunScripts(Configuration.Models.InstallBlocks.InstallBlock installBlock)
    {
        if (installBlock.Scripts.Count > 0)
        {
            AnsiConsole.MarkupLine("  [dim]Shell Scripts:[/]");
            foreach (var script in installBlock.Scripts)
            {
                AnsiConsole.MarkupLine($"    • {script}");
            }
        }
    }

    private static void RenderDryRunFonts(Configuration.Models.InstallBlocks.InstallBlock installBlock)
    {
        if (installBlock.Fonts.Count > 0)
        {
            AnsiConsole.MarkupLine("  [dim]Fonts:[/]");
            foreach (var font in installBlock.Fonts)
            {
                AnsiConsole.MarkupLine($"    • {font.Name}");
            }
        }
    }

    private static void RenderDryRunSnapPackages(Configuration.Models.InstallBlocks.InstallBlock installBlock)
    {
        if (installBlock.Snaps.Count > 0)
        {
            AnsiConsole.MarkupLine("  [dim]Snap Packages:[/]");
            foreach (var pkg in installBlock.Snaps)
            {
                AnsiConsole.MarkupLine($"    • {pkg.Name}");
            }
        }
    }

    private static void RenderLinkPhaseSummary(LinkPhaseResult linkPhase)
    {
        AnsiConsole.Write(new Rule("[dim]Link Phase[/]").LeftJustified());
        AnsiConsole.WriteLine();

        if (linkPhase.WasBlocked)
        {
            RenderBlockedLinkPhase(linkPhase);
            return;
        }

        RenderCompletedLinkPhase(linkPhase);
    }

    private static void RenderBlockedLinkPhase(LinkPhaseResult linkPhase)
    {
        AnsiConsole.MarkupLine("  [red]✗ Blocked[/] - Conflicts prevented linking (use --force to override)");
        if (linkPhase.ExecutionResult?.ConflictResult?.Conflicts != null)
        {
            foreach (var conflict in linkPhase.ExecutionResult.ConflictResult.Conflicts)
            {
                var target = conflict.Entry.Target;
                AnsiConsole.MarkupLine($"    [red]✗[/] {target}");
            }
        }

        AnsiConsole.WriteLine();
    }

    private static void RenderCompletedLinkPhase(LinkPhaseResult linkPhase)
    {
        var linkResult = linkPhase.ExecutionResult?.LinkResult;
        var backups = linkPhase.ExecutionResult?.BackupResults ?? [];

        if (linkResult is null)
        {
            return;
        }

        foreach (var link in linkResult.SuccessfulLinks)
        {
            AnsiConsole.MarkupLine($"  [green]✓ Created[/]     {link.ExpandedTargetPath} → {link.Entry.Source}");
        }

        foreach (var skip in linkResult.SkippedLinks)
        {
            AnsiConsole.MarkupLine($"  [yellow]○ Skipped[/]     {skip.ExpandedTargetPath} (already linked)");
        }

        foreach (var backup in backups)
        {
            AnsiConsole.MarkupLine($"  [blue]↻ Backed up[/]   {backup.OriginalPath} → {backup.BackupPath}");
        }

        foreach (var fail in linkResult.FailedLinks)
        {
            AnsiConsole.MarkupLine($"  [red]✗ Failed[/]      {fail.ExpandedTargetPath}: {fail.Error}");
        }

        AnsiConsole.WriteLine();
    }

    private static void RenderInstallPhaseSummary(InstallPhaseResult installPhase)
    {
        if (!installPhase.WasExecuted || installPhase.Results.Count == 0)
        {
            return;
        }

        AnsiConsole.Write(new Rule("[dim]Install Phase[/]").LeftJustified());
        AnsiConsole.WriteLine();

        // Group by source type
        var grouped = installPhase.Results
            .GroupBy(r => r.SourceType)
            .OrderBy(g => InstallerProgressHelper.GetSourceTypeOrder(g.Key));

        foreach (var group in grouped)
        {
            RenderInstallGroup(group);
        }

        AnsiConsole.MarkupLine($"  Progress: [bold]{installPhase.CompletedCount}/{installPhase.TotalCount}[/] complete ([bold]{installPhase.CompletionPercentage}%[/])");
        AnsiConsole.MarkupLine($"  [green]✓ Installed/Updated:[/] {installPhase.InstalledCount}");
        AnsiConsole.MarkupLine($"  [yellow]○ Already current:[/] {installPhase.SkippedCount}");
        AnsiConsole.MarkupLine($"  [blue]→ Left to fix:[/] {installPhase.RemainingCount}");
        AnsiConsole.WriteLine();
    }

    private static void RenderInstallGroup(IGrouping<InstallSourceType, InstallResult> group)
    {
        var sourceTypeName = GetSourceTypeName(group.Key);
        AnsiConsole.MarkupLine($"  [dim]{sourceTypeName}[/]");

        foreach (var result in group)
        {
            var (icon, color) = result.Status switch
            {
                InstallStatus.Success => ("✓", "green"),
                InstallStatus.Skipped => ("○", "yellow"),
                InstallStatus.Warning => ("⚠", "yellow"),
                InstallStatus.Failed => ("✗", "red"),
                _ => ("?", "dim"),
            };

            var statusText = result.Status switch
            {
                InstallStatus.Success => "Installed",
                InstallStatus.Skipped => "Skipped",
                InstallStatus.Warning => "Warning",
                InstallStatus.Failed => "Failed",
                _ => "Unknown",
            };

            var message = string.IsNullOrEmpty(result.Message) ? string.Empty : $" ({result.Message})";
            AnsiConsole.MarkupLine($"    [{color}]{icon} {statusText}[/]  {result.ItemName}{message}");
        }
    }

    private static void RenderOverallSummary(ApplyResult result)
    {
        AnsiConsole.Write(new Rule("[dim]Overall[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var linkSuccesses = result.LinkPhase.ExecutionResult?.LinkResult?.SuccessfulLinks.Count ?? 0;
        var linkSkipped = result.LinkPhase.ExecutionResult?.LinkResult?.SkippedLinks.Count ?? 0;
        var linkFailed = result.LinkPhase.ExecutionResult?.LinkResult?.FailedLinks.Count ?? 0;
        var linkTotal = linkSuccesses + linkSkipped + linkFailed;

        var installSuccesses = result.InstallPhase.Results.Count(r => r.Status == InstallStatus.Success);
        var installSkipped = result.InstallPhase.Results.Count(r => r.Status == InstallStatus.Skipped);
        var installFailed = result.InstallPhase.Results.Count(r => r.Status == InstallStatus.Failed);
        var installTotal = result.InstallPhase.Results.Count;

        var totalOperations = linkTotal + installTotal;
        var successCount = linkSuccesses + installSuccesses;
        var skippedCount = linkSkipped + installSkipped;
        var failedCount = linkFailed + installFailed;

        AnsiConsole.MarkupLine($"  Progress: [bold]{result.CompletedOperationCount}/{result.TotalOperationCount}[/] complete ([bold]{result.CompletionPercentage}%[/])");
        AnsiConsole.MarkupLine($"  Total: {totalOperations} operations");
        AnsiConsole.MarkupLine($"    [green]✓[/] Success: {successCount}");
        AnsiConsole.MarkupLine($"    [yellow]○[/] Skipped: {skippedCount}");
        AnsiConsole.MarkupLine($"    [red]✗[/] Failed: {failedCount}");
        AnsiConsole.MarkupLine($"    [blue]→[/] Left: {result.RemainingOperationCount}");

        AnsiConsole.WriteLine();

        if (result.OverallSuccess)
        {
            AnsiConsole.MarkupLine("[green]Apply completed successfully.[/]");
        }
        else if (result.LinkPhase.WasBlocked)
        {
            AnsiConsole.MarkupLine("[red]Apply blocked by conflicts. Use --force to override.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Apply completed with failures.[/]");
        }
    }

    private static string GetSourceTypeName(InstallSourceType sourceType) => InstallerProgressHelper.GetSourceTypeName(sourceType);
}
