// -----------------------------------------------------------------------
// <copyright file="InheritanceResolveResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Inheritance;

/// <summary>
/// Result of a profile inheritance resolution operation.
/// </summary>
public sealed record InheritanceResolveResult
{
    /// <summary>
    /// Gets the resolved profile if successful.
    /// </summary>
    /// <value>
    /// The resolved profile if successful.
    /// </value>
    public ResolvedProfile? Profile { get; init; }

    /// <summary>
    /// Gets the error message if the resolution failed.
    /// </summary>
    /// <value>
    /// The error message if the resolution failed.
    /// </value>
    public string? Error { get; init; }

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
    public static InheritanceResolveResult Success(ResolvedProfile profile)
    {
        return new InheritanceResolveResult { Profile = profile };
    }

    /// <summary>
    /// Creates a failure result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure result.</returns>
    public static InheritanceResolveResult Failure(string error)
    {
        return new InheritanceResolveResult { Error = error };
    }
}
