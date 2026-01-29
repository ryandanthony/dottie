// -----------------------------------------------------------------------
// <copyright file="ProfileResolveResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration;

/// <summary>
/// Result of a profile resolution operation.
/// </summary>
public sealed record ProfileResolveResult
{
    /// <summary>
    /// Gets the resolved profile if successful.
    /// </summary>
    /// <value>
    /// The resolved profile if successful.
    /// </value>
    public ConfigProfile? Profile { get; init; }

    /// <summary>
    /// Gets the error message if the resolution failed.
    /// </summary>
    /// <value>
    /// The error message if the resolution failed.
    /// </value>
    public string? Error { get; init; }

    /// <summary>
    /// Gets the list of available profile names (provided when resolution fails).
    /// </summary>
    /// <value>
    /// The list of available profile names (provided when resolution fails).
    /// </value>
    public IReadOnlyList<string> AvailableProfiles { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the resolution was successful.
    /// </summary>
    /// <value>
    /// A value indicating whether the resolution was successful.
    /// </value>
    public bool IsSuccess => Profile is not null && Error is null;

    /// <summary>
    /// Creates a successful result with the resolved profile.
    /// </summary>
    /// <param name="profile">The resolved profile.</param>
    /// <returns>A successful result.</returns>
    public static ProfileResolveResult Success(ConfigProfile profile)
    {
        return new ProfileResolveResult { Profile = profile };
    }

    /// <summary>
    /// Creates a failure result with an error message and available profile names.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="availableProfiles">The list of available profile names.</param>
    /// <returns>A failure result.</returns>
    public static ProfileResolveResult Failure(string error, IReadOnlyList<string> availableProfiles)
    {
        return new ProfileResolveResult
        {
            Error = error,
            AvailableProfiles = availableProfiles,
        };
    }
}
