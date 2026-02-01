// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Linking;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Linking;

/// <summary>
/// Tests for <see cref="SymlinkService"/>.
/// </summary>
public sealed class SymlinkServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly bool _canCreateSymlinks;
    private bool _disposed;

    public SymlinkServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "dottie-symlink-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _canCreateSymlinks = CanCreateSymlinks();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }

        _disposed = true;
    }

    private bool CanCreateSymlinks()
    {
        var testFile = Path.Combine(_testDir, "symlink-test-source.txt");
        var testLink = Path.Combine(_testDir, "symlink-test-link.txt");

        try
        {
            File.WriteAllText(testFile, "test");
            File.CreateSymbolicLink(testLink, testFile);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            if (File.Exists(testLink))
            {
                File.Delete(testLink);
            }

            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    [SkippableFact]
    public void CreateSymlink_WhenTargetDoesNotExist_CreatesSymlink()
    {
        Skip.IfNot(_canCreateSymlinks, "Symlink creation not available on this system");

        // Arrange
        var sourcePath = Path.Combine(_testDir, "source.txt");
        File.WriteAllText(sourcePath, "source content");

        var linkPath = Path.Combine(_testDir, "link.txt");
        var service = new SymlinkService();

        // Act
        var result = service.CreateSymlink(linkPath, sourcePath);

        // Assert
        result.Should().BeTrue();
        File.Exists(linkPath).Should().BeTrue();
        new FileInfo(linkPath).LinkTarget.Should().Be(sourcePath);
    }

    [SkippableFact]
    public void CreateSymlink_WhenParentDirectoryMissing_CreatesDirectoryAndSymlink()
    {
        Skip.IfNot(_canCreateSymlinks, "Symlink creation not available on this system");

        // Arrange
        var sourcePath = Path.Combine(_testDir, "source.txt");
        File.WriteAllText(sourcePath, "source content");

        var nestedLinkPath = Path.Combine(_testDir, "nested", "deep", "link.txt");
        var service = new SymlinkService();

        // Act
        var result = service.CreateSymlink(nestedLinkPath, sourcePath);

        // Assert
        result.Should().BeTrue();
        File.Exists(nestedLinkPath).Should().BeTrue();
        new FileInfo(nestedLinkPath).LinkTarget.Should().Be(sourcePath);
    }

    [SkippableFact]
    public void CreateSymlink_ForDirectory_CreatesDirectorySymlink()
    {
        Skip.IfNot(_canCreateSymlinks, "Symlink creation not available on this system");

        // Arrange
        var sourceDir = Path.Combine(_testDir, "sourcedir");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "file.txt"), "content");

        var linkPath = Path.Combine(_testDir, "linkdir");
        var service = new SymlinkService();

        // Act
        var result = service.CreateSymlink(linkPath, sourceDir);

        // Assert
        result.Should().BeTrue();
        Directory.Exists(linkPath).Should().BeTrue();
        new DirectoryInfo(linkPath).LinkTarget.Should().Be(sourceDir);
    }

    [SkippableFact]
    public void IsCorrectSymlink_WhenPointsToExpectedTarget_ReturnsTrue()
    {
        Skip.IfNot(_canCreateSymlinks, "Symlink creation not available on this system");

        // Arrange
        var sourcePath = Path.Combine(_testDir, "source.txt");
        File.WriteAllText(sourcePath, "source content");

        var linkPath = Path.Combine(_testDir, "link.txt");
        File.CreateSymbolicLink(linkPath, sourcePath);

        var service = new SymlinkService();

        // Act
        var result = service.IsCorrectSymlink(linkPath, sourcePath);

        // Assert
        result.Should().BeTrue();
    }

    [SkippableFact]
    public void IsCorrectSymlink_WhenPointsToDifferentTarget_ReturnsFalse()
    {
        Skip.IfNot(_canCreateSymlinks, "Symlink creation not available on this system");

        // Arrange
        var wrongSource = Path.Combine(_testDir, "wrong.txt");
        File.WriteAllText(wrongSource, "wrong content");

        var expectedSource = Path.Combine(_testDir, "expected.txt");
        File.WriteAllText(expectedSource, "expected content");

        var linkPath = Path.Combine(_testDir, "link.txt");
        File.CreateSymbolicLink(linkPath, wrongSource);

        var service = new SymlinkService();

        // Act
        var result = service.IsCorrectSymlink(linkPath, expectedSource);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCorrectSymlink_WhenNotASymlink_ReturnsFalse()
    {
        // Arrange
        var regularFile = Path.Combine(_testDir, "regular.txt");
        File.WriteAllText(regularFile, "regular content");

        var expectedTarget = Path.Combine(_testDir, "target.txt");
        var service = new SymlinkService();

        // Act
        var result = service.IsCorrectSymlink(regularFile, expectedTarget);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCorrectSymlink_WhenPathDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDir, "nonexistent.txt");
        var targetPath = Path.Combine(_testDir, "target.txt");
        var service = new SymlinkService();

        // Act
        var result = service.IsCorrectSymlink(nonExistentPath, targetPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void LastError_InitiallyNull()
    {
        // Arrange
        var service = new SymlinkService();

        // Assert
        service.LastError.Should().BeNull();
    }

    [Fact]
    public void CreateSymlink_WhenFails_PopulatesLastError()
    {
        // Arrange
        var linkPath = Path.Combine(_testDir, "link.txt");

        // Use invalid target path
        var invalidTargetPath = Path.Combine(_testDir, "nonexistent-source.txt");
        var service = new SymlinkService();

        // Act
        var result = service.CreateSymlink(linkPath, invalidTargetPath);

        // Assert - symlink creation might succeed or fail depending on OS
        // What's important is LastError gets populated on failure
        if (!result)
        {
            service.LastError.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void GetWindowsSymlinkErrorMessage_ReturnsGuidanceMessage()
    {
        // Arrange & Act
        var message = SymlinkService.GetWindowsSymlinkErrorMessage();

        // Assert - per spec FR-021
        message.Should().Contain("Unable to create symbolic link");
        message.Should().Contain("insufficient permissions");
        message.Should().Contain("Administrator");
        message.Should().Contain("Developer Mode");
    }
}
