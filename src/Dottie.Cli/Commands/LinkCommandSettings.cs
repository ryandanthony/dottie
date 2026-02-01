// -----------------------------------------------------------------------
// <copyright file="LinkCommandSettings.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Settings for the link command.
/// </summary>
public sealed class LinkCommandSettings : ProfileAwareSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to force linking by backing up conflicting files.
    /// </summary>
    [Description("Force linking by backing up conflicting files")]
    [CommandOption("-f|--force")]
    public bool Force { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to preview changes without making any modifications.
    /// </summary>
    [Description("Preview changes without making any modifications")]
    [CommandOption("-d|--dry-run")]
    public bool DryRun { get; set; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (DryRun && Force)
        {
            return ValidationResult.Error("--dry-run and --force cannot be used together.");
        }

        return ValidationResult.Success();
    }
}
