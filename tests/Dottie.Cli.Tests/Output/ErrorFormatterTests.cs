// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Cli.Output;
using Dottie.Configuration.Validation;
using FluentAssertions;

namespace Dottie.Cli.Tests.Output;

/// <summary>
/// Tests for <see cref="ErrorFormatter"/>.
/// </summary>
public class ErrorFormatterTests
{
    [Fact]
    public void WriteErrors_WithNullErrors_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<ValidationError>? errors = null;

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors!))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void WriteErrors_WithEmptyList_DoesNotThrow()
    {
        // Arrange
        var errors = new List<ValidationError>();

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void WriteErrors_WithSingleError_DoesNotThrow()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("profiles.default.dotfiles", "Source file not found", 10, 5),
        };

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void WriteErrors_WithMultipleErrors_DoesNotThrow()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("profiles.default.dotfiles[0]", "Source file not found", 10, 5),
            new("profiles.work", "Profile extends non-existent profile", 20, null),
            new(string.Empty, "Invalid YAML syntax", null, null),
        };

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void WriteErrors_WithErrorContainingPath_DoesNotThrow()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("profiles.default.dotfiles[0].source", "File does not exist", null, null),
        };

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void WriteErrors_WithErrorContainingLineOnly_DoesNotThrow()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("profiles.yaml", "Invalid syntax", 42, null),
        };

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void WriteErrors_WithErrorContainingLineAndColumn_DoesNotThrow()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("dottie.yaml", "Expected colon", 15, 8),
        };

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void WriteErrors_WithErrorContainingNoLocationInfo_DoesNotThrow()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new(string.Empty, "General validation error", null, null),
        };

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void WriteErrors_WithSpecialCharactersInMessage_DoesNotThrow()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("path", "Message with [brackets] and <angles>", null, null),
        };

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void WriteErrors_WithNullPath_DoesNotThrow()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new(null!, "Error message", 10, 5),
        };

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors))
            .Should()
            .NotThrow();
    }

    [Fact]
    public void WriteErrors_WithMixedErrors_DoesNotThrow()
    {
        // Arrange
        var errors = new List<ValidationError>
        {
            new("profiles.default", "Error with path only", null, null),
            new(string.Empty, "Error with line only", 10, null),
            new("profiles.work", "Error with line and column", 20, 15),
            new(string.Empty, "Error with no location", null, null),
        };

        // Act & Assert
        FluentActions.Invoking(() => ErrorFormatter.WriteErrors(errors))
            .Should()
            .NotThrow();
    }
}
