// -----------------------------------------------------------------------
// <copyright file="DotfileEntryValidator.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration.Validation;

/// <summary>
/// Validates <see cref="DotfileEntry"/> instances.
/// </summary>
public class DotfileEntryValidator
{
    /// <summary>
    /// Validates a dotfile entry.
    /// </summary>
    /// <param name="entry">The entry to validate.</param>
    /// <param name="path">The path in the config for error reporting.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult Validate(DotfileEntry entry, string path)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(entry.Source))
        {
            errors.Add(new ValidationError($"{path}.source", "Dotfile entry must have a 'source' field"));
        }

        if (string.IsNullOrWhiteSpace(entry.Target))
        {
            errors.Add(new ValidationError($"{path}.target", "Dotfile entry must have a 'target' field"));
        }

        return new ValidationResult { Errors = errors };
    }
}
