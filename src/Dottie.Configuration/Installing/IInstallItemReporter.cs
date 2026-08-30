// -----------------------------------------------------------------------
// <copyright file="IInstallItemReporter.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Installing;

/// <summary>
/// Receives lifecycle callbacks for individual install items so a UI can show
/// what is currently running and update it once each item finishes.
/// Implementations must be thread-safe: installers may process items in parallel.
/// </summary>
public interface IInstallItemReporter
{
    /// <summary>
    /// Called just before work begins on an item.
    /// </summary>
    /// <param name="itemName">Display name of the item starting.</param>
    /// <param name="sourceType">The install source type of the item.</param>
    void ItemStarted(string itemName, InstallSourceType sourceType);

    /// <summary>
    /// Called as soon as an item finishes, with its result.
    /// </summary>
    /// <param name="result">The completed item's result.</param>
    void ItemCompleted(InstallResult result);
}
