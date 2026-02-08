// -----------------------------------------------------------------------
// <copyright file="VariableResolutionResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Utilities;

/// <summary>
/// The outcome of resolving variables in a single string value.
/// </summary>
/// <param name="ResolvedValue">The string with all resolvable variables substituted.</param>
/// <param name="UnresolvedVariables">Variable names that could not be resolved.</param>
public sealed record VariableResolutionResult(
    string ResolvedValue,
    IReadOnlyList<string> UnresolvedVariables)
{
    /// <summary>
    /// Gets a value indicating whether any non-deferred variables were unresolvable.
    /// </summary>
    public bool HasErrors => UnresolvedVariables.Count > 0;
}
