// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="SnapPackageInstaller"/>.
/// </summary>
public class SnapPackageInstallerTests
{
    private readonly SnapPackageInstaller _installer = new();

    [Fact]
    public void SourceType_ReturnsSnapPackage()
    {
        // Act
        var result = _installer.SourceType;

        // Assert
        result.Should().Be(InstallSourceType.SnapPackage);
    }

    [Fact]
    public async Task InstallAsync_WithEmptySnapList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock();
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

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
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

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
            Snaps = new List<SnapItem>
            {
                new() { Name = "blender", Classic = false }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true, DryRun = true };

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
            Snaps = new List<SnapItem>
            {
                new() { Name = "blender", Classic = false }
            }
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
        results.First().Status.Should().Be(InstallStatus.Warning);
    }

    [Fact]
    public async Task InstallAsync_WithNoSnaps_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock { Snaps = new List<SnapItem>() };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithNullInstallBlock_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        Func<Task> act = async () => await _installer.InstallAsync(null!, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("installBlock");
    }

    [Fact]
    public async Task InstallAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var installBlock = new InstallBlock();

        // Act
        Func<Task> act = async () => await _installer.InstallAsync(installBlock, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task InstallAsync_WithNullSnapsList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock { Snaps = null };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_InterfaceMethod_WithNullContext_ThrowsArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _installer.InstallAsync((InstallContext)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task InstallAsync_InterfaceMethod_ReturnsEmptyList()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var results = await _installer.InstallAsync(context);

        // Assert
        results.Should().BeEmpty();
    }
}
