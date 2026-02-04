// -----------------------------------------------------------------------
// <copyright file="SoftwareStatusEntry.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing;

namespace Dottie.Configuration.Status;

/// <summary>
/// Result of checking a single software item's installation status.
/// </summary>
/// <param name="ItemName">Display name for the software item.</param>
/// <param name="SourceType">The source type (GitHub, APT, Snap, Font, etc.).</param>
/// <param name="State">Current installation state.</param>
/// <param name="InstalledVersion">Currently installed version (if detectable).</param>
/// <param name="TargetVersion">Configured/pinned version (if specified).</param>
/// <param name="InstalledPath">Path where the software is installed.</param>
/// <param name="Message">Additional details (error message for Unknown state).</param>
public sealed record SoftwareStatusEntry(
    string ItemName,
    InstallSourceType SourceType,
    SoftwareInstallState State,
    string? InstalledVersion,
    string? TargetVersion,
    string? InstalledPath,
    string? Message);
