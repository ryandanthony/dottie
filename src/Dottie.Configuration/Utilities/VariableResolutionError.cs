// -----------------------------------------------------------------------
// <copyright file="VariableResolutionError.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Utilities;

/// <summary>
/// A single error produced when a non-deferred variable cannot be resolved, with full context for actionable error reporting.
/// </summary>
/// <param name="ProfileName">Which profile contains the error.</param>
/// <param name="EntryIdentifier">Which entry (e.g., aptrepo name, github repo, dotfile source).</param>
/// <param name="FieldName">Which field contains the unresolvable variable.</param>
/// <param name="VariableName">The unresolvable variable name.</param>
/// <param name="Message">Human-readable error message.</param>
public sealed record VariableResolutionError(
    string ProfileName,
    string EntryIdentifier,
    string FieldName,
    string VariableName,
    string Message);
