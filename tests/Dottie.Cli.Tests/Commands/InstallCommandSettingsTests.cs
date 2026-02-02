// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Cli.Commands;
using Dottie.Configuration.Installing;
using FluentAssertions;
using Spectre.Console.Cli;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="InstallCommandSettings"/>.
/// </summary>
public class InstallCommandSettingsTests
{
    [Fact]
    public void InstallCommandSettings_HasDryRunFlag()
    {
        // Arrange
        var settings = new InstallCommandSettings { DryRun = true };

        // Act & Assert
        settings.DryRun.Should().BeTrue();
    }

    [Fact]
    public void InstallCommandSettings_DryRunDefaultIsFalse()
    {
        // Arrange
        var settings = new InstallCommandSettings();

        // Act & Assert
        settings.DryRun.Should().BeFalse();
    }

    [Fact]
    public void InstallCommandSettings_InheritsFromProfileAwareSettings()
    {
        // Arrange
        var settings = new InstallCommandSettings
        {
            ProfileName = "custom-profile",
            ConfigPath = "/path/to/config.yml",
        };

        // Act & Assert
        settings.ProfileName.Should().Be("custom-profile");
        settings.ConfigPath.Should().Be("/path/to/config.yml");
    }

    [Fact]
    public void InstallCommandSettings_SupportsCommandAttribute()
    {
        // Act
        var properties = typeof(InstallCommandSettings)
            .GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(CommandOptionAttribute), false).Any());

        // Assert
        properties.Should().Contain(p => p.Name == "DryRun");
    }
}
