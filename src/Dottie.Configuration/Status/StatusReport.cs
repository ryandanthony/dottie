// -----------------------------------------------------------------------
// <copyright file="StatusReport.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Status;

/// <summary>
/// Aggregated status report for display.
/// </summary>
/// <param name="ProfileName">Name of the resolved profile.</param>
/// <param name="InheritanceChain">Chain of inherited profile names.</param>
/// <param name="DotfileStatuses">Status of all dotfiles.</param>
/// <param name="SoftwareStatuses">Status of all software items.</param>
public sealed record StatusReport(
    string ProfileName,
    IReadOnlyList<string> InheritanceChain,
    IReadOnlyList<DotfileStatusEntry> DotfileStatuses,
    IReadOnlyList<SoftwareStatusEntry> SoftwareStatuses)
{
    /// <summary>
    /// Gets a value indicating whether the profile has dotfile entries.
    /// </summary>
    public bool HasDotfiles => DotfileStatuses.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the profile has software items.
    /// </summary>
    public bool HasSoftware => SoftwareStatuses.Count > 0;

    /// <summary>
    /// Gets the number of dotfiles in Linked state.
    /// </summary>
    public int LinkedCount => DotfileStatuses.Count(s => s.State == DotfileLinkState.Linked);

    /// <summary>
    /// Gets the number of dotfiles in Missing state.
    /// </summary>
    public int MissingDotfilesCount => DotfileStatuses.Count(s => s.State == DotfileLinkState.Missing);

    /// <summary>
    /// Gets the number of dotfiles in Broken state.
    /// </summary>
    public int BrokenCount => DotfileStatuses.Count(s => s.State == DotfileLinkState.Broken);

    /// <summary>
    /// Gets the number of dotfiles in Conflicting state.
    /// </summary>
    public int ConflictingCount => DotfileStatuses.Count(s => s.State == DotfileLinkState.Conflicting);

    /// <summary>
    /// Gets the number of software items in Installed state.
    /// </summary>
    public int InstalledCount => SoftwareStatuses.Count(s => s.State == SoftwareInstallState.Installed);

    /// <summary>
    /// Gets the number of software items in Missing state.
    /// </summary>
    public int MissingSoftwareCount => SoftwareStatuses.Count(s => s.State == SoftwareInstallState.Missing);

    /// <summary>
    /// Gets the number of software items in Outdated state.
    /// </summary>
    public int OutdatedCount => SoftwareStatuses.Count(s => s.State == SoftwareInstallState.Outdated);
}
