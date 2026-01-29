// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models;
using Dottie.Configuration.Validation;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Validation;

/// <summary>
/// Tests for <see cref="DotfileEntryValidator"/>.
/// </summary>
public class DotfileEntryValidatorTests
{
    [Fact]
    public void Validate_MissingSource_ReturnsError()
    {
        // Arrange
        var entry = new DotfileEntry { Source = string.Empty, Target = "~/.bashrc" };
        var validator = new DotfileEntryValidator();

        // Act
        var result = validator.Validate(entry, "profiles.default.dotfiles[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("source", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingTarget_ReturnsError()
    {
        // Arrange
        var entry = new DotfileEntry { Source = "dotfiles/bashrc", Target = string.Empty };
        var validator = new DotfileEntryValidator();

        // Act
        var result = validator.Validate(entry, "profiles.default.dotfiles[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("target", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ValidEntry_ReturnsSuccess()
    {
        // Arrange
        var entry = new DotfileEntry { Source = "dotfiles/bashrc", Target = "~/.bashrc" };
        var validator = new DotfileEntryValidator();

        // Act
        var result = validator.Validate(entry, "profiles.default.dotfiles[0]");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
