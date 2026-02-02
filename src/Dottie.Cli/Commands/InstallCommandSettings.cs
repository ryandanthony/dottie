// -----------------------------------------------------------------------
// <copyright file="InstallCommandSettings.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Settings for the install command.
/// </summary>
public sealed class InstallCommandSettings : ProfileAwareSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to preview without executing.
    /// </summary>
    /// <value>
    /// True to preview the installation without making changes; otherwise, false.
    /// </value>
    [Description("Preview the installation without executing changes")]
    [CommandOption("--dry-run")]
    public bool DryRun { get; set; }
}
