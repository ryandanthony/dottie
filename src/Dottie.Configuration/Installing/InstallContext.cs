// -----------------------------------------------------------------------
// <copyright file="InstallContext.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Installing;

/// <summary>
/// Shared context passed to all installer services.
/// </summary>
public sealed record InstallContext
{
    /// <summary>
    /// Gets the absolute path to the repository root.
    /// </summary>
    /// <value>
    /// The absolute path to the repository root.
    /// </value>
    public required string RepoRoot { get; init; }

    /// <summary>
    /// Gets the target directory for binaries (default: ~/bin/).
    /// </summary>
    /// <value>
    /// The target directory for binaries.
    /// </value>
    public string BinDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "bin");

    /// <summary>
    /// Gets the target directory for fonts (default: ~/.local/share/fonts/).
    /// </summary>
    /// <value>
    /// The target directory for fonts.
    /// </value>
    public string FontDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local",
        "share",
        "fonts");

    /// <summary>
    /// Gets the optional GitHub API token from environment.
    /// </summary>
    /// <value>
    /// The GitHub API token, or null if not set.
    /// </value>
    public string? GithubToken { get; init; }

    /// <summary>
    /// Gets a value indicating whether sudo is available on the system.
    /// </summary>
    /// <value>
    /// True if sudo is available; otherwise, false.
    /// </value>
    public bool HasSudo { get; init; }

    /// <summary>
    /// Gets a value indicating whether to preview without making changes.
    /// </summary>
    /// <value>
    /// True if this is a dry run; otherwise, false.
    /// </value>
    public bool DryRun { get; init; }
}
