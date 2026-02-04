# API Contract: ApplyProgressRenderer

**Feature**: 009-cli-apply  
**Date**: February 3, 2026

## Overview

The `ApplyProgressRenderer` renders verbose output showing every operation performed during apply, per FR-010.

## Interface

```csharp
/// <summary>
/// Renders progress and summary output for the apply command.
/// </summary>
public interface IApplyProgressRenderer
{
    /// <summary>
    /// Renders the dry-run preview for apply.
    /// </summary>
    /// <param name="profile">The resolved profile being previewed.</param>
    /// <param name="repoRoot">The repository root path.</param>
    void RenderDryRunPreview(ResolvedProfile profile, string repoRoot);

    /// <summary>
    /// Renders the verbose summary of all apply operations.
    /// </summary>
    /// <param name="result">The aggregated apply result.</param>
    /// <param name="profileName">The name of the applied profile.</param>
    void RenderVerboseSummary(ApplyResult result, string profileName);

    /// <summary>
    /// Renders an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    void RenderError(string message);
}
```

## Implementation

```csharp
/// <summary>
/// Default implementation of apply progress rendering using Spectre.Console.
/// </summary>
public sealed class ApplyProgressRenderer : IApplyProgressRenderer
{
    /// <inheritdoc/>
    public void RenderDryRunPreview(ResolvedProfile profile, string repoRoot)
    {
        AnsiConsole.MarkupLine("[yellow]Dry Run Mode:[/] Previewing apply operations\n");

        RenderDryRunLinkPreview(profile, repoRoot);
        RenderDryRunInstallPreview(profile);
    }

    /// <inheritdoc/>
    public void RenderVerboseSummary(ApplyResult result, string profileName)
    {
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

    private void RenderDryRunLinkPreview(ResolvedProfile profile, string repoRoot);
    private void RenderDryRunInstallPreview(ResolvedProfile profile);
    private void RenderLinkPhaseSummary(LinkPhaseResult linkPhase);
    private void RenderInstallPhaseSummary(InstallPhaseResult installPhase);
    private void RenderOverallSummary(ApplyResult result);
}
```

## Output Format

### Dry-Run Output

```text
Dry Run Mode: Previewing apply operations

── Dotfiles ──────────────────────────────────────────────────────
  [green]✓[/] Would link: ~/.bashrc → dotfiles/bashrc
  [green]✓[/] Would link: ~/.gitconfig → dotfiles/gitconfig
  [yellow]○[/] Already linked: ~/.vimrc → dotfiles/vimrc
  [red]✗[/] Conflict: ~/.zshrc (existing file)

── Software Installation ──────────────────────────────────────────
  [dim]GitHub Releases:[/]
    • fzf (junegunn/fzf)
    • ripgrep (BurntSushi/ripgrep)
  
  [dim]APT Packages:[/]
    • build-essential
    • curl
    • git

  [dim]Fonts:[/]
    • FiraCode Nerd Font
```

### Verbose Summary Output

```text
─────────────────────── Apply Summary: work ───────────────────────

── Link Phase ────────────────────────────────────────────────────
  [green]✓ Created[/]     ~/.bashrc → dotfiles/bashrc
  [green]✓ Created[/]     ~/.gitconfig → dotfiles/gitconfig
  [yellow]○ Skipped[/]     ~/.vimrc (already linked)
  [blue]↻ Backed up[/]   ~/.zshrc → ~/.zshrc.bak.20260203-143022
  [green]✓ Created[/]     ~/.zshrc → dotfiles/zshrc

── Install Phase ─────────────────────────────────────────────────
  [dim]GitHub Releases[/]
    [green]✓ Installed[/]  fzf v0.45.0
    [yellow]○ Skipped[/]    ripgrep (already installed)
  
  [dim]APT Packages[/]
    [green]✓ Installed[/]  build-essential
    [green]✓ Installed[/]  curl
    [yellow]○ Skipped[/]    git (already installed)
  
  [dim]Fonts[/]
    [green]✓ Installed[/]  FiraCode Nerd Font

── Overall ───────────────────────────────────────────────────────
  Total: 9 operations
    [green]✓[/] Success: 6
    [yellow]○[/] Skipped: 3
    [red]✗[/] Failed: 0

[green]Apply completed successfully.[/]
```

### Failure Summary Output

When failures occur, append failure details:

```text
── Failures ──────────────────────────────────────────────────────
  [red]✗[/] ripgrep: Download failed - connection timeout
  [red]✗[/] nodejs: APT package not found

[red]Apply completed with 2 failures.[/]
```

## Status Icons

| Icon | Meaning |
|------|---------|
| `✓` (green) | Success - operation completed |
| `○` (yellow) | Skipped - already in desired state |
| `✗` (red) | Failed - operation error |
| `↻` (blue) | Backed up - file preserved before overwrite |
