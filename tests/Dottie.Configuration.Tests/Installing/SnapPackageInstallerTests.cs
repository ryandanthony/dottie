// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Tests.Fakes;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="SnapPackageInstaller"/>.
/// </summary>
public class SnapPackageInstallerTests
{
    [Fact]
    public void SourceType_ReturnsSnapPackage()
    {
        // Arrange
        var installer = new SnapPackageInstaller();

        // Act
        var result = installer.SourceType;

        // Assert
        result.Should().Be(InstallSourceType.SnapPackage);
    }



    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithNullInstallBlock_ThrowsArgumentNullExceptionAsync()
    {
        // Arrange
        var installer = new SnapPackageInstaller();
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        Func<Task> act = async () => await installer.InstallAsync(null!, context, null).ConfigureAwait(false);

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
        var installer = new SnapPackageInstaller();
        var installBlock = new InstallBlock();

        // Act
        Func<Task> act = async () => await installer.InstallAsync(installBlock, null!, null).ConfigureAwait(false);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }



    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithEmptySnapList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installer = new SnapPackageInstaller();
        var installBlock = new InstallBlock();
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithNullSnapsList_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installer = new SnapPackageInstaller();
        var installBlock = new InstallBlock { Snaps = null };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null);

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithNoSnaps_ReturnsEmptyResultsAsync()
    {
        // Arrange
        var installer = new SnapPackageInstaller();
        var installBlock = new InstallBlock { Snaps = new List<SnapItem>() };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }



    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithDryRun_SkipsInstallationAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "blender", Classic = false }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true, DryRun = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
        fakeRunner.CallCount.Should().Be(0);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithDryRun_DoesNotExecuteAnyProcessesAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "vscode", Classic = true },
                new() { Name = "firefox", Classic = false }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true, DryRun = true };

        // Act
        await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        fakeRunner.Calls.Should().BeEmpty();
    }



    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithoutSudo_ReturnsWarningResultsAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "blender", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = false,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results.First().Status.Should().Be(InstallStatus.Warning);
        results.First().Message.Should().Contain("Sudo required");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithoutSudo_ReturnsWarningForEachSnapAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "snap1", Classic = false },
                new() { Name = "snap2", Classic = true },
                new() { Name = "snap3", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = false,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r =>
        {
            r.Status.Should().Be(InstallStatus.Warning);
            r.SourceType.Should().Be(InstallSourceType.SnapPackage);
        });
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithoutSudo_DoesNotExecuteAnyProcessesAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "blender", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = false,
        };

        // Act
        await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        fakeRunner.CallCount.Should().Be(0);
    }



    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithSingleSnap_ReturnsSuccessResultAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "vlc", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);
        results.First().ItemName.Should().Be("vlc");
        results.First().SourceType.Should().Be(InstallSourceType.SnapPackage);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithMultipleSnaps_ReturnsSuccessResultsForEachAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()
            .WithSuccessResult()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "vlc", Classic = false },
                new() { Name = "spotify", Classic = false },
                new() { Name = "code", Classic = true }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Status.Should().Be(InstallStatus.Success));
        results.Select(r => r.ItemName).Should().ContainInOrder("vlc", "spotify", "code");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_CallsSudoSnapInstall_WithCorrectArgumentsAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "vlc", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        fakeRunner.CallCount.Should().Be(1);
        fakeRunner.Calls[0].FileName.Should().Be("sudo");
        fakeRunner.Calls[0].Arguments.Should().Be("snap install vlc");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithClassicConfinement_AddsClassicFlagAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "code", Classic = true }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        fakeRunner.CallCount.Should().Be(1);
        fakeRunner.Calls[0].FileName.Should().Be("sudo");
        fakeRunner.Calls[0].Arguments.Should().Be("snap install code --classic");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithoutClassicConfinement_DoesNotAddClassicFlagAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "vlc", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        fakeRunner.Calls[0].Arguments.Should().NotContain("--classic");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithMixedClassicSettings_UsesCorrectFlagsForEachAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()
            .WithSuccessResult()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "vlc", Classic = false },
                new() { Name = "code", Classic = true },
                new() { Name = "firefox", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        fakeRunner.Calls[0].Arguments.Should().Be("snap install vlc");
        fakeRunner.Calls[1].Arguments.Should().Be("snap install code --classic");
        fakeRunner.Calls[2].Arguments.Should().Be("snap install firefox");
    }



    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenProcessFails_ReturnsFailedResultAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithFailureResult(exitCode: 1, error: "error: snap \"unknown\" not found");
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "unknown", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().ItemName.Should().Be("unknown");
        results.First().Message.Should().Contain("exit code 1");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithPartialFailures_ReportsCorrectStatusForEachAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()
            .WithFailureResult(exitCode: 1)
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "vlc", Classic = false },
                new() { Name = "unknown", Classic = false },
                new() { Name = "firefox", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(3);
        results.ElementAt(0).Status.Should().Be(InstallStatus.Success);
        results.ElementAt(0).ItemName.Should().Be("vlc");
        results.ElementAt(1).Status.Should().Be(InstallStatus.Failed);
        results.ElementAt(1).ItemName.Should().Be("unknown");
        results.ElementAt(2).Status.Should().Be(InstallStatus.Success);
        results.ElementAt(2).ItemName.Should().Be("firefox");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenAllSnapsFail_ReturnsAllFailedResultsAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithFailureResult(exitCode: 1)
            .WithFailureResult(exitCode: 1);
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "bad1", Classic = false },
                new() { Name = "bad2", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Status.Should().Be(InstallStatus.Failed));
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithNonZeroExitCode_IncludesExitCodeInMessageAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithFailureResult(exitCode: 127);
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "badsnap", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.First().Message.Should().Contain("127");
    }



    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenProcessRunnerThrows_ReturnsFailedResultAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithException(new InvalidOperationException("Process crashed"));
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "crashsnap", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().Message.Should().Contain("Exception during installation");
        results.First().Message.Should().Contain("Process crashed");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WhenExceptionOccurs_ContinuesWithRemainingSnapsAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithException(new InvalidOperationException("Crash"))
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "crashsnap", Classic = false },
                new() { Name = "goodsnap", Classic = false }
            },
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true,
        };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.ElementAt(0).Status.Should().Be(InstallStatus.Failed);
        results.ElementAt(1).Status.Should().Be(InstallStatus.Success);
    }

    [Fact]
    public void Constructor_WithNullProcessRunner_UsesDefaultRunner()
    {
        // Act
        var installer = new SnapPackageInstaller(null);

        // Assert - should not throw and be functional
        installer.Should().NotBeNull();
        installer.SourceType.Should().Be(InstallSourceType.SnapPackage);
    }

    [Fact]
    public void Constructor_WithCustomProcessRunner_UsesProvidedRunner()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner();

        // Act
        var installer = new SnapPackageInstaller(fakeRunner);

        // Assert - installer should use the provided runner
        installer.Should().NotBeNull();
    }



    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_ProcessesSnapsInOrderAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()
            .WithSuccessResult()
            .WithSuccessResult()
            .WithSuccessResult()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "first", Classic = false },
                new() { Name = "second", Classic = true },
                new() { Name = "third", Classic = false },
                new() { Name = "fourth", Classic = true },
                new() { Name = "fifth", Classic = false }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert - verify calls were made in exact order
        fakeRunner.CallCount.Should().Be(5);
        fakeRunner.Calls[0].Arguments.Should().Contain("first");
        fakeRunner.Calls[1].Arguments.Should().Contain("second");
        fakeRunner.Calls[2].Arguments.Should().Contain("third");
        fakeRunner.Calls[3].Arguments.Should().Contain("fourth");
        fakeRunner.Calls[4].Arguments.Should().Contain("fifth");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_ResultsMatchSnapOrderAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()
            .WithFailureResult(1)
            .WithSuccessResult()
            .WithFailureResult(2);
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "alpha", Classic = false },
                new() { Name = "beta", Classic = false },
                new() { Name = "gamma", Classic = false },
                new() { Name = "delta", Classic = false }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = (await installer.InstallAsync(installBlock, context, null, CancellationToken.None)).ToList();

        // Assert - results match exact order and status
        results.Should().HaveCount(4);
        results[0].ItemName.Should().Be("alpha");
        results[0].Status.Should().Be(InstallStatus.Success);
        results[1].ItemName.Should().Be("beta");
        results[1].Status.Should().Be(InstallStatus.Failed);
        results[2].ItemName.Should().Be("gamma");
        results[2].Status.Should().Be(InstallStatus.Success);
        results[3].ItemName.Should().Be("delta");
        results[3].Status.Should().Be(InstallStatus.Failed);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_EachSnapGetsIndividualProcessCallAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()
            .WithSuccessResult()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "snap-a", Classic = false },
                new() { Name = "snap-b", Classic = true },
                new() { Name = "snap-c", Classic = false }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert - each snap gets its own separate call
        fakeRunner.CallCount.Should().Be(3);
        fakeRunner.Calls.Should().OnlyContain(c => c.FileName == "sudo");
        fakeRunner.Calls[0].Arguments.Should().Be("snap install snap-a");
        fakeRunner.Calls[1].Arguments.Should().Be("snap install snap-b --classic");
        fakeRunner.Calls[2].Arguments.Should().Be("snap install snap-c");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_ContinuesProcessingAfterFailureAsync()
    {
        // Arrange - first snap fails, rest should still be processed
        var fakeRunner = new FakeProcessRunner()
            .WithFailureResult(exitCode: 1)
            .WithSuccessResult()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "failing-snap", Classic = false },
                new() { Name = "working-snap-1", Classic = false },
                new() { Name = "working-snap-2", Classic = false }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = (await installer.InstallAsync(installBlock, context, null, CancellationToken.None)).ToList();

        // Assert - all snaps were attempted
        fakeRunner.CallCount.Should().Be(3);
        results.Should().HaveCount(3);
        results[0].Status.Should().Be(InstallStatus.Failed);
        results[1].Status.Should().Be(InstallStatus.Success);
        results[2].Status.Should().Be(InstallStatus.Success);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_ContinuesProcessingAfterExceptionAsync()
    {
        // Arrange - first snap throws, rest should still be processed
        var fakeRunner = new FakeProcessRunner()
            .WithException(new TimeoutException("Process timed out"))
            .WithSuccessResult()
            .WithException(new InvalidOperationException("Another error"))
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "timeout-snap", Classic = false },
                new() { Name = "good-snap-1", Classic = false },
                new() { Name = "error-snap", Classic = false },
                new() { Name = "good-snap-2", Classic = false }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = (await installer.InstallAsync(installBlock, context, null, CancellationToken.None)).ToList();

        // Assert - all snaps were attempted despite exceptions
        fakeRunner.CallCount.Should().Be(4);
        results.Should().HaveCount(4);
        results[0].Status.Should().Be(InstallStatus.Failed);
        results[0].Message.Should().Contain("Process timed out");
        results[1].Status.Should().Be(InstallStatus.Success);
        results[2].Status.Should().Be(InstallStatus.Failed);
        results[2].Message.Should().Contain("Another error");
        results[3].Status.Should().Be(InstallStatus.Success);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithSingleSnap_MakesExactlyOneCallAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "single-snap", Classic = false }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        fakeRunner.CallCount.Should().Be(1);
        results.Should().HaveCount(1);
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithManySnaps_ProcessesAllOfThemAsync()
    {
        // Arrange - test with 10 snaps
        var fakeRunner = new FakeProcessRunner();
        for (int i = 0; i < 10; i++)
        {
            fakeRunner.WithSuccessResult();
        }

        var installer = new SnapPackageInstaller(fakeRunner);
        var snaps = Enumerable.Range(1, 10)
            .Select(i => new SnapItem { Name = $"snap-{i}", Classic = i % 2 == 0 })
            .ToList();
        var installBlock = new InstallBlock { Snaps = snaps };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = (await installer.InstallAsync(installBlock, context, null, CancellationToken.None)).ToList();

        // Assert
        fakeRunner.CallCount.Should().Be(10);
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(r => r.Status.Should().Be(InstallStatus.Success));
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_PreservesSnapNameInResultAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()
            .WithFailureResult(1);
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "my-special-snap", Classic = false },
                new() { Name = "another-snap-name", Classic = true }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = (await installer.InstallAsync(installBlock, context, null, CancellationToken.None)).ToList();

        // Assert - snap names are preserved exactly
        results[0].ItemName.Should().Be("my-special-snap");
        results[1].ItemName.Should().Be("another-snap-name");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithSnapNameContainingHyphen_FormatsArgumentCorrectlyAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "visual-studio-code", Classic = true }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert
        fakeRunner.Calls[0].Arguments.Should().Be("snap install visual-studio-code --classic");
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_AllResultsHaveCorrectSourceTypeAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()
            .WithFailureResult(1)
            .WithException(new Exception("error"));
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "success-snap", Classic = false },
                new() { Name = "failed-snap", Classic = false },
                new() { Name = "exception-snap", Classic = false }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, null, CancellationToken.None);

        // Assert - all results have correct source type regardless of status
        results.Should().AllSatisfy(r => r.SourceType.Should().Be(InstallSourceType.SnapPackage));
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    [Fact]
    public async Task InstallAsync_WithoutSudo_IteratesAllSnapsForWarningsAsync()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner();
        var installer = new SnapPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Snaps = new List<SnapItem>
            {
                new() { Name = "snap1", Classic = false },
                new() { Name = "snap2", Classic = true },
                new() { Name = "snap3", Classic = false },
                new() { Name = "snap4", Classic = true }
            },
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = false };

        // Act
        var results = (await installer.InstallAsync(installBlock, context, null, CancellationToken.None)).ToList();

        // Assert - each snap gets a warning result
        results.Should().HaveCount(4);
        results[0].ItemName.Should().Be("snap1");
        results[1].ItemName.Should().Be("snap2");
        results[2].ItemName.Should().Be("snap3");
        results[3].ItemName.Should().Be("snap4");
        results.Should().AllSatisfy(r =>
        {
            r.Status.Should().Be(InstallStatus.Warning);
            r.Message.Should().Contain("Sudo required");
        });

        // No process calls should have been made
        fakeRunner.CallCount.Should().Be(0);
    }
}
