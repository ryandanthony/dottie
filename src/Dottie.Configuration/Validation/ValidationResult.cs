// -----------------------------------------------------------------------
// <copyright file="ValidationResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Validation;

/// <summary>
/// Result of configuration validation.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation passed with no errors.
    /// </summary>
    /// <value>
    /// A value indicating whether the validation passed with no errors.
    /// </value>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    /// <value>
    /// The list of validation errors.
    /// </value>
    public IList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <returns>A successful <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Failure(params ValidationError[] errors) =>
        new() { Errors = [.. errors] };

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) =>
        new() { Errors = [.. errors] };
}
