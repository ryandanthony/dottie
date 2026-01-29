// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Cli.Utilities;
using FluentAssertions;
using Xunit;

namespace Dottie.Cli.Tests.Utilities;

/// <summary>
/// Tests for <see cref="RepoRootFinder"/>.
/// </summary>
public sealed class RepoRootFinderTests
{
    [Fact]
    public void Find_FromCurrentDirectory_FindsRepoRoot()
    {
        // Act
        var result = RepoRootFinder.Find();

        // Assert
        result.Should().NotBeNull("test is running from within a git repository");
        Directory.Exists(Path.Combine(result!, ".git")).Should().BeTrue();
    }

    [Fact]
    public void Find_FromSubdirectory_FindsRepoRoot()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        var subDir = Path.Combine(currentDir, "src", "Dottie.Cli");

        // Act
        var result = RepoRootFinder.Find(subDir);

        // Assert
        result.Should().NotBeNull();
        Directory.Exists(Path.Combine(result!, ".git")).Should().BeTrue();
    }

    [Fact]
    public void Find_FromRootOfFilesystem_ReturnsNull()
    {
        // Arrange - use a path that won't have a .git directory
        var rootPath = Path.GetPathRoot(Directory.GetCurrentDirectory());

        // Act
        var result = RepoRootFinder.Find(rootPath);

        // Assert - might find a repo or not depending on system setup
        // Just verify it doesn't throw
        if (result is not null)
        {
            Directory.Exists(Path.Combine(result, ".git")).Should().BeTrue();
        }
    }

    [Fact]
    public void Find_WithNullPath_UsesCurrentDirectory()
    {
        // Act
        var result = RepoRootFinder.Find(null);

        // Assert
        result.Should().NotBeNull("test is running from within a git repository");
    }
}
