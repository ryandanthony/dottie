// -----------------------------------------------------------------------
// <copyright file="ValidationError.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Validation;

/// <summary>
/// Represents a validation error with location information.
/// </summary>
/// <param name="Path">JSON-path style location (e.g., "profiles.work.dotfiles[0].source").</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Line">Line number in the YAML file, if available.</param>
/// <param name="Column">Column number in the YAML file, if available.</param>
public sealed record ValidationError(
    string Path,
    string Message,
    int? Line = null,
    int? Column = null);
