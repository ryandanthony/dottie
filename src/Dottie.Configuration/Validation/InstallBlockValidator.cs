// -----------------------------------------------------------------------
// <copyright file="InstallBlockValidator.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Validation;

/// <summary>
/// Validates install block items.
/// </summary>
public class InstallBlockValidator
{
    /// <summary>
    /// Validates a GitHub release item.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <param name="path">The path in the config for error reporting.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult ValidateGithubRelease(GithubReleaseItem item, string path)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(item.Repo))
        {
            errors.Add(new ValidationError($"{path}.repo", "GitHub release must have a 'repo' field (format: owner/repo)"));
        }

        if (string.IsNullOrWhiteSpace(item.Asset))
        {
            errors.Add(new ValidationError($"{path}.asset", "GitHub release must have an 'asset' pattern field"));
        }

        if (string.IsNullOrWhiteSpace(item.Binary))
        {
            errors.Add(new ValidationError($"{path}.binary", "GitHub release must have a 'binary' name field"));
        }

        return new ValidationResult { Errors = errors };
    }

    /// <summary>
    /// Validates an APT repository item.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <param name="path">The path in the config for error reporting.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult ValidateAptRepo(AptRepoItem item, string path)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(item.KeyUrl))
        {
            errors.Add(new ValidationError($"{path}.keyUrl", "APT repository must have a 'keyUrl' field"));
        }

        if (string.IsNullOrWhiteSpace(item.Repo))
        {
            errors.Add(new ValidationError($"{path}.repo", "APT repository must have a 'repo' field"));
        }

        return new ValidationResult { Errors = errors };
    }

    /// <summary>
    /// Validates a Snap item.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <param name="path">The path in the config for error reporting.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult ValidateSnap(SnapItem item, string path)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(item.Name))
        {
            errors.Add(new ValidationError($"{path}.name", "Snap package must have a 'name' field"));
        }

        return new ValidationResult { Errors = errors };
    }

    /// <summary>
    /// Validates a Font item.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <param name="path">The path in the config for error reporting.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult ValidateFont(FontItem item, string path)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(item.Url))
        {
            errors.Add(new ValidationError($"{path}.url", "Font must have a 'url' field"));
        }

        return new ValidationResult { Errors = errors };
    }
}
