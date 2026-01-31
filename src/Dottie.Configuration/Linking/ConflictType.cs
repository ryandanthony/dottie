// -----------------------------------------------------------------------
// <copyright file="ConflictType.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Linking;

/// <summary>
/// The type of conflict detected at a target path.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// No conflict - target doesn't exist or is already correctly linked.
    /// </summary>
    None = 0,

    /// <summary>
    /// Target path exists as a regular file.
    /// </summary>
    File = 1,

    /// <summary>
    /// Target path exists as a directory.
    /// </summary>
    Directory = 2,

    /// <summary>
    /// Target path is a symlink pointing to a different location than expected.
    /// </summary>
    MismatchedSymlink = 3,
}
