// -----------------------------------------------------------------------
// <copyright file="ProfileResolver.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration;

/// <summary>
/// Resolves profile names to their configuration and provides profile listing.
/// </summary>
public sealed class ProfileResolver
{
    private readonly DottieConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileResolver"/> class.
    /// </summary>
    /// <param name="configuration">The loaded configuration containing profiles.</param>
    public ProfileResolver(DottieConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Gets a profile by name.
    /// </summary>
    /// <param name="profileName">The name of the profile to retrieve, or null if not specified.</param>
    /// <returns>A result containing the profile or an error with available profile names.</returns>
    public ProfileResolveResult GetProfile(string? profileName)
    {
        var availableProfiles = ListProfiles();

        if (string.IsNullOrEmpty(profileName))
        {
            return ProfileResolveResult.Failure(
                "No profile specified. Please specify a profile name.",
                availableProfiles);
        }

        if (_configuration.Profiles.TryGetValue(profileName, out var profile))
        {
            return ProfileResolveResult.Success(profile);
        }

        return ProfileResolveResult.Failure(
            $"Profile '{profileName}' not found.",
            availableProfiles);
    }

    /// <summary>
    /// Gets a list of all available profile names.
    /// </summary>
    /// <returns>A collection of profile names sorted alphabetically.</returns>
    public IReadOnlyList<string> ListProfiles()
    {
        return _configuration.Profiles.Keys
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();
    }
}
