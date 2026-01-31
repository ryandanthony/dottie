// -----------------------------------------------------------------------
// <copyright file="ProfileAwareSettings.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Base settings class for commands that accept a profile option.
/// </summary>
public abstract class ProfileAwareSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the profile name to use.
    /// </summary>
    /// <value>
    /// The profile name to use. Defaults to "default" if not specified.
    /// </value>
    [Description("Profile to use (default: 'default')")]
    [CommandOption("-p|--profile")]
    public string? ProfileName { get; set; }

    /// <summary>
    /// Gets or sets the path to the configuration file.
    /// </summary>
    /// <value>
    /// The path to the configuration file. Defaults to dottie.yaml in repo root.
    /// </value>
    [Description("Path to the configuration file (default: dottie.yaml in repo root)")]
    [CommandOption("-c|--config")]
    public string? ConfigPath { get; set; }
}
