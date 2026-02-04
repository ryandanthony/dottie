// -----------------------------------------------------------------------
// <copyright file="SoftwareInstallState.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Status;

/// <summary>
/// Represents the current installation state of a software item.
/// </summary>
public enum SoftwareInstallState
{
    /// <summary>
    /// Software is installed and version matches (if pinned).
    /// </summary>
    Installed,

    /// <summary>
    /// Software is not installed.
    /// </summary>
    Missing,

    /// <summary>
    /// Software is installed but version doesn't match pinned version.
    /// </summary>
    Outdated,

    /// <summary>
    /// State cannot be determined (detection error).
    /// </summary>
    Unknown,
}
