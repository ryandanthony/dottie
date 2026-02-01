// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="InstallResult"/>.
/// </summary>
public class InstallResultTests
{
    [Fact]
    public void Success_CreatesResultWithSuccessStatus()
    {
        // Arrange
        var name = "test-package";
        var sourceType = InstallSourceType.GithubRelease;
        var path = "/home/user/bin/test";

        // Act
        var result = InstallResult.Success(name, sourceType, path, "v1.0.0 installed");

        // Assert
        result.ItemName.Should().Be(name);
        result.SourceType.Should().Be(sourceType);
        result.Status.Should().Be(InstallStatus.Success);
        result.InstalledPath.Should().Be(path);
        result.Message.Should().Be("v1.0.0 installed");
    }

    [Fact]
    public void Success_WithoutPathAndMessage_CreatesValidResult()
    {
        // Arrange
        var name = "test-package";
        var sourceType = InstallSourceType.AptPackage;

        // Act
        var result = InstallResult.Success(name, sourceType);

        // Assert
        result.ItemName.Should().Be(name);
        result.SourceType.Should().Be(sourceType);
        result.Status.Should().Be(InstallStatus.Success);
        result.InstalledPath.Should().BeNull();
        result.Message.Should().BeNull();
    }

    [Fact]
    public void Skipped_CreatesResultWithSkippedStatus()
    {
        // Arrange
        var name = "test-package";
        var sourceType = InstallSourceType.Font;
        var reason = "Font already installed";

        // Act
        var result = InstallResult.Skipped(name, sourceType, reason);

        // Assert
        result.ItemName.Should().Be(name);
        result.SourceType.Should().Be(sourceType);
        result.Status.Should().Be(InstallStatus.Skipped);
        result.Message.Should().Be(reason);
        result.InstalledPath.Should().BeNull();
    }

    [Fact]
    public void Warning_CreatesResultWithWarningStatus()
    {
        // Arrange
        var name = "test-package";
        var sourceType = InstallSourceType.AptRepo;
        var reason = "Sudo not available, skipping repo setup";

        // Act
        var result = InstallResult.Warning(name, sourceType, reason);

        // Assert
        result.ItemName.Should().Be(name);
        result.SourceType.Should().Be(sourceType);
        result.Status.Should().Be(InstallStatus.Warning);
        result.Message.Should().Be(reason);
        result.InstalledPath.Should().BeNull();
    }

    [Fact]
    public void Failed_CreatesResultWithFailedStatus()
    {
        // Arrange
        var name = "test-package";
        var sourceType = InstallSourceType.SnapPackage;
        var error = "Download failed: 404 Not Found";

        // Act
        var result = InstallResult.Failed(name, sourceType, error);

        // Assert
        result.ItemName.Should().Be(name);
        result.SourceType.Should().Be(sourceType);
        result.Status.Should().Be(InstallStatus.Failed);
        result.Message.Should().Be(error);
        result.InstalledPath.Should().BeNull();
    }

    [Fact]
    public void InstallStatusEnum_HasCorrectValues()
    {
        // Assert
        ((int)InstallStatus.Success).Should().Be(0);
        ((int)InstallStatus.Skipped).Should().Be(1);
        ((int)InstallStatus.Warning).Should().Be(2);
        ((int)InstallStatus.Failed).Should().Be(3);
    }

    [Fact]
    public void InstallSourceTypeEnum_HasCorrectPriorityOrder()
    {
        // Assert - values represent priority order
        ((int)InstallSourceType.GithubRelease).Should().Be(1);
        ((int)InstallSourceType.AptPackage).Should().Be(2);
        ((int)InstallSourceType.AptRepo).Should().Be(3);
        ((int)InstallSourceType.Script).Should().Be(4);
        ((int)InstallSourceType.Font).Should().Be(5);
        ((int)InstallSourceType.SnapPackage).Should().Be(6);
    }

    [Fact]
    public void InstallResult_IsRecord_SupportedEquality()
    {
        // Arrange
        var result1 = InstallResult.Success("pkg1", InstallSourceType.GithubRelease, "/path");
        var result2 = InstallResult.Success("pkg1", InstallSourceType.GithubRelease, "/path");
        var result3 = InstallResult.Success("pkg2", InstallSourceType.GithubRelease, "/path");

        // Act & Assert
        result1.Should().Be(result2);
        result1.Should().NotBe(result3);
    }
}
