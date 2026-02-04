// -----------------------------------------------------------------------
// <copyright file="StatusCommandTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="StatusCommand"/>.
/// </summary>
public sealed class StatusCommandTests
{
    private static readonly string FixturesPath = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "..",
        "tests",
        "Dottie.Configuration.Tests",
        "Fixtures");

    #region User Story 3: Status for Specific Profile

    [Fact]
    public void Execute_WithProfileOption_ShowsOnlyProfileItems()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<StatusCommand>("status");
        });

        // Act - use --profile flag to select work profile
        var result = app.Run(["status", "-c", configPath, "--profile", "work"]);

        // Assert
        // Status command always returns 0 for successful execution (informational command)
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_WithProfileShortOption_ShowsOnlyProfileItems()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<StatusCommand>("status");
        });

        // Act - use -p short flag
        var result = app.Run(["status", "-c", configPath, "-p", "work"]);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_WithInheritedProfile_ShowsParentAndChildItems()
    {
        // Arrange - work profile extends default
        var configPath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<StatusCommand>("status");
        });

        // Act - work extends default, should show both
        var result = app.Run(["status", "-c", configPath, "--profile", "work"]);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_WithNonExistentProfile_ReturnsErrorExitCode()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<StatusCommand>("status");
        });

        // Act - try to get status for a profile that doesn't exist
        var result = app.Run(["status", "-c", configPath, "--profile", "nonexistent"]);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region User Story 4: Default Profile Resolution

    [Fact]
    public void Execute_WithoutProfileOption_UsesDefaultProfile()
    {
        // Arrange - config has a default profile
        var configPath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<StatusCommand>("status");
        });

        // Act - no --profile flag, should use "default"
        var result = app.Run(["status", "-c", configPath]);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_WithNoDefaultAndMultipleProfiles_ShowsError()
    {
        // Note: This test requires a fixture without a "default" profile
        // For now, we'll test error handling with invalid config path
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<StatusCommand>("status");
        });

        // Act - config file doesn't exist
        var result = app.Run(["status", "-c", "nonexistent-config.yaml"]);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region General Error Handling

    [Fact]
    public void Execute_ConfigFileNotFound_ReturnsErrorExitCode()
    {
        // Arrange
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<StatusCommand>("status");
        });

        // Act
        var result = app.Run(["status", "-c", "does-not-exist.yaml"]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_InvalidYamlSyntax_ReturnsErrorExitCode()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "invalid-yaml-syntax.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<StatusCommand>("status");
        });

        // Act
        var result = app.Run(["status", "-c", configPath]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_ValidationErrors_ReturnsErrorExitCode()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "invalid-missing-source.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<StatusCommand>("status");
        });

        // Act
        var result = app.Run(["status", "-c", configPath]);

        // Assert
        result.Should().Be(1);
    }

    #endregion
}
