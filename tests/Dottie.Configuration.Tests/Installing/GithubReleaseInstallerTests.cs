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

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithEmptyReleasesList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installBlock = new InstallBlock { Github = new List<GithubReleaseItem>() };
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithValidContext_DoesNotThrowAsync()
    {
        // Arrange
        var installBlock = new InstallBlock();
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var action = async () => await _installer.InstallAsync(installBlock, context, null, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await action.Should().NotThrowAsync();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithNullGithubList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installBlock = new InstallBlock { Github = null };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithEmptyGithubList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installBlock = new InstallBlock { Github = new List<GithubReleaseItem>() };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = false };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithNullInstallBlock_ThrowsArgumentNullExceptionAsync()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        Func<Task> act = async () => await _installer.InstallAsync(null!, context, null).ConfigureAwait(false);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("installBlock");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithNullContext_ThrowsArgumentNullExceptionAsync()
    {
        // Arrange
        var installBlock = new InstallBlock();

        // Act
        Func<Task> act = async () => await _installer.InstallAsync(installBlock, null!, null).ConfigureAwait(false);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }


    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_WhenReleaseExists_ReturnsSuccessAsync()
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);
        results.First().ItemName.Should().Be("mybin");
        results.First().Message.Should().Contain("would be installed");

        _httpTest.ShouldHaveCalled("https://api.github.com/repos/owner/repo/releases/tags/v1.0.0")
            .WithVerb(HttpMethod.Head)
            .Times(1);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_WhenReleaseNotFound_ReturnsFailedAsync()
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("404");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_WithoutVersion_UsesLatestEndpointAsync()
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        await installer.InstallAsync(installBlock, context, null);

        // Assert
        _httpTest.ShouldHaveCalled("https://api.github.com/repos/owner/repo/releases/latest")
            .WithVerb(HttpMethod.Head)
            .Times(1);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_WithVersion_UsesTagEndpointAsync()
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        await installer.InstallAsync(installBlock, context, null);

        // Assert
        _httpTest.ShouldHaveCalled("https://api.github.com/repos/owner/repo/releases/tags/v2.0.0")
            .Times(1);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_SetsUserAgentHeaderAsync()
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        await installer.InstallAsync(installBlock, context, null);

        // Assert
        _httpTest.ShouldHaveCalled("*")
            .WithHeader("User-Agent", "dottie-dotfiles*")
            .Times(1);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_WhenHttpException_ReturnsFailedAsync()
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("Failed to verify");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_MultipleItems_ProcessesAllAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest
            .RespondWith(status: 200) // First item succeeds
            .RespondWith(status: 404); // Second item not found

        var installer = new GithubReleaseInstaller();
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem { Repo = "owner/exists", Asset = "*.tar.gz", Binary = "bin1" },
                new GithubReleaseItem { Repo = "owner/notexists", Asset = "*.tar.gz", Binary = "bin2" }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(2);
        results.Should().ContainSingle(r => r.Status == InstallStatus.Success && r.ItemName == "bin1");
        results.Should().ContainSingle(r => r.Status == InstallStatus.Failed && r.ItemName == "bin2");
    }



    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenReleaseApiReturnsNull_ReturnsFailedAsync()
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("not found");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenAssetNotMatched_ReturnsFailedAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            assets = new[]
            {
                new { name = "app-windows.exe", browser_download_url = "https://example.com/app-windows.exe" }
            },
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("No asset matching pattern");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenDownloadFails_ReturnsFailedAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            assets = new[]
            {
                new { name = "app-linux.tar.gz", browser_download_url = "https://example.com/app-linux.tar.gz" }
            },
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("Failed to download");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithGlobPattern_MatchesCorrectAssetAsync()
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
            },
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
            },
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
                BinDirectory = tempDir,
            };

            // Act
            await installer.InstallAsync(installBlock, context, null);

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

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_ParsesGithubApiResponse_CorrectlyAsync()
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
            },
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
                },
            };
            var context = new InstallContext
            {
                RepoRoot = "/repo",
                DryRun = false,
                HasSudo = true,
                BinDirectory = tempDir,
            };

            // Act
            var results = await installer.InstallAsync(installBlock, context, null);

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



    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenExceptionThrown_ReturnsFailedResultAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            assets = new[]
            {
                new { name = "app.tar.gz", browser_download_url = "https://example.com/app.tar.gz" }
            },
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenJsonParsingFails_ReturnsNullAsync()
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("not found");
    }

    #region Binary Existence Detection Tests (T003, T013-T015)

    /// <summary>
    /// Tests that IsBinaryInstalled returns true when the binary exists in ~/bin/.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenBinaryExistsInBinDirectory_SkipsInstallationAsync()
    {
        // Arrange
        var tempBinDir = Path.Combine(Path.GetTempPath(), $"dottie-test-bin-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempBinDir);
            var binaryPath = Path.Combine(tempBinDir, "mybin");
            await File.WriteAllTextAsync(binaryPath, "binary content");

            var processRunner = new FakeProcessRunner();
            var installer = new GithubReleaseInstaller(processRunner: processRunner);
            var installBlock = new InstallBlock
            {
                Github = new List<GithubReleaseItem>
                {
                    new GithubReleaseItem
                    {
                        Repo = "owner/repo",
                        Asset = "*.tar.gz",
                        Binary = "mybin",
                    },
                },
            };
            var context = new InstallContext { RepoRoot = "/repo", BinDirectory = tempBinDir, DryRun = false };

            // Act
            var results = await installer.InstallAsync(installBlock, context, null);

            // Assert
            results.Should().HaveCount(1);
            results.First().Status.Should().Be(InstallStatus.Skipped);
            results.First().Message.Should().Contain("Already installed");
        }
        finally
        {
            if (Directory.Exists(tempBinDir))
            {
                Directory.Delete(tempBinDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that IsBinaryInstalled returns true when binary is found via PATH (using which command).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenBinaryExistsInPath_SkipsInstallationAsync()
    {
        // Arrange
        var tempBinDir = Path.Combine(Path.GetTempPath(), $"dottie-test-bin-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempBinDir);
            // Binary NOT in ~/bin/ but will be found via 'which'

            var processRunner = new FakeProcessRunner()
                .WithResult(ProcessResult.Succeeded("/usr/local/bin/mybin")); // which returns path

            var installer = new GithubReleaseInstaller(processRunner: processRunner);
            var installBlock = new InstallBlock
            {
                Github = new List<GithubReleaseItem>
                {
                    new GithubReleaseItem
                    {
                        Repo = "owner/repo",
                        Asset = "*.tar.gz",
                        Binary = "mybin",
                    },
                },
            };
            var context = new InstallContext { RepoRoot = "/repo", BinDirectory = tempBinDir, DryRun = false };

            // Act
            var results = await installer.InstallAsync(installBlock, context, null);

            // Assert
            results.Should().HaveCount(1);
            results.First().Status.Should().Be(InstallStatus.Skipped);
            results.First().Message.Should().Contain("Already installed");
        }
        finally
        {
            if (Directory.Exists(tempBinDir))
            {
                Directory.Delete(tempBinDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that installation proceeds when binary is not found anywhere.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenBinaryNotFound_ProceedsWithInstallationAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            assets = new[]
            {
                new { name = "app-linux-amd64.tar.gz", browser_download_url = "https://example.com/app.tar.gz" },
            },
        });

        var tempBinDir = Path.Combine(Path.GetTempPath(), $"dottie-test-bin-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempBinDir);
            // Binary NOT in ~/bin/

            var processRunner = new FakeProcessRunner()
                .WithResult(ProcessResult.Failed(1, "not found")); // which returns non-zero

            var mockDownloader = new Mock<HttpDownloader>();
            mockDownloader
                .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Simulated download for test"));

            var installer = new GithubReleaseInstaller(mockDownloader.Object, processRunner);
            var installBlock = new InstallBlock
            {
                Github = new List<GithubReleaseItem>
                {
                    new GithubReleaseItem
                    {
                        Repo = "owner/repo",
                        Asset = "*.tar.gz",
                        Binary = "mybin",
                    },
                },
            };
            var context = new InstallContext { RepoRoot = "/repo", BinDirectory = tempBinDir, DryRun = false };

            // Act
            var results = await installer.InstallAsync(installBlock, context, null);

            // Assert - It should attempt to download (and fail due to mock), proving it didn't skip
            results.Should().HaveCount(1);
            results.First().Status.Should().Be(InstallStatus.Failed);
            results.First().Message.Should().Contain("download");
        }
        finally
        {
            if (Directory.Exists(tempBinDir))
            {
                Directory.Delete(tempBinDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that dry-run checks system state and shows "would skip" for already-installed binaries.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_WhenBinaryAlreadyInstalled_ShowsWouldSkipAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 200);

        var tempBinDir = Path.Combine(Path.GetTempPath(), $"dottie-test-bin-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempBinDir);
            var binaryPath = Path.Combine(tempBinDir, "mybin");
            await File.WriteAllTextAsync(binaryPath, "binary content");

            var processRunner = new FakeProcessRunner();
            var installer = new GithubReleaseInstaller(processRunner: processRunner);
            var installBlock = new InstallBlock
            {
                Github = new List<GithubReleaseItem>
                {
                    new GithubReleaseItem
                    {
                        Repo = "owner/repo",
                        Asset = "*.tar.gz",
                        Binary = "mybin",
                        Version = "v1.0.0",
                    },
                },
            };
            var context = new InstallContext { RepoRoot = "/repo", BinDirectory = tempBinDir, DryRun = true };

            // Act
            var results = await installer.InstallAsync(installBlock, context, null);

            // Assert
            results.Should().HaveCount(1);
            results.First().Status.Should().Be(InstallStatus.Skipped);
            results.First().Message.Should().Contain("Already installed");
        }
        finally
        {
            if (Directory.Exists(tempBinDir))
            {
                Directory.Delete(tempBinDir, recursive: true);
            }
        }
    }

    /// <summary>
    /// Tests that dry-run checks system state and shows "would install" for binaries not found.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_WhenBinaryNotInstalled_ShowsWouldInstallAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 200);

        var tempBinDir = Path.Combine(Path.GetTempPath(), $"dottie-test-bin-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempBinDir);
            // Binary NOT in ~/bin/

            var processRunner = new FakeProcessRunner()
                .WithResult(ProcessResult.Failed(1, "not found")); // which returns non-zero

            var installer = new GithubReleaseInstaller(processRunner: processRunner);
            var installBlock = new InstallBlock
            {
                Github = new List<GithubReleaseItem>
                {
                    new GithubReleaseItem
                    {
                        Repo = "owner/repo",
                        Asset = "*.tar.gz",
                        Binary = "mybin",
                        Version = "v1.0.0",
                    },
                },
            };
            var context = new InstallContext { RepoRoot = "/repo", BinDirectory = tempBinDir, DryRun = true };

            // Act
            var results = await installer.InstallAsync(installBlock, context, null);

            // Assert
            results.Should().HaveCount(1);
            results.First().Status.Should().Be(InstallStatus.Success);
            results.First().Message.Should().Contain("would be installed");
        }
        finally
        {
            if (Directory.Exists(tempBinDir))
            {
                Directory.Delete(tempBinDir, recursive: true);
            }
        }
    }

    #endregion

    #region RELEASE_VERSION Variable Resolution Tests (T024)

    /// <summary>
    /// Verifies that ${RELEASE_VERSION} in asset pattern is resolved with the specified version
    /// during dry-run validation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_WithReleaseVersionVariable_ResolvesVersionInAssetPatternAsync()
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
                    Repo = "owner/tool",
                    Asset = "tool-${RELEASE_VERSION}-linux_amd64.tar.gz",
                    Binary = "tool",
                    Version = "v1.2.0",
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert - dry run should succeed since we're just validating the release exists
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);
    }

    #endregion

    #region Type: Deb Installation Tests (US1 - T009-T011)

    /// <summary>
    /// T009: Verify type: deb downloads and installs via dpkg.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeDeb_DownloadsAndInstallsViaDpkgAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v1.0.0",
            assets = new[]
            {
                new { name = "app-arm64.deb", browser_download_url = "https://example.com/app-arm64.deb" },
            },
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync("https://example.com/app-arm64.deb", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0xDE, 0xB0 }); // Fake .deb bytes

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Succeeded("/usr/bin/dpkg")) // which dpkg
            .WithResult(ProcessResult.Succeeded("drawio"))         // dpkg-deb -W (package name)
            .WithResult(ProcessResult.Failed(1, "not installed"))  // dpkg -s (not installed)
            .WithResult(ProcessResult.Succeeded())                 // sudo dpkg -i
            .WithResult(ProcessResult.Succeeded());                // sudo apt-get install -f -y

        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "jgraph/drawio-desktop",
                    Asset = "app-arm64.deb",
                    Type = GithubReleaseAssetType.Deb,
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);
        results.First().Message.Should().Contain("installed via dpkg");

        // Verify dpkg -i was called
        fakeRunner.Calls.Should().Contain(c => c.FileName == "sudo" && c.Arguments.Contains("dpkg -i"));
        // Verify apt-get install -f was called
        fakeRunner.Calls.Should().Contain(c => c.FileName == "sudo" && c.Arguments.Contains("apt-get install -f -y"));
    }

    /// <summary>
    /// T010: Verify type: deb with version downloads specific release.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeDeb_WithVersion_DownloadsSpecificReleaseAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v29.3.6",
            assets = new[]
            {
                new { name = "drawio-arm64-29.3.6.deb", browser_download_url = "https://example.com/drawio.deb" },
            },
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0xDE, 0xB0 });

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Succeeded("/usr/bin/dpkg")) // which dpkg
            .WithResult(ProcessResult.Succeeded("drawio"))         // dpkg-deb -W
            .WithResult(ProcessResult.Failed(1, "not installed"))  // dpkg -s
            .WithResult(ProcessResult.Succeeded())                 // sudo dpkg -i
            .WithResult(ProcessResult.Succeeded());                // sudo apt-get install -f -y

        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "jgraph/drawio-desktop",
                    Asset = "drawio-arm64-*.deb",
                    Type = GithubReleaseAssetType.Deb,
                    Version = "v29.3.6",
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);

        // Verify specific version tag was used in API call
        _httpTest.ShouldHaveCalled("https://api.github.com/repos/jgraph/drawio-desktop/releases/tags/v29.3.6");
    }

    /// <summary>
    /// T011: Verify variable substitution works for deb asset patterns.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeDeb_VariableSubstitution_ResolvesAssetPatternAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v2.0.0",
            assets = new[]
            {
                new { name = "app-2.0.0-amd64.deb", browser_download_url = "https://example.com/app.deb" },
            },
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0xDE, 0xB0 });

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Succeeded("/usr/bin/dpkg")) // which dpkg
            .WithResult(ProcessResult.Succeeded("app"))            // dpkg-deb -W
            .WithResult(ProcessResult.Failed(1, "not installed"))  // dpkg -s
            .WithResult(ProcessResult.Succeeded())                 // sudo dpkg -i
            .WithResult(ProcessResult.Succeeded());                // sudo apt-get install -f -y

        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/app",
                    Asset = "app-${RELEASE_VERSION}-*.deb",
                    Type = GithubReleaseAssetType.Deb,
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);
        // If we got here, ${RELEASE_VERSION} was resolved to "2.0.0" from tag "v2.0.0"
    }

    #endregion

    #region Type: Deb Backward Compatibility Tests (US2 - T015-T016)

    /// <summary>
    /// T015: Verify omitted type follows binary path.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeOmitted_FollowsBinaryPathAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v1.0.0",
            assets = new[]
            {
                new { name = "app-linux-amd64", browser_download_url = "https://example.com/app" },
            },
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x7f, 0x45, 0x4c, 0x46 }); // ELF header

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Failed(1, "not found")) // which (not found)
            .WithResult(ProcessResult.Succeeded());            // chmod +x

        var tempDir = Path.Combine(Path.GetTempPath(), $"test-bin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);
            var installBlock = new InstallBlock
            {
                Github = new List<GithubReleaseItem>
                {
                    new GithubReleaseItem
                    {
                        Repo = "owner/repo",
                        Asset = "app-linux-amd64",
                        Binary = "app",
                        // Type is NOT set — defaults to Binary
                    },
                },
            };
            var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true, BinDirectory = tempDir };

            // Act
            var results = await installer.InstallAsync(installBlock, context, null);

            // Assert
            results.Should().HaveCount(1);
            results.First().Status.Should().Be(InstallStatus.Success);
            results.First().Message.Should().Contain("from owner/repo");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// T016: Verify explicit type: binary is identical to omitted type.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeBinaryExplicit_IdenticalToOmittedAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v1.0.0",
            assets = new[]
            {
                new { name = "app-linux-amd64", browser_download_url = "https://example.com/app" },
            },
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x7f, 0x45, 0x4c, 0x46 });

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Failed(1, "not found"))
            .WithResult(ProcessResult.Succeeded());

        var tempDir = Path.Combine(Path.GetTempPath(), $"test-bin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);
            var installBlock = new InstallBlock
            {
                Github = new List<GithubReleaseItem>
                {
                    new GithubReleaseItem
                    {
                        Repo = "owner/repo",
                        Asset = "app-linux-amd64",
                        Binary = "app",
                        Type = GithubReleaseAssetType.Binary, // Explicit
                    },
                },
            };
            var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true, BinDirectory = tempDir };

            // Act
            var results = await installer.InstallAsync(installBlock, context, null);

            // Assert
            results.Should().HaveCount(1);
            results.First().Status.Should().Be(InstallStatus.Success);
            results.First().Message.Should().Contain("from owner/repo");
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

    #region Type: Deb Idempotency Tests (US3 - T019-T020)

    /// <summary>
    /// T019: Verify already-installed .deb package is skipped.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeDeb_PackageAlreadyInstalled_SkipsAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v1.0.0",
            assets = new[]
            {
                new { name = "app.deb", browser_download_url = "https://example.com/app.deb" },
            },
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0xDE, 0xB0 });

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Succeeded("/usr/bin/dpkg")) // which dpkg
            .WithResult(ProcessResult.Succeeded("drawio"))         // dpkg-deb -W
            .WithResult(ProcessResult.Succeeded("Status: install ok installed")); // dpkg -s → installed!

        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "jgraph/drawio-desktop",
                    Asset = "app.deb",
                    Type = GithubReleaseAssetType.Deb,
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Skipped);
        results.First().Message.Should().Contain("already installed");

        // Verify dpkg -i was NOT called
        fakeRunner.Calls.Should().NotContain(c => c.FileName == "sudo" && c.Arguments.Contains("dpkg -i"));
    }

    /// <summary>
    /// T020: Verify not-installed package proceeds with installation.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeDeb_PackageNotInstalled_ProceedsAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v1.0.0",
            assets = new[]
            {
                new { name = "app.deb", browser_download_url = "https://example.com/app.deb" },
            },
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0xDE, 0xB0 });

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Succeeded("/usr/bin/dpkg")) // which dpkg
            .WithResult(ProcessResult.Succeeded("app"))            // dpkg-deb -W
            .WithResult(ProcessResult.Failed(1, "not installed"))  // dpkg -s → NOT installed
            .WithResult(ProcessResult.Succeeded())                 // sudo dpkg -i
            .WithResult(ProcessResult.Succeeded());                // sudo apt-get install -f -y

        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/app",
                    Asset = "app.deb",
                    Type = GithubReleaseAssetType.Deb,
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);

        // Verify dpkg -i WAS called
        fakeRunner.Calls.Should().Contain(c => c.FileName == "sudo" && c.Arguments.Contains("dpkg -i"));
    }

    #endregion

    #region Type: Deb Dry Run Tests (US4 - T023-T024)

    /// <summary>
    /// T023: Verify dry-run type: deb reports "would be installed via dpkg".
    /// </summary>
    [Fact]
    public async Task DryRun_TypeDeb_ValidRelease_ReportsWouldInstallAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 200); // HEAD request succeeds

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Succeeded("/usr/bin/dpkg")); // which dpkg

        var installer = new GithubReleaseInstaller(processRunner: fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "jgraph/drawio-desktop",
                    Asset = "drawio-arm64-*.deb",
                    Type = GithubReleaseAssetType.Deb,
                    Version = "v29.3.6",
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Skipped);
        results.First().Message.Should().Contain("would be installed via dpkg");

        // Verify no download or dpkg calls
        fakeRunner.Calls.Should().NotContain(c => c.FileName == "sudo");
        fakeRunner.Calls.Should().NotContain(c => c.FileName == "dpkg-deb");
    }

    /// <summary>
    /// T024: Verify dry-run type: deb with invalid release reports error.
    /// </summary>
    [Fact]
    public async Task DryRun_TypeDeb_InvalidRelease_ReportsErrorAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 404);

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Succeeded("/usr/bin/dpkg")); // which dpkg

        var installer = new GithubReleaseInstaller(processRunner: fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/nonexistent",
                    Asset = "*.deb",
                    Type = GithubReleaseAssetType.Deb,
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("404");
    }

    #endregion

    #region Type: Deb Error Handling Tests (US5 - T027-T031)

    /// <summary>
    /// T027: Verify no sudo returns warning for deb type.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeDeb_NoSudo_ReturnsWarningAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner();
        var installer = new GithubReleaseInstaller(processRunner: fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "jgraph/drawio-desktop",
                    Asset = "drawio-arm64-*.deb",
                    Type = GithubReleaseAssetType.Deb,
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = false };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Warning);
        results.First().Message.Should().Contain("Sudo required to install .deb packages");
    }

    /// <summary>
    /// T028: Verify no dpkg returns failed for deb type.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeDeb_NoDpkg_ReturnsFailedAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Failed(1, "not found")); // which dpkg → not found

        var installer = new GithubReleaseInstaller(processRunner: fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "jgraph/drawio-desktop",
                    Asset = "drawio-arm64-*.deb",
                    Type = GithubReleaseAssetType.Deb,
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("dpkg is not available on this system");
    }

    /// <summary>
    /// T029: Verify asset not ending in .deb returns failed.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeDeb_AssetNotDeb_ReturnsFailedAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v1.0.0",
            assets = new[]
            {
                new { name = "app-linux.tar.gz", browser_download_url = "https://example.com/app.tar.gz" },
            },
        });

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Succeeded("/usr/bin/dpkg")); // which dpkg

        var installer = new GithubReleaseInstaller(processRunner: fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "app-linux.tar.gz",
                    Type = GithubReleaseAssetType.Deb,
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("Asset does not appear to be a .deb package");
    }

    /// <summary>
    /// T030: Verify dpkg -i failure returns failed with stderr.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeDeb_DpkgInstallFails_ReturnsFailedAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v1.0.0",
            assets = new[]
            {
                new { name = "app.deb", browser_download_url = "https://example.com/app.deb" },
            },
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0xDE, 0xB0 });

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Succeeded("/usr/bin/dpkg")) // which dpkg
            .WithResult(ProcessResult.Succeeded("app"))            // dpkg-deb -W
            .WithResult(ProcessResult.Failed(1, "not installed"))  // dpkg -s
            .WithResult(ProcessResult.Failed(1, "dpkg: error processing archive")); // dpkg -i fails

        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "app.deb",
                    Type = GithubReleaseAssetType.Deb,
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("dpkg installation failed");
    }

    /// <summary>
    /// T031: Verify apt-get install -f failure returns failed.
    /// </summary>
    [Fact]
    public async Task InstallSingleItem_TypeDeb_DependencyResolutionFails_ReturnsFailedAsync()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWithJson(new
        {
            tag_name = "v1.0.0",
            assets = new[]
            {
                new { name = "app.deb", browser_download_url = "https://example.com/app.deb" },
            },
        });

        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0xDE, 0xB0 });

        var fakeRunner = new FakeProcessRunner()
            .WithResult(ProcessResult.Succeeded("/usr/bin/dpkg")) // which dpkg
            .WithResult(ProcessResult.Succeeded("app"))            // dpkg-deb -W
            .WithResult(ProcessResult.Failed(1, "not installed"))  // dpkg -s
            .WithResult(ProcessResult.Succeeded())                 // dpkg -i succeeds
            .WithResult(ProcessResult.Failed(1, "unmet dependencies")); // apt-get install -f fails

        var installer = new GithubReleaseInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Github = new List<GithubReleaseItem>
            {
                new GithubReleaseItem
                {
                    Repo = "owner/repo",
                    Asset = "app.deb",
                    Type = GithubReleaseAssetType.Deb,
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = false, HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("Dependency resolution failed");
    }

    #endregion
}
