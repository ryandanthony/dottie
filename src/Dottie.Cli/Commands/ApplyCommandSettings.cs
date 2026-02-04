// -----------------------------------------------------------------------
// <copyright file="ApplyCommandSettings.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Settings for the apply command.
/// </summary>
public sealed class ApplyCommandSettings : ProfileAwareSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to force apply by backing up conflicting files.
    /// </summary>
    /// <value>
    /// <c>true</c> to force apply by backing up conflicts; otherwise, <c>false</c>.
    /// </value>
    [Description("Force apply by backing up conflicting files")]
    [CommandOption("-f|--force")]
    public bool Force { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to preview changes without making any modifications.
    /// </summary>
    /// <value>
    /// <c>true</c> to preview changes only; otherwise, <c>false</c>.
    /// </value>
    [Description("Preview changes without making any modifications")]
    [CommandOption("-d|--dry-run")]
    public bool DryRun { get; set; }
}
