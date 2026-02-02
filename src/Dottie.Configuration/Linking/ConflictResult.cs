// -----------------------------------------------------------------------
// <copyright file="ConflictResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration.Linking;

/// <summary>
/// The result of conflict detection for a set of dotfile entries.
/// </summary>
public sealed record ConflictResult
{
    /// <summary>
    /// Gets a value indicating whether any conflicts were detected.
    /// </summary>
    /// <value>
    /// <placeholder>A value indicating whether any conflicts were detected.</placeholder>
    /// </value>
    public bool HasConflicts => Conflicts.Count > 0;

    /// <summary>
    /// Gets the list of conflicts detected.
    /// </summary>
    /// <value>
    /// <placeholder>The list of conflicts detected.</placeholder>
    /// </value>
    public required IReadOnlyList<Conflict> Conflicts { get; init; }

    /// <summary>
    /// Gets the list of dotfile entries that are safe to link (no conflicts).
    /// </summary>
    /// <remarks>
    /// Includes entries where the target doesn't exist or is already correctly linked.
    /// </remarks>
    /// <value>
    /// <placeholder>The list of dotfile entries that are safe to link (no conflicts).</placeholder>
    /// </value>
    public required IReadOnlyList<DotfileEntry> SafeEntries { get; init; }

    /// <summary>
    /// Gets the list of entries that are already correctly linked (skipped).
    /// </summary>
    /// <value>
    /// <placeholder>The list of entries that are already correctly linked (skipped).</placeholder>
    /// </value>
    public required IReadOnlyList<DotfileEntry> AlreadyLinked { get; init; }

    /// <summary>
    /// Creates a result with no conflicts.
    /// </summary>
    /// <param name="safeEntries">The safe entries to link.</param>
    /// <param name="alreadyLinked">The entries that are already linked.</param>
    /// <returns>A conflict result with no conflicts.</returns>
    public static ConflictResult NoConflicts(
        IReadOnlyList<DotfileEntry> safeEntries,
        IReadOnlyList<DotfileEntry> alreadyLinked) =>
        new()
        {
            Conflicts = [],
            SafeEntries = safeEntries,
            AlreadyLinked = alreadyLinked,
        };

    /// <summary>
    /// Creates a result with conflicts.
    /// </summary>
    /// <param name="conflicts">The detected conflicts.</param>
    /// <param name="safeEntries">The safe entries to link.</param>
    /// <param name="alreadyLinked">The entries that are already linked.</param>
    /// <returns>A conflict result with the specified conflicts.</returns>
    public static ConflictResult WithConflicts(
        IReadOnlyList<Conflict> conflicts,
        IReadOnlyList<DotfileEntry> safeEntries,
        IReadOnlyList<DotfileEntry> alreadyLinked) =>
        new()
        {
            Conflicts = conflicts,
            SafeEntries = safeEntries,
            AlreadyLinked = alreadyLinked,
        };
}
