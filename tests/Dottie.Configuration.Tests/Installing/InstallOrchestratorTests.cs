// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;
using Moq;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="InstallOrchestrator"/>.
/// </summary>
public class InstallOrchestratorTests
{
    private readonly InstallOrchestrator _orchestrator;
    private readonly Mock<IInstallSource> _mockInstaller;

    public InstallOrchestratorTests()
    {
        _mockInstaller = new Mock<IInstallSource>();
        _orchestrator = new InstallOrchestrator(new[] { _mockInstaller.Object });
    }

    [Fact]
    public async Task InstallAsync_WithEmptyInstallBlock_ReturnsEmptyResults()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo" };
        var installBlock = new InstallBlock();

        // Act
        var results = await _orchestrator.InstallAsync(context, installBlock);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_CallsAllRegisteredInstallers()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo" };
        var installBlock = new InstallBlock();

        _mockInstaller
            .Setup(x => x.InstallAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                InstallResult.Success("test-pkg", InstallSourceType.GithubRelease)
            });

        // Act
        var results = await _orchestrator.InstallAsync(context, installBlock);

        // Assert
        _mockInstaller.Verify(x => x.InstallAsync(context, It.IsAny<CancellationToken>()), Times.Once);
        results.Should().HaveCount(1);
    }

    [Fact]
    public async Task InstallAsync_AggregatesResultsFromMultipleInstallers()
    {
        // Arrange
        var installer2 = new Mock<IInstallSource>();
        var orchestrator = new InstallOrchestrator(new[] { _mockInstaller.Object, installer2.Object });
        var context = new InstallContext { RepoRoot = "/repo" };
        var installBlock = new InstallBlock();

        _mockInstaller
            .Setup(x => x.InstallAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { InstallResult.Success("pkg1", InstallSourceType.GithubRelease) });

        installer2
            .Setup(x => x.InstallAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { InstallResult.Success("pkg2", InstallSourceType.AptPackage) });

        // Act
        var results = await orchestrator.InstallAsync(context, installBlock);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task InstallAsync_WithDryRun_PassesContextToInstallers()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };
        var installBlock = new InstallBlock();

        _mockInstaller
            .Setup(x => x.InstallAsync(It.Is<InstallContext>(c => c.DryRun), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<InstallResult>());

        // Act
        await _orchestrator.InstallAsync(context, installBlock);

        // Assert
        _mockInstaller.Verify(
            x => x.InstallAsync(It.Is<InstallContext>(c => c.DryRun), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InstallAsync_ContinuesEvenIfOneInstallerFails()
    {
        // Arrange
        var installer2 = new Mock<IInstallSource>();
        var orchestrator = new InstallOrchestrator(new[] { _mockInstaller.Object, installer2.Object });
        var context = new InstallContext { RepoRoot = "/repo" };
        var installBlock = new InstallBlock();

        _mockInstaller
            .Setup(x => x.InstallAsync(context, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Installer error"));

        installer2
            .Setup(x => x.InstallAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { InstallResult.Success("pkg2", InstallSourceType.AptPackage) });

        // Act
        var results = await orchestrator.InstallAsync(context, installBlock);

        // Assert
        results.Should().HaveCount(1);
        results.First().ItemName.Should().Be("pkg2");
    }
}
