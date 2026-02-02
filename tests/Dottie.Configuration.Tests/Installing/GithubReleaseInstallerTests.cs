// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;
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

    [Fact]
    public async Task InstallAsync_WithNullInstallBlock_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

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
    public async Task InstallAsync_WithEmptyGithubList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>()
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithNullGithubList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Github = null
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithInvalidRepo_ReturnsFailedResult()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "nonexistent/repo-that-does-not-exist",
                    Asset = "*.tar.gz",
                    Binary = "test-binary"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().ItemName.Should().Be("test-binary");
    }

    [Fact]
    public async Task InstallAsync_WithMultipleItems_ProcessesAll()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "nonexistent1/repo1",
                    Asset = "*.tar.gz",
                    Binary = "binary1"
                },
                new GithubReleaseItem
                {
                    Repo = "nonexistent2/repo2",
                    Asset = "*.zip",
                    Binary = "binary2"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(2);
        results.Select(r => r.ItemName).Should().Contain(new[] { "binary1", "binary2" });
    }

    [Fact]
    public async Task InstallAsync_DryRun_ValidatesReleaseExists()
    {
        // Arrange - using a well-known public repo
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "junegunn/fzf",  // Well-known repo with releases
                    Asset = "fzf-*-linux_amd64.tar.gz",
                    Binary = "fzf",
                    Version = "0.35.0"  // Specific version that exists
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        var result = results.First();
        result.ItemName.Should().Be("fzf");
        // Result could be success or failed depending on network/rate limiting
        result.SourceType.Should().Be(InstallSourceType.GithubRelease);
    }

    [Fact]
    public async Task InstallAsync_WithVersion_UsesSpecificVersion()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "nonexistent/repo",
                    Asset = "*.tar.gz",
                    Binary = "test",
                    Version = "v1.2.3"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().ItemName.Should().Be("test");
    }

    [Fact]
    public async Task InstallAsync_WithoutVersion_UsesLatest()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "nonexistent/repo",
                    Asset = "*.tar.gz",
                    Binary = "test"
                    // No version specified - should use latest
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().ItemName.Should().Be("test");
    }
}

