// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models;
using Dottie.Configuration.Validation;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Validation;

/// <summary>
/// Tests for <see cref="ConfigurationValidator"/>.
/// </summary>
public class ConfigurationValidatorTests
{
    [Fact]
    public void Validate_ValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["default"] = new ConfigProfile
                {
                    Dotfiles = [new DotfileEntry { Source = "dotfiles/bashrc", Target = "~/.bashrc" }],
                },
            },
        };
        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyProfiles_ReturnsError()
    {
        // Arrange
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>(),
        };
        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("at least one profile");
    }

    [Fact]
    public void Validate_ExtendsNonExistentProfile_ReturnsError()
    {
        // Arrange
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["work"] = new ConfigProfile { Extends = "nonexistent" },
            },
        };
        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("non-existent profile 'nonexistent'");
    }

    [Fact]
    public void Validate_ExtendsExistingProfile_ReturnsSuccess()
    {
        // Arrange
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["default"] = new ConfigProfile
                {
                    Dotfiles = [new DotfileEntry { Source = "dotfiles/bashrc", Target = "~/.bashrc" }],
                },
                ["work"] = new ConfigProfile { Extends = "default" },
            },
        };
        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MultipleProfiles_ValidatesAll()
    {
        // Arrange
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["valid"] = new ConfigProfile
                {
                    Dotfiles = [new DotfileEntry { Source = "dotfiles/bashrc", Target = "~/.bashrc" }],
                },
                ["invalid"] = new ConfigProfile
                {
                    Dotfiles = [new DotfileEntry { Source = string.Empty, Target = "~/.bashrc" }],
                },
            },
        };
        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Path.Should().Contain("profiles.invalid.dotfiles[0].source");
    }
}
