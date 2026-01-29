// -----------------------------------------------------------------------
// <copyright file="ValidateCommandSettings.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Settings for the validate command.
/// </summary>
public sealed class ValidateCommandSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the profile name to validate.
    /// </summary>
    /// <value>
    /// The profile name to validate.
    /// </value>
    [Description("The profile name to validate")]
    [CommandArgument(0, "[profile]")]
    public string? ProfileName { get; set; }

    /// <summary>
    /// Gets or sets the path to the configuration file.
    /// </summary>
    /// <value>
    /// The path to the configuration file.
    /// </value>
    [Description("Path to the configuration file (default: dottie.yaml in repo root)")]
    [CommandOption("-c|--config")]
    public string? ConfigPath { get; set; }
}
