// -----------------------------------------------------------------------
// <copyright file="InstallStatus.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Installing;

/// <summary>
/// Status of an installation operation.
/// </summary>
public enum InstallStatus
{
    /// <summary>
    /// Item installed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Item already installed or not applicable.
    /// </summary>
    Skipped,

    /// <summary>
    /// Item skipped due to missing capability (e.g., no sudo).
    /// </summary>
    Warning,

    /// <summary>
    /// Item installation failed.
    /// </summary>
    Failed,
}
