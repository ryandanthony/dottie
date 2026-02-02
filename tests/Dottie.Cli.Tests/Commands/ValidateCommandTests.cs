// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="ValidateCommand"/>.
/// </summary>
public sealed class ValidateCommandTests
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

    [Fact]
    public void Execute_ValidConfig_ReturnsZero()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act - use --profile flag
        var result = app.Run(["validate", "-c", configPath, "--profile", "default"]);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_ValidConfigNoProfile_ReturnsZero()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act
        var result = app.Run(["validate", "-c", configPath]);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_InvalidYamlSyntax_ReturnsNonZero()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "invalid-yaml-syntax.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act
        var result = app.Run(["validate", "-c", configPath]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_MissingProfile_ReturnsNonZero()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act - use --profile flag
        var result = app.Run(["validate", "-c", configPath, "--profile", "nonexistent"]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_CircularInheritance_ReturnsNonZero()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "invalid-circular-inheritance.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act - use --profile flag
        var result = app.Run(["validate", "-c", configPath, "--profile", "alpha"]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_ProfileWithInheritance_ReturnsZero()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act - use --profile flag
        var result = app.Run(["validate", "-c", configPath, "--profile", "work"]);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_ConfigFileNotFound_ReturnsNonZero()
    {
        // Arrange
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act
        var result = app.Run(["validate", "-c", "nonexistent-config.yaml"]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_ValidationErrors_ReturnsNonZero()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "invalid-missing-source.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act
        var result = app.Run(["validate", "-c", configPath]);

        // Assert
        result.Should().Be(1);
    }

#pragma warning disable SA1124 // Do not use regions

    #region Profile Flag Tests (US1)

    [Fact]
    public void Execute_WithProfileFlag_UsesSpecifiedProfile()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act - use --profile flag instead of positional argument
        var result = app.Run(["validate", "-c", configPath, "--profile", "work"]);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_WithProfileFlagShortForm_UsesSpecifiedProfile()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act - use -p short form
        var result = app.Run(["validate", "-c", configPath, "-p", "work"]);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_WithoutProfileFlag_UsesDefaultProfile()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act - no profile flag should use "default"
        var result = app.Run(["validate", "-c", configPath]);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_WithInvalidProfile_ShowsErrorWithAvailableProfiles()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ValidateCommand>("validate");
        });

        // Act
        var result = app.Run(["validate", "-c", configPath, "--profile", "nonexistent"]);

        // Assert
        result.Should().Be(1);
    }

    #endregion

#pragma warning restore SA1124
}
