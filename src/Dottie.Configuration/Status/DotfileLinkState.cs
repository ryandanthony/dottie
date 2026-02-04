// -----------------------------------------------------------------------
// <copyright file="DotfileLinkState.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Status;

/// <summary>
/// Represents the current state of a dotfile link.
/// </summary>
public enum DotfileLinkState
{
    /// <summary>
    /// Symlink exists and points to the correct source file.
    /// </summary>
    Linked,

    /// <summary>
    /// Target path does not exist (file not linked yet).
    /// </summary>
    Missing,

    /// <summary>
    /// Symlink exists but points to a non-existent file.
    /// </summary>
    Broken,

    /// <summary>
    /// A file or directory exists at target but is not the expected symlink.
    /// </summary>
    Conflicting,

    /// <summary>
    /// State cannot be determined (permission error, access issue).
    /// </summary>
    Unknown,
}
