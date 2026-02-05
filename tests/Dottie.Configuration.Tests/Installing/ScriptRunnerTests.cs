// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="ScriptRunner"/>.
/// </summary>
public class ScriptRunnerTests
{
    private readonly ScriptRunner _runner = new();

    [Fact]
    public void SourceType_ReturnsScript()
    {
        // Act
        var result = _runner.SourceType;

        // Assert
        result.Should().Be(InstallSourceType.Script);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithEmptyScriptList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installBlock = new InstallBlock();
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var results = await _runner.InstallAsync(installBlock, context, null, CancellationToken.None);

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
        var action = async () => await _runner.InstallAsync(installBlock, context, null, CancellationToken.None).ConfigureAwait(false);

        // Assert
        await action.Should().NotThrowAsync();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithDryRun_ValidatesScriptExistenceAsync()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Scripts = new List<string> { "scripts/install.sh", "scripts/nonexistent.sh" },
        };
        var tempDir = Path.Combine(Path.GetTempPath(), "test-repo");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "scripts"));
        File.WriteAllText(Path.Combine(tempDir, "scripts", "install.sh"), "#!/bin/bash\necho done");

        var context = new InstallContext { RepoRoot = tempDir, DryRun = true };

        try
        {
            // Act
            var results = await _runner.InstallAsync(installBlock, context, null, CancellationToken.None);

            // Assert
            results.Should().HaveCount(2);
            results.Should().ContainSingle(r => r.Status == InstallStatus.Success && r.ItemName == "scripts/install.sh");
            results.Should().ContainSingle(r => r.Status == InstallStatus.Failed && r.ItemName == "scripts/nonexistent.sh");
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithPathEscapeAttempt_ReturnsFailedAsync()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Scripts = new List<string> { "../../../etc/passwd" },
        };
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var results = await _runner.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Status.Should().Be(InstallStatus.Failed));
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithEmptyScriptList_ReturnsEmptyResults_WhenScriptsIsEmptyListAsync()
    {
        // Arrange
        var installBlock = new InstallBlock { Scripts = new List<string>() };
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var results = await _runner.InstallAsync(installBlock, context, null, CancellationToken.None);

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
        Func<Task> act = async () => await _runner.InstallAsync(null!, context, null).ConfigureAwait(false);

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
        Func<Task> act = async () => await _runner.InstallAsync(installBlock, null!, null).ConfigureAwait(false);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithNullScriptsList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installBlock = new InstallBlock { Scripts = null };
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var results = await _runner.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().BeEmpty();
    }
}
