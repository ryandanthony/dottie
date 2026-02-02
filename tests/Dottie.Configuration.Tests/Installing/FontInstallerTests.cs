// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Tests.Fakes;
using FluentAssertions;
using Moq;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="FontInstaller"/>.
/// </summary>
public class FontInstallerTests : IDisposable
{
    private readonly FontInstaller _installer = new();
    private readonly string _tempDir;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontInstallerTests"/> class.
    /// </summary>
    public FontInstallerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FontInstallerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Creates a valid zip archive containing a dummy font file.
    /// </summary>
    private static byte[] CreateValidZipWithFontFile(string fontFileName = "TestFont.ttf")
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(fontFileName);
            using var entryStream = entry.Open();

            // Write some dummy font data
            var dummyData = new byte[] { 0x00, 0x01, 0x00, 0x00 }; // Minimal TTF header bytes
            entryStream.Write(dummyData, 0, dummyData.Length);
        }

        memoryStream.Position = 0;
        return memoryStream.ToArray();
    }

    [Fact]
    public void SourceType_ReturnsFont()
    {
        // Act
        var result = _installer.SourceType;

        // Assert
        result.Should().Be(InstallSourceType.Font);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithEmptyFontList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installBlock = new InstallBlock();
        var context = new InstallContext { RepoRoot = "/repo", FontDirectory = _tempDir };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

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
        var context = new InstallContext { RepoRoot = "/repo", FontDirectory = _tempDir };

        // Act
        var action = async () => await _installer.InstallAsync(installBlock, context, CancellationToken.None).ConfigureAwait(false);

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
            Fonts = new List<FontItem>
            {
                new() { Name = "Ubuntu", Url = "https://example.com/font.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
            DryRun = true,
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithoutFonts_SkipsInstallationAsync()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>(),
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

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
        var context = new InstallContext { RepoRoot = "/repo", FontDirectory = _tempDir };

        // Act
        Func<Task> act = async () => await _installer.InstallAsync(null!, context).ConfigureAwait(false);

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
        Func<Task> act = async () => await _installer.InstallAsync(installBlock, null!).ConfigureAwait(false);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithNullFontsList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installBlock = new InstallBlock { Fonts = null };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().BeEmpty();
    }


    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenDownloadSucceeds_ReturnsSuccessResultAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        var validZipBytes = CreateValidZipWithFontFile();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validZipBytes);

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult(); // fc-cache succeeds

        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "TestFont", Url = "https://example.com/testfont.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);
        results.First().ItemName.Should().Be("TestFont");
        results.First().SourceType.Should().Be(InstallSourceType.Font);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenDownloadFails_ReturnsFailedResultAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var fakeRunner = new FakeProcessRunner();
        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "FailFont", Url = "https://example.com/failfont.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().ItemName.Should().Be("FailFont");
        results.First().Message.Should().Contain("Failed to download");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithMultipleFonts_ProcessesAllAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        var validZipBytes = CreateValidZipWithFontFile();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validZipBytes);

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult(); // fc-cache

        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "Font1", Url = "https://example.com/font1.zip" },
                new() { Name = "Font2", Url = "https://example.com/font2.zip" },
                new() { Name = "Font3", Url = "https://example.com/font3.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Status.Should().Be(InstallStatus.Success));
        results.Select(r => r.ItemName).Should().BeEquivalentTo(new[] { "Font1", "Font2", "Font3" });
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithMultipleFonts_ReportsPartialSuccessAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        var validZipBytes = CreateValidZipWithFontFile();

        mockDownloader
            .SetupSequence(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validZipBytes) // Font1 succeeds
            .ThrowsAsync(new HttpRequestException("Network error")) // Font2 fails
            .ReturnsAsync(validZipBytes); // Font3 succeeds

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult(); // fc-cache

        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "Font1", Url = "https://example.com/font1.zip" },
                new() { Name = "Font2", Url = "https://example.com/font2.zip" },
                new() { Name = "Font3", Url = "https://example.com/font3.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().HaveCount(3);
        results.Where(r => r.Status == InstallStatus.Success).Should().HaveCount(2);
        results.Where(r => r.Status == InstallStatus.Failed).Should().HaveCount(1);
        results.Single(r => r.Status == InstallStatus.Failed).ItemName.Should().Be("Font2");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_OnSuccess_RefreshesFontCacheAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        var validZipBytes = CreateValidZipWithFontFile();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validZipBytes);

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult(); // fc-cache

        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "TestFont", Url = "https://example.com/testfont.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        fakeRunner.CallCount.Should().Be(1);
        fakeRunner.Calls[0].FileName.Should().Be("fc-cache");
        fakeRunner.Calls[0].Arguments.Should().Be("-fv");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenAllFontsFail_DoesNotRefreshFontCacheAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var fakeRunner = new FakeProcessRunner();
        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "FailFont", Url = "https://example.com/failfont.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert - fc-cache should NOT be called
        fakeRunner.CallCount.Should().Be(0);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenFontCacheFails_DoesNotThrowAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        var validZipBytes = CreateValidZipWithFontFile();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validZipBytes);

        var fakeRunner = new FakeProcessRunner()
            .WithFailureResult(1, "fc-cache not found"); // fc-cache fails

        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "TestFont", Url = "https://example.com/testfont.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        var action = async () => await installer.InstallAsync(installBlock, context, CancellationToken.None).ConfigureAwait(false);

        // Assert - should not throw even if fc-cache fails
        await action.Should().NotThrowAsync();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_CreatesFontSubdirectoryAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        var validZipBytes = CreateValidZipWithFontFile();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validZipBytes);

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult();

        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "MyCustomFont", Url = "https://example.com/font.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        var fontSubDir = Path.Combine(_tempDir, "MyCustomFont");
        Directory.Exists(fontSubDir).Should().BeTrue();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DownloadsFromCorrectUrlAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        var validZipBytes = CreateValidZipWithFontFile();
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validZipBytes);

        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult();

        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var expectedUrl = "https://github.com/ryanoasis/nerd-fonts/releases/download/v3.0.0/JetBrainsMono.zip";
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "JetBrainsMono", Url = expectedUrl }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        mockDownloader.Verify(
            d => d.DownloadAsync(expectedUrl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullDownloader_CreatesDefaultDownloader()
    {
        // Act
        var installer = new FontInstaller(null, null);

        // Assert
        installer.Should().NotBeNull();
        installer.SourceType.Should().Be(InstallSourceType.Font);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenExtractFails_ReturnsFailedResultAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();

        // Return invalid zip data that will fail extraction
        var invalidZipBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        mockDownloader
            .Setup(d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidZipBytes);

        var fakeRunner = new FakeProcessRunner();
        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "BadArchive", Url = "https://example.com/bad.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().ItemName.Should().Be("BadArchive");
        results.First().Message.Should().Contain("extract");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_DoesNotDownloadFontsAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        var fakeRunner = new FakeProcessRunner();

        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "TestFont", Url = "https://example.com/testfont.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
            DryRun = true,
        };

        // Act
        await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert - download should NOT be called in dry-run mode
        mockDownloader.Verify(
            d => d.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_DryRun_DoesNotRefreshFontCacheAsync()
    {
        // Arrange
        var mockDownloader = new Mock<HttpDownloader>();
        var fakeRunner = new FakeProcessRunner();

        var installer = new FontInstaller(mockDownloader.Object, fakeRunner);
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>
            {
                new() { Name = "TestFont", Url = "https://example.com/testfont.zip" }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = _tempDir,
            DryRun = true,
        };

        // Act
        await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert - fc-cache should NOT be called in dry-run mode
        fakeRunner.CallCount.Should().Be(0);
    }
}
