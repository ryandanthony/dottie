// -----------------------------------------------------------------------
// <copyright file="DotfileStatusEntry.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;

namespace Dottie.Configuration.Status;

/// <summary>
/// Result of checking a single dotfile entry's status.
/// </summary>
/// <param name="Entry">The dotfile configuration being checked.</param>
/// <param name="State">Current state of the link.</param>
/// <param name="Message">Additional details (conflict type, error reason, existing target).</param>
/// <param name="ExpandedTarget">Target path with ~ expanded.</param>
public sealed record DotfileStatusEntry(
    DotfileEntry Entry,
    DotfileLinkState State,
    string? Message,
    string ExpandedTarget);
