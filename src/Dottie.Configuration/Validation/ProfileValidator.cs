// -----------------------------------------------------------------------
// <copyright file="ProfileValidator.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration.Validation;

/// <summary>
/// Validates <see cref="ConfigProfile"/> instances.
/// </summary>
public class ProfileValidator
{
    private readonly DotfileEntryValidator _dotfileValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileValidator"/> class.
    /// </summary>
    public ProfileValidator()
        : this(new DotfileEntryValidator())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileValidator"/> class.
    /// </summary>
    /// <param name="dotfileValidator">The dotfile entry validator.</param>
    public ProfileValidator(DotfileEntryValidator dotfileValidator)
    {
        _dotfileValidator = dotfileValidator ?? throw new ArgumentNullException(nameof(dotfileValidator));
    }

    /// <summary>
    /// Validates a profile.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="profileName">The name of the profile for error reporting.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult Validate(ConfigProfile profile, string profileName)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileName);

        var errors = new List<ValidationError>();
        var basePath = $"profiles.{profileName}";

        // Validate dotfile entries
        for (var i = 0; i < profile.Dotfiles.Count; i++)
        {
            var dotfileResult = _dotfileValidator.Validate(
                profile.Dotfiles[i],
                $"{basePath}.dotfiles[{i}]");

            if (!dotfileResult.IsValid)
            {
                errors.AddRange(dotfileResult.Errors);
            }
        }

        return new ValidationResult { Errors = errors };
    }
}
