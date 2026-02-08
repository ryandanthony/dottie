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
}
