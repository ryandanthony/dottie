// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Validation;
using FluentAssertions;
using Xunit;

namespace Dottie.Configuration.Tests.Validation;

/// <summary>
/// Tests for profile name validation within <see cref="ConfigurationValidator"/>.
/// </summary>
public sealed class ProfileNameValidatorTests
{
    [Theory]
    [InlineData("default")]
    [InlineData("work")]
    [InlineData("my-profile")]
    [InlineData("my_profile")]
    [InlineData("Profile123")]
    [InlineData("a")]
    [InlineData("A1_b2-c3")]
    public void ValidateProfileName_ValidNames_NoErrors(string profileName)
    {
        // Arrange & Act
        var isValid = ConfigurationValidator.IsValidProfileName(profileName);

        // Assert
        isValid.Should().BeTrue($"'{profileName}' should be a valid profile name");
    }

    [Theory]
    [InlineData("my profile", "contains space")]
    [InlineData("work@home", "contains @")]
    [InlineData("profile.name", "contains period")]
    [InlineData("profile/path", "contains slash")]
    [InlineData("profile\\path", "contains backslash")]
    [InlineData("name:value", "contains colon")]
    [InlineData("profile!", "contains exclamation")]
    [InlineData("profile#tag", "contains hash")]
    public void ValidateProfileName_InvalidNames_ReturnsFalse(string profileName, string reason)
    {
        // Arrange & Act
        var isValid = ConfigurationValidator.IsValidProfileName(profileName);

        // Assert
        isValid.Should().BeFalse($"'{profileName}' should be invalid because it {reason}");
    }

    [Fact]
    public void Validate_ConfigWithInvalidProfileName_ReturnsErrors()
    {
        // Arrange
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "Fixtures",
            "profile-invalid-name.yaml");

        var loader = new Dottie.Configuration.Parsing.ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(loadResult.Configuration!);

        // Assert
        result.IsValid.Should().BeFalse("configuration has profiles with invalid names");
        result.Errors.Should().Contain(e => e.Message.Contains("my profile"));
        result.Errors.Should().Contain(e => e.Message.Contains("work@home"));
    }
}
