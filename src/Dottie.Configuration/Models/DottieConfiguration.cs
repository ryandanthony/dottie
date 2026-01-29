// -----------------------------------------------------------------------
// <copyright file="DottieConfiguration.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Models;

/// <summary>
/// Root configuration object representing a dottie.yaml file.
/// </summary>
public sealed record DottieConfiguration
{
    /// <summary>
    /// Gets the named profiles defined in this configuration.
    /// </summary>
    /// <value>
    /// Dictionary where key is the profile name (e.g., "default", "work").
    /// </value>
    public required IDictionary<string, ConfigProfile> Profiles { get; init; }
}
