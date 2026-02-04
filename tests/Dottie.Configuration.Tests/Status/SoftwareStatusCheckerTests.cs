// -----------------------------------------------------------------------
// <copyright file="SoftwareStatusCheckerTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Status;
using FluentAssertions;
using Moq;

namespace Dottie.Configuration.Tests.Status;

/// <summary>
/// Tests for <see cref="SoftwareStatusChecker"/>.
/// </summary>
public class SoftwareStatusCheckerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly InstallContext _context;
    private readonly Mock<IProcessRunner> _mockProcessRunner;
    private bool _disposed;

    public SoftwareStatusCheckerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"SoftwareStatusCheckerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var binDir = Path.Combine(_tempDir, "bin");
        var fontDir = Path.Combine(_tempDir, "fonts");
        Directory.CreateDirectory(binDir);
        Directory.CreateDirectory(fontDir);

        _context = new InstallContext
        {
            RepoRoot = _tempDir,
            BinDirectory = binDir,
            FontDirectory = fontDir,
            DryRun = true,
        };

        _mockProcessRunner = new Mock<IProcessRunner>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
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

        _disposed = true;
    }

    [Fact]
    public async Task CheckStatusAsync_WhenNullInstallBlock_ReturnsEmptyList()
    {
        // Arrange
        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);

        // Act
        var results = await checker.CheckStatusAsync(null, _context);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckStatusAsync_WhenEmptyInstallBlock_ReturnsEmptyList()
    {
        // Arrange
        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock();

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckStatusAsync_WhenGitHubBinaryExists_ReturnsInstalledState()
    {
        // Arrange
        var binaryName = "test-binary";
        var binaryPath = Path.Combine(_context.BinDirectory, binaryName);
        await File.WriteAllTextAsync(binaryPath, "binary content");

        _mockProcessRunner
            .Setup(x => x.RunAsync(binaryPath, "--version", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "v1.2.3", string.Empty));

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Github = [new GithubReleaseItem { Repo = "owner/repo", Asset = "test-*.tar.gz", Binary = binaryName, Version = "1.2.3" }],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(binaryName);
        results[0].State.Should().Be(SoftwareInstallState.Installed);
        results[0].SourceType.Should().Be(InstallSourceType.GithubRelease);
    }

    [Fact]
    public async Task CheckStatusAsync_WhenGitHubBinaryMissing_ReturnsMissingState()
    {
        // Arrange
        var binaryName = "missing-binary";

        _mockProcessRunner
            .Setup(x => x.RunAsync("where", binaryName, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, string.Empty));

        _mockProcessRunner
            .Setup(x => x.RunAsync("which", binaryName, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, string.Empty));

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Github = [new GithubReleaseItem { Repo = "owner/repo", Asset = "test-*.tar.gz", Binary = binaryName }],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(binaryName);
        results[0].State.Should().Be(SoftwareInstallState.Missing);
    }

    [Fact]
    public async Task CheckStatusAsync_WhenGitHubVersionMismatch_ReturnsOutdatedState()
    {
        // Arrange
        var binaryName = "outdated-binary";
        var binaryPath = Path.Combine(_context.BinDirectory, binaryName);
        await File.WriteAllTextAsync(binaryPath, "binary content");

        _mockProcessRunner
            .Setup(x => x.RunAsync(binaryPath, "--version", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "v1.0.0", string.Empty));

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Github = [new GithubReleaseItem { Repo = "owner/repo", Asset = "test-*.tar.gz", Binary = binaryName, Version = "2.0.0" }],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(binaryName);
        results[0].State.Should().Be(SoftwareInstallState.Outdated);
        results[0].InstalledVersion.Should().Be("1.0.0");
        results[0].TargetVersion.Should().Be("2.0.0");
    }

    [Fact]
    public async Task CheckStatusAsync_WhenAptPackageInstalled_ReturnsInstalledState()
    {
        // Arrange
        var packageName = "vim";

        _mockProcessRunner
            .Setup(x => x.RunAsync("dpkg", $"-s {packageName}", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "Package: vim\nStatus: install ok installed", string.Empty));

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Apt = [packageName],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(packageName);
        results[0].State.Should().Be(SoftwareInstallState.Installed);
        results[0].SourceType.Should().Be(InstallSourceType.AptPackage);
    }

    [Fact]
    public async Task CheckStatusAsync_WhenAptPackageMissing_ReturnsMissingState()
    {
        // Arrange
        var packageName = "nonexistent-package";

        _mockProcessRunner
            .Setup(x => x.RunAsync("dpkg", $"-s {packageName}", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "package not installed"));

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Apt = [packageName],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(packageName);
        results[0].State.Should().Be(SoftwareInstallState.Missing);
    }

    [Fact]
    public async Task CheckStatusAsync_WhenSnapPackageInstalled_ReturnsInstalledState()
    {
        // Arrange
        var packageName = "code";

        _mockProcessRunner
            .Setup(x => x.RunAsync("snap", $"list {packageName}", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "Name  Version    Rev\ncode  1.85.0     102", string.Empty));

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Snaps = [new SnapItem { Name = packageName }],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(packageName);
        results[0].State.Should().Be(SoftwareInstallState.Installed);
        results[0].SourceType.Should().Be(InstallSourceType.SnapPackage);
    }

    [Fact]
    public async Task CheckStatusAsync_WhenSnapPackageMissing_ReturnsMissingState()
    {
        // Arrange
        var packageName = "nonexistent-snap";

        _mockProcessRunner
            .Setup(x => x.RunAsync("snap", $"list {packageName}", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "no matching snaps installed"));

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Snaps = [new SnapItem { Name = packageName }],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(packageName);
        results[0].State.Should().Be(SoftwareInstallState.Missing);
    }

    [Fact]
    public async Task CheckStatusAsync_WhenFontInstalled_ReturnsInstalledState()
    {
        // Arrange
        var fontName = "FiraCode";
        var fontFile = Path.Combine(_context.FontDirectory, "FiraCode-Regular.ttf");
        await File.WriteAllTextAsync(fontFile, "font data");

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Fonts = [new FontItem { Name = fontName, Url = "https://example.com/font.zip" }],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(fontName);
        results[0].State.Should().Be(SoftwareInstallState.Installed);
        results[0].SourceType.Should().Be(InstallSourceType.Font);
    }

    [Fact]
    public async Task CheckStatusAsync_WhenFontMissing_ReturnsMissingState()
    {
        // Arrange
        var fontName = "MissingFont";

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Fonts = [new FontItem { Name = fontName, Url = "https://example.com/font.zip" }],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(fontName);
        results[0].State.Should().Be(SoftwareInstallState.Missing);
    }

    [Fact]
    public async Task CheckStatusAsync_WhenFontDirectoryMissing_ReturnsMissingState()
    {
        // Arrange
        var fontName = "SomeFont";
        Directory.Delete(_context.FontDirectory, recursive: true);

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Fonts = [new FontItem { Name = fontName, Url = "https://example.com/font.zip" }],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(fontName);
        results[0].State.Should().Be(SoftwareInstallState.Missing);
    }

    [Fact]
    public async Task CheckStatusAsync_WhenDetectionFails_ReturnsUnknownState()
    {
        // Arrange
        var packageName = "error-package";

        _mockProcessRunner
            .Setup(x => x.RunAsync("dpkg", $"-s {packageName}", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Command failed"));

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Apt = [packageName],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(packageName);
        results[0].State.Should().Be(SoftwareInstallState.Unknown);
        results[0].Message.Should().Contain("Detection error");
    }

    [Fact]
    public async Task CheckStatusAsync_WithMultipleItems_ReturnsAllStatuses()
    {
        // Arrange
        var binaryPath = Path.Combine(_context.BinDirectory, "fzf");
        await File.WriteAllTextAsync(binaryPath, "binary");

        _mockProcessRunner
            .Setup(x => x.RunAsync(binaryPath, "--version", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "0.45.0", string.Empty));

        _mockProcessRunner
            .Setup(x => x.RunAsync("dpkg", "-s git", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, "installed", string.Empty));

        _mockProcessRunner
            .Setup(x => x.RunAsync("dpkg", "-s curl", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(1, string.Empty, "not installed"));

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Github = [new GithubReleaseItem { Repo = "junegunn/fzf", Asset = "fzf-*.tar.gz", Binary = "fzf" }],
            Apt = ["git", "curl"],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().HaveCount(3);
        results.Should().Contain(r => r.ItemName == "fzf" && r.State == SoftwareInstallState.Installed);
        results.Should().Contain(r => r.ItemName == "git" && r.State == SoftwareInstallState.Installed);
        results.Should().Contain(r => r.ItemName == "curl" && r.State == SoftwareInstallState.Missing);
    }

    [Fact]
    public async Task CheckStatusAsync_WhenGitHubBinaryFoundInPath_ReturnsInstalledState()
    {
        // Arrange
        var binaryName = "path-binary";
        var pathLocation = "/usr/local/bin/path-binary";

        // Binary not in ~/bin/
        var command = OperatingSystem.IsWindows() ? "where" : "which";
        _mockProcessRunner
            .Setup(x => x.RunAsync(command, binaryName, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessResult(0, pathLocation, string.Empty));

        var checker = new SoftwareStatusChecker(_mockProcessRunner.Object);
        var installBlock = new InstallBlock
        {
            Github = [new GithubReleaseItem { Repo = "owner/repo", Asset = "test-*.tar.gz", Binary = binaryName }],
        };

        // Act
        var results = await checker.CheckStatusAsync(installBlock, _context);

        // Assert
        results.Should().ContainSingle();
        results[0].ItemName.Should().Be(binaryName);
        results[0].State.Should().Be(SoftwareInstallState.Installed);
        results[0].InstalledPath.Should().Be(pathLocation);
    }
}
