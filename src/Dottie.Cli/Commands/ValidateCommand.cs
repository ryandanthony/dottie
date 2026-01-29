// -----------------------------------------------------------------------
// <copyright file="ValidateCommand.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Output;
using Dottie.Cli.Utilities;
using Dottie.Configuration;
using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Models;
using Dottie.Configuration.Parsing;
using Dottie.Configuration.Validation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Dottie.Cli.Commands;

/// <summary>
/// Command to validate a dottie configuration file.
/// </summary>
public sealed class ValidateCommand : Command<ValidateCommandSettings>
{
    /// <inheritdoc/>
    public override int Execute(CommandContext context, ValidateCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        // Find config file
        var configPath = settings.ConfigPath ?? FindConfigFile();
        if (configPath is null)
        {
            return WriteConfigNotFoundError();
        }

        // Load configuration
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(configPath);

        if (!loadResult.IsSuccess)
        {
            ErrorFormatter.WriteErrors(loadResult.Errors.ToList());
            return 1;
        }

        // Validate configuration structure
        var validator = new ConfigurationValidator();
        var validationResult = validator.Validate(loadResult.Configuration!);

        if (!validationResult.IsValid)
        {
            ErrorFormatter.WriteErrors(validationResult.Errors.ToList());
            return 1;
        }

        // Resolve profile if specified
        return settings.ProfileName is not null
            ? ValidateProfile(loadResult.Configuration!, settings.ProfileName)
            : ListAvailableProfiles(loadResult.Configuration!);
    }

    private static int ValidateProfile(DottieConfiguration configuration, string profileName)
    {
        var resolver = new ProfileResolver(configuration);
        var resolveResult = resolver.GetProfile(profileName);

        if (!resolveResult.IsSuccess)
        {
            WriteProfileNotFoundError(resolveResult.Error!, resolveResult.AvailableProfiles);
            return 1;
        }

        // Check for inheritance issues
        var merger = new ProfileMerger(configuration);
        var mergeResult = merger.Resolve(profileName);

        if (!mergeResult.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {mergeResult.Error}");
            return 1;
        }

        WriteProfileValidSuccess(profileName, mergeResult.Profile!);
        return 0;
    }

    private static int ListAvailableProfiles(DottieConfiguration configuration)
    {
        var resolver = new ProfileResolver(configuration);
        var profiles = resolver.ListProfiles();

        AnsiConsole.MarkupLine("[green]✓[/] Configuration file is valid.");
        WriteAvailableProfiles(profiles);
        AnsiConsole.MarkupLine(string.Empty);
        AnsiConsole.MarkupLine("[dim]Run 'dottie validate <profile>' to validate a specific profile.[/]");

        return 0;
    }

    private static int WriteConfigNotFoundError()
    {
        AnsiConsole.MarkupLine("[red]Error:[/] Could not find dottie.yaml in the repository root.");
        AnsiConsole.MarkupLine("[yellow]Hint:[/] Make sure you're running from within a git repository.");
        return 1;
    }

    private static void WriteProfileNotFoundError(string error, IReadOnlyList<string> availableProfiles)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {error}");
        WriteAvailableProfiles(availableProfiles);
    }

    private static void WriteAvailableProfiles(IReadOnlyList<string> profiles)
    {
        AnsiConsole.MarkupLine("[yellow]Available profiles:[/]");
        foreach (var profile in profiles)
        {
            AnsiConsole.MarkupLine($"  • {profile}");
        }
    }

    private static void WriteProfileValidSuccess(string profileName, ResolvedProfile resolvedProfile)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] Profile '[bold]{profileName}[/]' is valid.");
        if (resolvedProfile.InheritanceChain.Count > 1)
        {
            AnsiConsole.MarkupLine($"  Inheritance chain: {string.Join(" → ", resolvedProfile.InheritanceChain)}");
        }
    }

    private static string? FindConfigFile()
    {
        var repoRoot = RepoRootFinder.Find();
        if (repoRoot is null)
        {
            return null;
        }

        var configPath = Path.Combine(repoRoot, "dottie.yaml");
        return File.Exists(configPath) ? configPath : null;
    }
}
