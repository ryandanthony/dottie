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

            config.AddCommand<ValidateCommand>("validate")
                .WithDescription("Validate the dottie.yaml configuration file")
                .WithExample("validate", "default")
                .WithExample("validate", "--config", "./my-config.yaml", "work");
        });

        return app.Run(args);
    }
}
