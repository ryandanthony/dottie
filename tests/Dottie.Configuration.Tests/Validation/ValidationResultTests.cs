// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Validation;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Validation;

/// <summary>
/// Tests for <see cref="ValidationResult"/>.
/// </summary>
public class ValidationResultTests
{
    [Fact]
    public void Success_ReturnsValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithParamsErrors_ReturnsInvalidResult()
    {
        // Arrange
        var error1 = new ValidationError("path1", "Error 1");
        var error2 = new ValidationError("path2", "Error 2");

        // Act
        var result = ValidationResult.Failure(error1, error2);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(error1);
        result.Errors.Should().Contain(error2);
    }

    [Fact]
    public void Failure_WithEnumerableErrors_ReturnsInvalidResult()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("path1", "Error 1"),
            new("path2", "Error 2"),
        };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void IsValid_WithEmptyErrorsList_ReturnsTrue()
    {
        // Arrange
        var result = new ValidationResult();

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithErrors_ReturnsFalse()
    {
        // Arrange
        var result = new ValidationResult
        {
            Errors = [new ValidationError("path", "Error")],
        };

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
