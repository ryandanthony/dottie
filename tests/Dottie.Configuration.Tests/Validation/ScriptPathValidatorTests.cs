// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Validation;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Validation;

/// <summary>
/// Tests for <see cref="ScriptPathValidator"/>.
/// </summary>
public class ScriptPathValidatorTests
{
    private readonly ScriptPathValidator _validator = new();

    [Fact]
    public void Validate_PathWithinRepo_ReturnsSuccess()
    {
        // Arrange
        var path = "scripts/setup.sh";

        // Act
        var result = _validator.Validate(path, "profiles.default.install.scripts[0]");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NestedPathWithinRepo_ReturnsSuccess()
    {
        // Arrange
        var path = "scripts/install/packages.sh";

        // Act
        var result = _validator.Validate(path, "profiles.default.install.scripts[0]");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AbsolutePath_ReturnsError()
    {
        // Arrange
        var path = "/etc/passwd";

        // Act
        var result = _validator.Validate(path, "profiles.default.install.scripts[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("absolute", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ParentTraversal_ReturnsError()
    {
        // Arrange
        var path = "../../../etc/passwd";

        // Act
        var result = _validator.Validate(path, "profiles.default.install.scripts[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("traversal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_HiddenParentTraversal_ReturnsError()
    {
        // Arrange - path that looks relative but escapes
        var path = "scripts/../../../etc/passwd";

        // Act
        var result = _validator.Validate(path, "profiles.default.install.scripts[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("traversal", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WindowsAbsolutePath_ReturnsError()
    {
        // Arrange
        var path = "C:\\Windows\\System32\\cmd.exe";

        // Act
        var result = _validator.Validate(path, "profiles.default.install.scripts[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("absolute", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_EmptyPath_ReturnsError()
    {
        // Arrange
        var path = string.Empty;

        // Act
        var result = _validator.Validate(path, "profiles.default.install.scripts[0]");

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
