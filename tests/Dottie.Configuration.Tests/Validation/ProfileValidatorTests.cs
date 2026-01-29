// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models;
using Dottie.Configuration.Validation;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Validation;

/// <summary>
/// Tests for <see cref="ProfileValidator"/>.
/// </summary>
public class ProfileValidatorTests
{
    [Fact]
    public void Validate_ValidProfile_ReturnsSuccess()
    {
        // Arrange
        var profile = new ConfigProfile
        {
            Dotfiles =
            [
                new DotfileEntry { Source = "dotfiles/bashrc", Target = "~/.bashrc" },
            ],
        };
        var validator = new ProfileValidator();

        // Act
        var result = validator.Validate(profile, "default");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ProfileWithInvalidDotfile_ReturnsError()
    {
        // Arrange
        var profile = new ConfigProfile
        {
            Dotfiles =
            [
                new DotfileEntry { Source = string.Empty, Target = "~/.bashrc" },
            ],
        };
        var validator = new ProfileValidator();

        // Act
        var result = validator.Validate(profile, "default");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Path.Should().Contain("profiles.default.dotfiles[0].source");
    }

    [Fact]
    public void Validate_ProfileWithMultipleDotfiles_ValidatesAll()
    {
        // Arrange
        var profile = new ConfigProfile
        {
            Dotfiles =
            [
                new DotfileEntry { Source = "dotfiles/bashrc", Target = "~/.bashrc" },
                new DotfileEntry { Source = string.Empty, Target = string.Empty },
            ],
        };
        var validator = new ProfileValidator();

        // Act
        var result = validator.Validate(profile, "work");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Path.Contains("profiles.work.dotfiles[1].source"));
        result.Errors.Should().Contain(e => e.Path.Contains("profiles.work.dotfiles[1].target"));
    }

    [Fact]
    public void Validate_EmptyProfile_ReturnsSuccess()
    {
        // Arrange
        var profile = new ConfigProfile();
        var validator = new ProfileValidator();

        // Act
        var result = validator.Validate(profile, "empty");

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
