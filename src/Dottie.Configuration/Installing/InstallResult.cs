// -----------------------------------------------------------------------
// <copyright file="InstallResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Installing;

/// <summary>
/// Result of a single installation operation.
/// </summary>
public sealed record InstallResult
{
    /// <summary>
    /// Gets the identifier for the item (e.g., repo name, package name).
    /// </summary>
    /// <value>
    /// The identifier for the item.
    /// </value>
    public required string ItemName { get; init; }

    /// <summary>
    /// Gets the source type that processed this item.
    /// </summary>
    /// <value>
    /// The source type that processed this item.
    /// </value>
    public required InstallSourceType SourceType { get; init; }

    /// <summary>
    /// Gets the status of the installation.
    /// </summary>
    /// <value>
    /// The status of the installation.
    /// </value>
    public required InstallStatus Status { get; init; }

    /// <summary>
    /// Gets additional details (version installed, skip reason, error).
    /// </summary>
    /// <value>
    /// Additional details for the installation.
    /// </value>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the path where the item was installed, if applicable.
    /// </summary>
    /// <value>
    /// The path where the item was installed.
    /// </value>
    public string? InstalledPath { get; init; }

    /// <summary>
    /// Creates a successful installation result.
    /// </summary>
    /// <param name="name">The item name or identifier.</param>
    /// <param name="source">The installation source type.</param>
    /// <param name="path">Optional path where the item was installed.</param>
    /// <param name="message">Optional additional details (e.g., version).</param>
    /// <returns>A successful InstallResult.</returns>
    public static InstallResult Success(string name, InstallSourceType source, string? path = null, string? message = null)
        => new()
        {
            ItemName = name,
            SourceType = source,
            Status = InstallStatus.Success,
            InstalledPath = path,
            Message = message,
        };

    /// <summary>
    /// Creates a skipped installation result.
    /// </summary>
    /// <param name="name">The item name or identifier.</param>
    /// <param name="source">The installation source type.</param>
    /// <param name="reason">The reason why the item was skipped.</param>
    /// <returns>A skipped InstallResult.</returns>
    public static InstallResult Skipped(string name, InstallSourceType source, string reason)
        => new()
        {
            ItemName = name,
            SourceType = source,
            Status = InstallStatus.Skipped,
            Message = reason,
        };

    /// <summary>
    /// Creates a warning installation result.
    /// </summary>
    /// <param name="name">The item name or identifier.</param>
    /// <param name="source">The installation source type.</param>
    /// <param name="reason">The reason for the warning (e.g., missing capability).</param>
    /// <returns>A warning InstallResult.</returns>
    public static InstallResult Warning(string name, InstallSourceType source, string reason)
        => new()
        {
            ItemName = name,
            SourceType = source,
            Status = InstallStatus.Warning,
            Message = reason,
        };

    /// <summary>
    /// Creates a failed installation result.
    /// </summary>
    /// <param name="name">The item name or identifier.</param>
    /// <param name="source">The installation source type.</param>
    /// <param name="error">The error message describing why the installation failed.</param>
    /// <returns>A failed InstallResult.</returns>
    public static InstallResult Failed(string name, InstallSourceType source, string error)
        => new()
        {
            ItemName = name,
            SourceType = source,
            Status = InstallStatus.Failed,
            Message = error,
        };
}
