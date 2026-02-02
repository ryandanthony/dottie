// -----------------------------------------------------------------------
// <copyright file="InstallSourceType.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Installing;

/// <summary>
/// Source type for installation, determining priority order (value = priority).
/// </summary>
public enum InstallSourceType
{
    /// <summary>
    /// Download from GitHub release (Priority 1).
    /// </summary>
    GithubRelease = 1,

    /// <summary>
    /// Install standard APT package (Priority 2).
    /// </summary>
    AptPackage = 2,

    /// <summary>
    /// Setup private APT repository (Priority 3).
    /// </summary>
    AptRepo = 3,

    /// <summary>
    /// Execute shell script (Priority 4).
    /// </summary>
    Script = 4,

    /// <summary>
    /// Install font (Priority 5).
    /// </summary>
    Font = 5,

    /// <summary>
    /// Install snap package (Priority 6).
    /// </summary>
    SnapPackage = 6,
}
