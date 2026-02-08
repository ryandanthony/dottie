// -----------------------------------------------------------------------
// <copyright file="ConfigurationResolutionResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration.Utilities;

/// <summary>
/// The outcome of resolving all variables across an entire <see cref="DottieConfiguration"/>.
/// Wraps the resolved configuration and any accumulated errors.
/// </summary>
/// <param name="Configuration">The configuration with all resolvable variables substituted.</param>
/// <param name="Errors">All errors from unresolvable non-deferred variables.</param>
public sealed record ConfigurationResolutionResult(
    DottieConfiguration Configuration,
    IReadOnlyList<VariableResolutionError> Errors)
{
    /// <summary>
    /// Gets a value indicating whether any resolution errors occurred.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}
