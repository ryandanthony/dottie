// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="FontInstaller"/>.
/// </summary>
public class FontInstallerTests
{
    private readonly FontInstaller _installer = new();

    [Fact]
    public void SourceType_ReturnsFont()
    {
        // Act
        var result = _installer.SourceType;

        // Assert
        result.Should().Be(InstallSourceType.Font);
    }

    [Fact]
    public async Task InstallAsync_WithEmptyFontList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock();
        var context = new InstallContext { RepoRoot = "/repo", FontDirectory = "/root/.local/share/fonts" };

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
        var context = new InstallContext { RepoRoot = "/repo", FontDirectory = "/root/.local/share/fonts" };

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
            Fonts = new List<FontItem>
            {
                new() { Name = "Ubuntu", Url = "https://example.com/font.zip" }
            }
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = "/root/.local/share/fonts",
            DryRun = true
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithoutFonts_SkipsInstallation()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Fonts = new List<FontItem>()
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = "/root/.local/share/fonts"
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithNoFonts_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock { Fonts = new List<FontItem>() };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = "/root/.local/share/fonts"
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithNullInstallBlock_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo", FontDirectory = "/fonts" };

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

    [Fact]
    public async Task InstallAsync_WithNullFontsList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock { Fonts = null };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            FontDirectory = "/root/.local/share/fonts"
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().BeEmpty();
    }
}

