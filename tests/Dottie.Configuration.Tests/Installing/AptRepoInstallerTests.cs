// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="AptRepoInstaller"/>.
/// </summary>
public class AptRepoInstallerTests
{
    private readonly AptRepoInstaller _installer = new();

    [Fact]
    public void SourceType_ReturnsAptRepo()
    {
        // Act
        var result = _installer.SourceType;

        // Assert
        result.Should().Be(InstallSourceType.AptRepo);
    }

    [Fact]
    public async Task InstallAsync_WithEmptyRepoList_ReturnsEmptyResults()
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
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "vscode",
                    Repo = "deb [arch=amd64,arm64] https://packages.microsoft.com/repos/vscode stable main",
                    KeyUrl = "https://packages.microsoft.com/keys/microsoft.asc",
                    Packages = new List<string> { "code" }
                }
            }
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
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "vscode",
                    Repo = "deb [arch=amd64,arm64] https://packages.microsoft.com/repos/vscode stable main",
                    KeyUrl = "https://packages.microsoft.com/keys/microsoft.asc",
                    Packages = new List<string> { "code" }
                }
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
        results.Should().AllSatisfy(r => r.Status.Should().Be(InstallStatus.Warning));
    }

    [Fact]
    public async Task InstallAsync_WithEmptyRepoList_ReturnsEmptyResults_WhenAptReposIsEmptyList()
    {
        // Arrange
        var installBlock = new InstallBlock { AptRepos = new List<AptRepoItem>() };
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
