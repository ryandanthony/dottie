// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Utilities;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Utilities;

/// <summary>
/// Tests for <see cref="PathExpander"/>.
/// </summary>
public class PathExpanderTests
{
    [Fact]
    public void Expand_TildePath_ReturnsHomeDirectory()
    {
        // Arrange
        var path = "~/.bashrc";
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var result = PathExpander.Expand(path);

        // Assert
        result.Should().StartWith(homeDir);
        result.Should().EndWith(".bashrc");
    }

    [Fact]
    public void Expand_AbsolutePath_ReturnsUnchanged()
    {
        // Arrange
        var path = "/home/user/.bashrc";

        // Act
        var result = PathExpander.Expand(path);

        // Assert
        result.Should().Be(path);
    }

    [Fact]
    public void Expand_RelativePath_ReturnsUnchanged()
    {
        // Arrange
        var path = "dotfiles/bashrc";

        // Act
        var result = PathExpander.Expand(path);

        // Assert
        result.Should().Be(path);
    }

    [Fact]
    public void Expand_TildeOnlyPath_ReturnsHomeDirectory()
    {
        // Arrange
        var path = "~";
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var result = PathExpander.Expand(path);

        // Assert
        result.Should().Be(homeDir);
    }
}
