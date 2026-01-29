// -----------------------------------------------------------------------
// <copyright file="ConfigurationValidator.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration.Validation;

/// <summary>
/// Orchestrates validation of the entire configuration.
/// </summary>
public class ConfigurationValidator
{
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
}
