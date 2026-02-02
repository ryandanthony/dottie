// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Tests.Fakes;
using FluentAssertions;
using Flurl.Http.Testing;
using Moq;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="GithubReleaseInstaller"/>.
/// </summary>
public class GithubReleaseInstallerTests : IDisposable
{
    private readonly GithubReleaseInstaller _installer = new();
    private HttpTest? _httpTest;

    public void Dispose()
    {
        _httpTest?.Dispose();
    }

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
        var installBlock = new InstallBlock { Github = new List<GithubReleaseItem>() };
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
    public async Task InstallAsync_WithNullGithubList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock { Github = null };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithEmptyGithubList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock { Github = new List<GithubReleaseItem>() };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = false };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

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

    #region Dry Run Tests with HttpTest

    [Fact]
    public async Task InstallAsync_DryRun_WhenReleaseExists_ReturnsSuccess()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 200);

        var installer = new GithubReleaseInstaller();
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*.tar.gz",
                    Binary = "mybin",
                    Version = "v1.0.0"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);
        results.First().ItemName.Should().Be("mybin");
        results.First().Message.Should().Contain("would be installed");

        _httpTest.ShouldHaveCalled("https://api.github.com/repos/owner/repo/releases/tags/v1.0.0")
            .WithVerb(HttpMethod.Head)
            .Times(1);
    }

    [Fact]
    public async Task InstallAsync_DryRun_WhenReleaseNotFound_ReturnsFailed()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 404);

        var installer = new GithubReleaseInstaller();
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/nonexistent",
                    Asset = "*.tar.gz",
                    Binary = "mybin"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("404");
    }

    [Fact]
    public async Task InstallAsync_DryRun_WithoutVersion_UsesLatestEndpoint()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 200);

        var installer = new GithubReleaseInstaller();
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*.tar.gz",
                    Binary = "mybin"
                    // No version - should use latest
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        await installer.InstallAsync(installBlock, context);

        // Assert
        _httpTest.ShouldHaveCalled("https://api.github.com/repos/owner/repo/releases/latest")
            .WithVerb(HttpMethod.Head)
            .Times(1);
    }

    [Fact]
    public async Task InstallAsync_DryRun_WithVersion_UsesTagEndpoint()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 200);

        var installer = new GithubReleaseInstaller();
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*.tar.gz",
                    Binary = "mybin",
                    Version = "v2.0.0"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        await installer.InstallAsync(installBlock, context);

        // Assert
        _httpTest.ShouldHaveCalled("https://api.github.com/repos/owner/repo/releases/tags/v2.0.0")
            .Times(1);
    }

    [Fact]
    public async Task InstallAsync_DryRun_SetsUserAgentHeader()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 200);

        var installer = new GithubReleaseInstaller();
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*.tar.gz",
                    Binary = "mybin"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        await installer.InstallAsync(installBlock, context);

        // Assert
        _httpTest.ShouldHaveCalled("*")
            .WithHeader("User-Agent", "dottie-dotfiles*")
            .Times(1);
    }

    [Fact]
    public async Task InstallAsync_DryRun_WhenHttpException_ReturnsFailed()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.SimulateTimeout();

        var installer = new GithubReleaseInstaller();
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*.tar.gz",
                    Binary = "mybin"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("Failed to verify");
    }

    [Fact]
    public async Task InstallAsync_DryRun_MultipleItems_ProcessesAll()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest
            .RespondWith(status: 200)  // First item succeeds
            .RespondWith(status: 404); // Second item not found

        var installer = new GithubReleaseInstaller();
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem { Repo = "owner/exists", Asset = "*.tar.gz", Binary = "bin1" },
                new GithubReleaseItem { Repo = "owner/notexists", Asset = "*.tar.gz", Binary = "bin2" }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(2);
        results.Should().ContainSingle(r => r.Status == InstallStatus.Success && r.ItemName == "bin1");
        results.Should().ContainSingle(r => r.Status == InstallStatus.Failed && r.ItemName == "bin2");
    }

    #endregion

    #region Full Install Tests with HttpTest and FakeProcessRunner

    [Fact]
    public async Task InstallAsync_WhenReleaseApiReturnsNull_ReturnsFailed()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 404); // Will cause GetGithubReleaseAsync to return null

        var mockDownloader = new Mock<HttpDownloader>();
        var fakeRunner = new FakeProcessRunner();
        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);

        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*.tar.gz",
                    Binary = "mybin"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("not found");
    }

    [Fact]
    public async Task InstallAsync_WhenAssetNotMatched_ReturnsFailed()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            assets = new[]
            {
                new { name = "app-windows.exe", browser_download_url = "https://example.com/app-windows.exe" }
            }
        });

        var mockDownloader = new Mock<HttpDownloader>();
        var fakeRunner = new FakeProcessRunner();
        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);

        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*-linux.tar.gz",  // Won't match
                    Binary = "mybin"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("No asset matching pattern");
    }

    [Fact]
    public async Task InstallAsync_WhenDownloadFails_ReturnsFailed()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            assets = new[]
            {
                new { name = "app-linux.tar.gz", browser_download_url = "https://example.com/app-linux.tar.gz" }
            }
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Download failed"));

        var fakeRunner = new FakeProcessRunner();
        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);

        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*-linux.tar.gz",
                    Binary = "mybin"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("Failed to download");
    }

    [Fact]
    public async Task InstallAsync_WithGlobPattern_MatchesCorrectAsset()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            assets = new[]
            {
                new { name = "app-v1.0.0-windows-amd64.zip", browser_download_url = "https://example.com/win.zip" },
                new { name = "app-v1.0.0-linux-amd64.tar.gz", browser_download_url = "https://example.com/linux.tar.gz" },
                new { name = "app-v1.0.0-darwin-amd64.tar.gz", browser_download_url = "https://example.com/darwin.tar.gz" }
            }
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync("https://example.com/linux.tar.gz", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x1f, 0x8b }); // Fake tar.gz header

        var fakeRunner = new FakeProcessRunner().WithSuccessResult();
        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);

        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*-linux-amd64.tar.gz",
                    Binary = "app"
                }
            }
        };

        var tempDir = Path.Combine(Path.GetTempPath(), $"test-bin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var context = new InstallContext
            {
                RepoRoot = "/repo",
                DryRun = false,
                HasSudo = true,
                BinDirectory = tempDir
            };

            // Act
            await installer.InstallAsync(installBlock, context);

            // Assert - verify the correct asset URL was downloaded
            mockDownloader.Verify(d => d.DownloadAsync("https://example.com/linux.tar.gz", It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task InstallAsync_ParsesGithubApiResponse_Correctly()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v1.2.3",
            assets = new[]
            {
                new
                {
                    name = "myapp-linux-amd64",
                    browser_download_url = "https://github.com/owner/repo/releases/download/v1.2.3/myapp-linux-amd64"
                }
            }
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x7f, 0x45, 0x4c, 0x46 }); // ELF header

        var fakeRunner = new FakeProcessRunner().WithSuccessResult();
        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);

        var tempDir = Path.Combine(Path.GetTempPath(), $"test-bin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var installBlock = new InstallBlock
            {
                Github = new List<GithubReleaseItem>
                {
                    new GithubReleaseItem
                    {
                        Repo = "owner/repo",
                        Asset = "myapp-linux-amd64",
                        Binary = "myapp"
                    }
                }
            };
            var context = new InstallContext
            {
                RepoRoot = "/repo",
                DryRun = false,
                HasSudo = true,
                BinDirectory = tempDir
            };

            // Act
            var results = await installer.InstallAsync(installBlock, context);

            // Assert
            results.Should().HaveCount(1);
            results.First().Status.Should().Be(InstallStatus.Success);
            results.First().ItemName.Should().Be("myapp");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullParameters_CreatesDefaults()
    {
        // Act
        var installer = new GithubReleaseInstaller(null, null);

        // Assert
        installer.Should().NotBeNull();
        installer.SourceType.Should().Be(InstallSourceType.GithubRelease);
    }

    [Fact]
    public void Constructor_WithCustomDownloader_UsesProvided()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();

        // Act
        var installer = new GithubReleaseInstaller(mockDownloader.Object);

        // Assert
        installer.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomProcessRunner_UsesProvided()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner();

        // Act
        var installer = new GithubReleaseInstaller(null, fakeRunner);

        // Assert
        installer.Should().NotBeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task InstallAsync_WhenExceptionThrown_ReturnsFailedResult()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            assets = new[]
            {
                new { name = "app.tar.gz", browser_download_url = "https://example.com/app.tar.gz" }
            }
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        var installer = new GithubReleaseInstaller(mockDownloader.Object);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*.tar.gz",
                    Binary = "mybin"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
    }

    [Fact]
    public async Task InstallAsync_WhenJsonParsingFails_ReturnsNull()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith("not valid json", status: 200);

        var installer = new GithubReleaseInstaller();
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "*.tar.gz",
                    Binary = "mybin"
                }
            }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("not found");
    }

    #endregion
}
