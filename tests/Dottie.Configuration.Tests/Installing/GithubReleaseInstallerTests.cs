// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="GithubReleaseInstaller"/>.
/// </summary>
public class GithubReleaseInstallerTests
{
    private readonly GithubReleaseInstaller _installer = new();

    [Fact]
    public void SourceType_ReturnsGithubRelease()
    {
        // Act
        var result = _installer.SourceType;

        // Assert
        result.Should().Be(InstallSourceType.GithubRelease);
    }

    [Fact]
    public async Task InstallAsync_WithEmptyReleasesList_ReturnsEmptyResults()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var results = await _installer.InstallAsync(context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithValidContext_DoesNotThrow()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var action = async () => await _installer.InstallAsync(context, CancellationToken.None);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InstallAsync_WithDryRun_SkipsInstallation()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithoutSudo_SkipsInstallation()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = false };

        // Act
        var results = await _installer.InstallAsync(context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

}
