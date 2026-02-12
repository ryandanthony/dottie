// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Tests.Fakes;
using FluentAssertions;
using Moq;

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

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithEmptyRepoList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installBlock = new InstallBlock();
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
    public async Task InstallAsync_WithDryRun_SkipsInstallationAsync()
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
            },
        };
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
    public async Task InstallAsync_WithoutSudo_ReturnsWarningResultsAsync()
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
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = false,
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Status.Should().Be(InstallStatus.Warning));
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithEmptyRepoList_ReturnsEmptyResults_WhenAptReposIsEmptyListAsync()
    {
        // Arrange
        var installBlock = new InstallBlock { AptRepos = new List<AptRepoItem>() };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

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
        var context = new InstallContext { RepoRoot = "/repo" };

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
    public async Task InstallAsync_WithNullAptReposList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installBlock = new InstallBlock { AptRepos = null };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().BeEmpty();
    }


    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithSudo_AddsGpgKeyAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 }); // Fake GPG key data

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult() // Add GPG key
            .WithSuccessResult() // Remove conflicting .sources file
            .WithSuccessResult() // Add source
            .WithSuccessResult(); // Install package

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "testrepo",
                    Repo = "deb https://example.com/repo stable main",
                    KeyUrl = "https://example.com/key.gpg",
                    Packages = new List<string> { "testpkg" }
                }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        fakeRunner.Calls.Should().Contain(c => c.FileName == "bash" && c.Arguments.Contains("trusted.gpg.d"));
        mockDownloader.Verify(d => d.DownloadAsync("https://example.com/key.gpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithSudo_AddsSourcesListAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult() // Add GPG key
            .WithSuccessResult() // Remove conflicting .sources file
            .WithSuccessResult() // Add source
            .WithSuccessResult(); // Install package

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "testrepo",
                    Repo = "deb https://example.com/repo stable main",
                    KeyUrl = "https://example.com/key.gpg",
                    Packages = new List<string> { "testpkg" }
                }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        fakeRunner.Calls.Should().Contain(c => c.FileName == "bash" && c.Arguments.Contains("sources.list.d"));
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithSudo_InstallsPackagesFromRepoAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult() // Add GPG key
            .WithSuccessResult() // Remove conflicting .sources file
            .WithSuccessResult() // Add source
            .WithSuccessResult() // Install package 1
            .WithSuccessResult(); // Install package 2

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "testrepo",
                    Repo = "deb https://example.com/repo stable main",
                    KeyUrl = "https://example.com/key.gpg",
                    Packages = new List<string> { "pkg1", "pkg2" }
                }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(3); // 1 repo + 2 packages
        fakeRunner.Calls.Should().Contain(c => c.FileName == "sudo" && c.Arguments == "apt-get install -y pkg1");
        fakeRunner.Calls.Should().Contain(c => c.FileName == "sudo" && c.Arguments == "apt-get install -y pkg2");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenKeyDownloadFails_ReturnsFailedResultAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var fakeRunner = new FakeProcessRunner();
        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "testrepo",
                    Repo = "deb https://example.com/repo stable main",
                    KeyUrl = "https://example.com/key.gpg",
                    Packages = new List<string> { "testpkg" }
                }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("Failed to download GPG key");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenAddGpgKeyFails_ReturnsFailedResultAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        var fakeRunner = new FakeProcessRunner()
            .WithFailureResult(1, "Permission denied"); // Add GPG key fails

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "testrepo",
                    Repo = "deb https://example.com/repo stable main",
                    KeyUrl = "https://example.com/key.gpg",
                    Packages = new List<string> { "testpkg" }
                }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("Failed to add GPG key");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenAddSourceFails_ReturnsFailedResultAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult() // Add GPG key succeeds
            .WithSuccessResult() // Remove conflicting .sources file
            .WithFailureResult(1, "Permission denied"); // Add source fails

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "testrepo",
                    Repo = "deb https://example.com/repo stable main",
                    KeyUrl = "https://example.com/key.gpg",
                    Packages = new List<string> { "testpkg" }
                }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("Failed to add repository source");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenPackageInstallFails_ReturnsFailedResultForPackageAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult() // Add GPG key
            .WithSuccessResult() // Remove conflicting .sources file
            .WithSuccessResult() // Add source
            .WithSuccessResult() // apt-get update
            .WithFailureResult(100, "Package not found"); // Install fails

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "testrepo",
                    Repo = "deb https://example.com/repo stable main",
                    KeyUrl = "https://example.com/key.gpg",
                    Packages = new List<string> { "testpkg" }
                }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(2); // 1 repo success + 1 package failure
        results.Should().ContainSingle(r => r.Status == InstallStatus.Success && r.ItemName == "testrepo");
        results.Should().ContainSingle(r => r.Status == InstallStatus.Failed && r.ItemName == "testpkg");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithNoPackages_OnlyAddsRepoAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult() // Add GPG key
            .WithSuccessResult() // Remove conflicting .sources file
            .WithSuccessResult() // Add source
            .WithSuccessResult(); // apt-get update

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "testrepo",
                    Repo = "deb https://example.com/repo stable main",
                    KeyUrl = "https://example.com/key.gpg",
                    Packages = null // No packages
                }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);
        results.First().ItemName.Should().Be("testrepo");
        fakeRunner.CallCount.Should().Be(4); // GPG key, remove conflicting source, source, and apt-get update
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithMultipleRepos_ProcessesAllReposAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult() // Repo1: Add GPG key
            .WithSuccessResult() // Repo1: Remove conflicting .sources file
            .WithSuccessResult() // Repo1: Add source
            .WithSuccessResult() // Repo1: Install pkg
            .WithSuccessResult() // Repo2: Add GPG key
            .WithSuccessResult() // Repo2: Remove conflicting .sources file
            .WithSuccessResult() // Repo2: Add source
            .WithSuccessResult(); // Repo2: Install pkg

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "repo1",
                    Repo = "deb https://example1.com/repo stable main",
                    KeyUrl = "https://example1.com/key.gpg",
                    Packages = new List<string> { "pkg1" }
                },
                new AptRepoItem
                {
                    Name = "repo2",
                    Repo = "deb https://example2.com/repo stable main",
                    KeyUrl = "https://example2.com/key.gpg",
                    Packages = new List<string> { "pkg2" }
                }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(4); // 2 repos + 2 packages
        results.Select(r => r.ItemName).Should().Contain(new[] { "repo1", "repo2", "pkg1", "pkg2" });
    }

    /// <summary>
    /// Verifies that dottie removes a conflicting DEB822 .sources file before writing
    /// the legacy .list file, preventing APT "configured multiple times" warnings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithSudo_RemovesConflictingSourcesFileBeforeWritingListAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult() // Add GPG key
            .WithSuccessResult() // Remove conflicting .sources file
            .WithSuccessResult() // Add .list source
            .WithSuccessResult() // apt-get update
            .WithSuccessResult(); // Install package

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
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
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        fakeRunner.Calls.Should().Contain(c =>
            c.FileName == "bash" &&
            c.Arguments.Contains("rm -f /etc/apt/sources.list.d/vscode.sources"));
    }

    [Fact]
    public void Constructor_WithNullParameters_CreatesDefaults()
    {
        // Act
        var installer = new AptRepoInstaller(null, null);

        // Assert
        installer.Should().NotBeNull();
        installer.SourceType.Should().Be(InstallSourceType.AptRepo);
    }

    /// <summary>
    /// Verifies that ${SIGNING_FILE} in the repo line is resolved to the
    /// actual GPG key path at install time.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithSigningFileVariable_ResolvesToGpgKeyPathAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult() // Add GPG key
            .WithSuccessResult() // Remove conflicting .sources file
            .WithSuccessResult() // Add .list source
            .WithSuccessResult() // apt-get update
            .WithSuccessResult(); // Install package

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "typora",
                    Repo = "deb [signed-by=${SIGNING_FILE}] https://downloads.typora.io/linux ./",
                    KeyUrl = "https://typora.io/linux/public-key.asc",
                    Packages = new List<string> { "typora" },
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();

        // The sources list write command should contain the resolved path, not the variable
        fakeRunner.Calls.Should().Contain(c =>
            c.FileName == "bash" &&
            c.Arguments.Contains("signed-by=/etc/apt/trusted.gpg.d/typora.gpg") &&
            !c.Arguments.Contains("${SIGNING_FILE}"));
    }

    /// <summary>
    /// Verifies that repo lines without ${SIGNING_FILE} are written unchanged.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithoutSigningFileVariable_WritesRepoUnchangedAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03 });

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult() // Add GPG key
            .WithSuccessResult() // Remove conflicting .sources file
            .WithSuccessResult() // Add .list source
            .WithSuccessResult() // apt-get update
            .WithSuccessResult(); // Install package

        var installer = new AptRepoInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            AptRepos = new List<AptRepoItem>
            {
                new AptRepoItem
                {
                    Name = "testrepo",
                    Repo = "deb https://example.com/repo stable main",
                    KeyUrl = "https://example.com/key.gpg",
                    Packages = new List<string> { "testpkg" },
                },
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();

        // The sources list write command should contain the original repo line
        fakeRunner.Calls.Should().Contain(c =>
            c.FileName == "bash" &&
            c.Arguments.Contains("deb https://example.com/repo stable main"));
    }
}
