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
    private const string ProfileOption = "--profile";
    private const string ConfigOption = "-c";
    private const string ProfileShortOption = "-p";
    private const string SampleConfigPath = "./my-config.yaml";

    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    public static int Main(string[] args)
    {
        var app = new CommandApp();

        app.Configure(config =>
        {
            config.SetApplicationName("dottie");
            config.SetApplicationVersion("0.1.0");

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
                .WithExample(InstallCommandName, "--dry-run")
                .WithExample(InstallCommandName, ConfigOption, SampleConfigPath, ProfileShortOption, "work", "--dry-run");

            config.AddCommand<StatusCommand>(StatusCommandName)
                .WithDescription("Display status of dotfiles and software installations")
                .WithExample(StatusCommandName)
                .WithExample(StatusCommandName, ProfileOption, "work")
                .WithExample(StatusCommandName, ConfigOption, SampleConfigPath, ProfileShortOption, "work");
        });

        return app.Run(args);
    }
}
