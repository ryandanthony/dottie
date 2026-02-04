// -----------------------------------------------------------------------
// <copyright file="ApplyProgressRenderer.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Models;
using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Linking;
using Spectre.Console;

namespace Dottie.Cli.Output;

/// <summary>
/// Default implementation of apply progress rendering using Spectre.Console.
/// </summary>
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
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            RenderInstallGroup(group);
        }

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

        AnsiConsole.MarkupLine($"  Total: {totalOperations} operations");
        AnsiConsole.MarkupLine($"    [green]✓[/] Success: {successCount}");
        AnsiConsole.MarkupLine($"    [yellow]○[/] Skipped: {skippedCount}");
        AnsiConsole.MarkupLine($"    [red]✗[/] Failed: {failedCount}");

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

    private static string GetSourceTypeName(InstallSourceType sourceType) => sourceType switch
    {
        InstallSourceType.GithubRelease => "GitHub Releases",
        InstallSourceType.AptPackage => "APT Packages",
        InstallSourceType.AptRepo => "APT Repositories",
        InstallSourceType.Script => "Shell Scripts",
        InstallSourceType.Font => "Fonts",
        InstallSourceType.SnapPackage => "Snap Packages",
        _ => "Other",
    };
}
