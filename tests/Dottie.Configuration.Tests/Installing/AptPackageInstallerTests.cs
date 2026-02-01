// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="AptPackageInstaller"/>.
/// </summary>
public class AptPackageInstallerTests
{
    private readonly AptPackageInstaller _installer = new();

    [Fact]
    public void SourceType_ReturnsAptPackage()
    {
        // Act
        var result = _installer.SourceType;

        // Assert
        result.Should().Be(InstallSourceType.AptPackage);
    }

    [Fact]
    public async Task InstallAsync_WithEmptyPackageList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock();
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithValidContext_DoesNotThrow()
    {
        // Arrange
        var installBlock = new InstallBlock();
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var action = async () => await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InstallAsync_WithDryRun_SkipsInstallation()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Apt = new List<string> { "git", "curl" }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithoutSudo_ReturnsWarningResults()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Apt = new List<string> { "git", "curl" }
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = false
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Status.Should().Be(InstallStatus.Warning));
    }

    [Fact]
    public async Task InstallAsync_WithEmptyPackageList_ReturnsEmptyResults_WhenAptIsEmptyList()
    {
        // Arrange
        var installBlock = new InstallBlock { Apt = new List<string>() };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }
}
