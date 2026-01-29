// -----------------------------------------------------------------------
// <copyright file="LoadResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;
using Dottie.Configuration.Validation;

namespace Dottie.Configuration.Parsing;

/// <summary>
/// Result of loading and parsing a configuration file.
/// </summary>
public sealed class LoadResult
{
    /// <summary>
    /// Gets the loaded configuration if successful.
    /// </summary>
    /// <value>
    /// The loaded configuration if successful.
    /// </value>
    public DottieConfiguration? Configuration { get; init; }

    /// <summary>
    /// Gets the validation errors if unsuccessful.
    /// </summary>
    /// <value>
    /// The validation errors if unsuccessful.
    /// </value>
    public IList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the load was successful.
    /// </summary>
    /// <value>
    /// A value indicating whether the load was successful.
    /// </value>
    public bool IsSuccess => Errors.Count == 0 && Configuration is not null;
}
