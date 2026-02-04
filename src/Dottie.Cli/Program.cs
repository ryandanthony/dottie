// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Commands;
using Spectre.Console.Cli;

namespace Dottie.Cli;

/// <summary>
/// Entry point for the Dottie CLI application.
/// </summary>
public static class Program
{
    private const string ValidateCommandName = "validate";
    private const string LinkCommandName = "link";
    private const string InstallCommandName = "install";
    private const string StatusCommandName = "status";
    private const string ApplyCommandName = "apply";
    private const string ProfileOption = "--profile";
    private const string ConfigOption = "-c";
    private const string ProfileShortOption = "-p";
    private const string SampleConfigPath = "./my-config.yaml";
    private const string DryRunOption = "--dry-run";

    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    public static int Main(string[] args)
    {
        var app = new CommandApp();

        // Get version from assembly metadata (set by CI via /p:Version)
        var version = typeof(Program).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion ?? "0.0.0-local";

        app.Configure(config =>
        {
            config.SetApplicationName("dottie");
            config.SetApplicationVersion(version);

            config.AddCommand<ValidateCommand>(ValidateCommandName)
                .WithDescription("Validate the dottie.yaml configuration file")
                .WithExample(ValidateCommandName)
                .WithExample(ValidateCommandName, ProfileOption, "work")
                .WithExample(ValidateCommandName, ConfigOption, SampleConfigPath, ProfileShortOption, "work");

            config.AddCommand<LinkCommand>(LinkCommandName)
                .WithDescription("Create symbolic links for dotfiles")
                .WithExample(LinkCommandName)
                .WithExample(LinkCommandName, ProfileOption, "work")
                .WithExample(LinkCommandName, "--force")
                .WithExample(LinkCommandName, ConfigOption, SampleConfigPath, ProfileShortOption, "work", "-f");

            config.AddCommand<InstallCommand>(InstallCommandName)
                .WithDescription("Install software from configured sources")
                .WithExample(InstallCommandName)
                .WithExample(InstallCommandName, ProfileOption, "work")
                .WithExample(InstallCommandName, DryRunOption)
                .WithExample(InstallCommandName, ConfigOption, SampleConfigPath, ProfileShortOption, "work", DryRunOption);

            config.AddCommand<StatusCommand>(StatusCommandName)
                .WithDescription("Display status of dotfiles and software installations")
                .WithExample(StatusCommandName)
                .WithExample(StatusCommandName, ProfileOption, "work")
                .WithExample(StatusCommandName, ConfigOption, SampleConfigPath, ProfileShortOption, "work");

            config.AddCommand<ApplyCommand>(ApplyCommandName)
                .WithDescription("Apply profile: create symlinks and install software")
                .WithExample(ApplyCommandName)
                .WithExample(ApplyCommandName, ProfileOption, "work")
                .WithExample(ApplyCommandName, DryRunOption)
                .WithExample(ApplyCommandName, "--force")
                .WithExample(ApplyCommandName, "-p", "work", "-f", DryRunOption);
        });

        return app.Run(args);
    }
}
