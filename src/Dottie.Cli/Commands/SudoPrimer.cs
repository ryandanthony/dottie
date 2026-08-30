// -----------------------------------------------------------------------
// <copyright file="SudoPrimer.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using Spectre.Console;

namespace Dottie.Cli.Commands;

/// <summary>
/// Decides whether an up-front sudo prompt is needed for an install block and,
/// if so, prompts for credentials before any full-screen UI takes over the
/// terminal (so the password / fingerprint prompt is clearly visible).
/// </summary>
public sealed class SudoPrimer
{
    private readonly SudoChecker _sudoChecker;
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Initializes a new instance of the <see cref="SudoPrimer"/> class.
    /// </summary>
    /// <param name="sudoChecker">Sudo checker (injectable for tests). Defaults to a real checker.</param>
    /// <param name="console">Console for the prompt messages. Defaults to <see cref="AnsiConsole.Console"/>.</param>
    public SudoPrimer(SudoChecker? sudoChecker = null, IAnsiConsole? console = null)
    {
        _sudoChecker = sudoChecker ?? new SudoChecker();
        _console = console ?? AnsiConsole.Console;
    }

    /// <summary>
    /// Determines whether an interactive sudo prompt should be shown for the
    /// given install block: it must contain privileged items, sudo must be
    /// available, and credentials must not already be cached.
    /// </summary>
    /// <param name="installBlock">The resolved install block, or null.</param>
    /// <returns>True if the caller should prompt for sudo up front.</returns>
    public bool ShouldPrompt(InstallBlock? installBlock)
    {
        return SudoChecker.RequiresSudo(installBlock)
            && _sudoChecker.IsSudoAvailable()
            && !_sudoChecker.HasCachedCredentials();
    }

    /// <summary>
    /// Prompts for sudo credentials up front when <see cref="ShouldPrompt"/> is
    /// true, printing guidance and a warning if priming fails. No-op otherwise.
    /// </summary>
    /// <param name="installBlock">The resolved install block, or null.</param>
    public void PrimeIfNeeded(InstallBlock? installBlock)
    {
        if (!ShouldPrompt(installBlock))
        {
            return;
        }

        _console.MarkupLine("[yellow]This profile installs system packages that require sudo.[/]");
        _console.MarkupLine("[dim]Enter your password or use your fingerprint when prompted.[/]");

        if (!_sudoChecker.PrimeInteractive())
        {
            _console.MarkupLine("[yellow]⚠ Could not cache sudo credentials; privileged steps may be skipped or fail.[/]");
        }

        _console.WriteLine();
    }
}
