// -----------------------------------------------------------------------
// <copyright file="ConfigurationValidator.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Dottie.Configuration.Models;

namespace Dottie.Configuration.Validation;

/// <summary>
/// Orchestrates validation of the entire configuration.
/// </summary>
public partial class ConfigurationValidator
{
    /// <summary>
    /// Pattern for valid profile names: alphanumeric, hyphens, and underscores only.
    /// </summary>
    private const string ProfileNamePattern = @"^[a-zA-Z0-9_-]+$";

    private readonly ProfileValidator _profileValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidator"/> class.
    /// </summary>
    public ConfigurationValidator()
        : this(new ProfileValidator())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidator"/> class.
    /// </summary>
    /// <param name="profileValidator">The profile validator.</param>
    public ConfigurationValidator(ProfileValidator profileValidator)
    {
        _profileValidator = profileValidator ?? throw new ArgumentNullException(nameof(profileValidator));
    }

    /// <summary>
    /// Validates whether a profile name contains only valid characters.
    /// </summary>
    /// <param name="profileName">The profile name to validate.</param>
    /// <returns>True if the name is valid, false otherwise.</returns>
    public static bool IsValidProfileName(string profileName)
    {
        if (string.IsNullOrEmpty(profileName))
        {
            return false;
        }

        return ProfileNameRegex().IsMatch(profileName);
    }

    /// <summary>
    /// Validates the entire configuration.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult Validate(DottieConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var errors = new List<ValidationError>();

        // Validate that at least one profile exists
        if (configuration.Profiles.Count == 0)
        {
            errors.Add(new ValidationError("profiles", "Configuration must contain at least one profile"));
            return new ValidationResult { Errors = errors };
        }

        // Validate profile names
        ValidateProfileNames(configuration, errors);

        // Validate each profile
        foreach (var (profileName, profile) in configuration.Profiles)
        {
            var profileResult = _profileValidator.Validate(profile, profileName);
            if (!profileResult.IsValid)
            {
                errors.AddRange(profileResult.Errors);
            }
        }

        // Validate extends references point to existing profiles
        foreach (var (profileName, profile) in configuration.Profiles)
        {
            if (!string.IsNullOrWhiteSpace(profile.Extends) &&
                !configuration.Profiles.ContainsKey(profile.Extends))
            {
                errors.Add(new ValidationError(
                    $"profiles.{profileName}.extends",
                    $"Profile '{profileName}' extends non-existent profile '{profile.Extends}'"));
            }
        }

        return new ValidationResult { Errors = errors };
    }

    /// <summary>
    /// Validates that all profile names contain only valid characters.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <param name="errors">The list to add validation errors to.</param>
    private static void ValidateProfileNames(DottieConfiguration configuration, List<ValidationError> errors)
    {
        var invalidNames = configuration.Profiles.Keys
            .Where(name => !IsValidProfileName(name))
            .Select(name => new ValidationError(
                $"profiles.{name}",
                $"Profile name '{name}' contains invalid characters. Profile names must contain only letters, numbers, hyphens, and underscores."));

        errors.AddRange(invalidNames);
    }

    [GeneratedRegex(ProfileNamePattern, RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ProfileNameRegex();
}
