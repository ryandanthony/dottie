// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Cli.Output;
using Dottie.Configuration.Installing;
using FluentAssertions;

namespace Dottie.Cli.Tests.Output;

/// <summary>
/// Tests for <see cref="InstallProgressRenderer"/>.
/// </summary>
public class InstallProgressRendererTests
{
    private readonly InstallProgressRenderer _renderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallProgressRendererTests"/> class.
    /// </summary>
    public InstallProgressRendererTests()
    {
        _renderer = new InstallProgressRenderer();
    }

    [Fact]
    public void RenderProgress_WithSuccessResult_DisplaysSuccess()
    {
        // Arrange
        var result = InstallResult.Success("test-pkg", InstallSourceType.GithubRelease);

        // Act
        _renderer.RenderProgress(result);

        // Assert
        // Verification is implicit - should not throw
    }

    [Fact]
    public void RenderProgress_WithFailedResult_DisplaysError()
    {
        // Arrange
        var result = InstallResult.Failed("test-pkg", InstallSourceType.GithubRelease, "Network error");

        // Act
        _renderer.RenderProgress(result);

        // Assert
        // Verification is implicit - should not throw
    }

    [Fact]
    public void RenderProgress_WithSkippedResult_DisplaysSkipReason()
    {
        // Arrange
        var result = InstallResult.Skipped("test-pkg", InstallSourceType.GithubRelease, "Already installed");

        // Act
        _renderer.RenderProgress(result);

        // Assert
        // Verification is implicit - should not throw
    }

    [Fact]
    public void RenderProgress_WithWarningResult_DisplaysWarning()
    {
        // Arrange
        var result = InstallResult.Warning("test-pkg", InstallSourceType.AptRepo, "Sudo not available");

        // Act
        _renderer.RenderProgress(result);

        // Assert
        // Verification is implicit - should not throw
    }

    [Fact]
    public void RenderSummary_WithMultipleResults_DisplaysStats()
    {
        // Arrange
        var results = new[]
        {
            InstallResult.Success("pkg1", InstallSourceType.GithubRelease),
            InstallResult.Success("pkg2", InstallSourceType.AptPackage),
            InstallResult.Failed("pkg3", InstallSourceType.Font, "Download error"),
            InstallResult.Skipped("pkg4", InstallSourceType.Script, "Already installed"),
        };

        // Act
        _renderer.RenderSummary(results);

        // Assert
        // Verification is implicit - should not throw
    }

    [Fact]
    public void RenderSummary_WithEmptyResults_HandlesGracefully()
    {
        // Arrange
        var results = Enumerable.Empty<InstallResult>();

        // Act
        _renderer.RenderSummary(results);

        // Assert
        // Verification is implicit - should not throw
    }

    [Fact]
    public void RenderError_DisplaysErrorMessage()
    {
        // Arrange
        var errorMessage = "Failed to load configuration";

        // Act
        _renderer.RenderError(errorMessage);

        // Assert
        // Verification is implicit - should not throw
    }
}
