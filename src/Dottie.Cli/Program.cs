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
                .WithExample(ValidateCommandName, "--profile", "work")
                .WithExample(ValidateCommandName, "-c", "./my-config.yaml", "-p", "work");

            config.AddCommand<LinkCommand>(LinkCommandName)
                .WithDescription("Create symbolic links for dotfiles")
                .WithExample(LinkCommandName)
                .WithExample(LinkCommandName, "--profile", "work")
                .WithExample(LinkCommandName, "--force")
                .WithExample(LinkCommandName, "-c", "./my-config.yaml", "-p", "work", "-f");
        });

        return app.Run(args);
    }
}
