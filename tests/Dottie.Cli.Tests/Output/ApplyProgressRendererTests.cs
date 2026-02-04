// -----------------------------------------------------------------------
// <copyright file="ApplyProgressRendererTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Models;
using Dottie.Cli.Output;
using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Linking;
using Dottie.Configuration.Models;
using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;
using Xunit;

namespace Dottie.Cli.Tests.Output;

/// <summary>
/// Unit tests for <see cref="ApplyProgressRenderer"/>.
/// </summary>
public sealed class ApplyProgressRendererTests
{
    private readonly ApplyProgressRenderer _renderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyProgressRendererTests"/> class.
    /// </summary>
    public ApplyProgressRendererTests()
    {
        _renderer = new ApplyProgressRenderer();
    }

    [Fact]
    public void RenderError_WithMessage_DoesNotThrow()
    {
        // Act
        var act = () => _renderer.RenderError("Test error message");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderError_WithEmptyMessage_DoesNotThrow()
    {
        // Act
        var act = () => _renderer.RenderError(string.Empty);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderDryRunPreview_WithNullProfile_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _renderer.RenderDryRunPreview(null!, "/repo");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RenderDryRunPreview_WithEmptyProfile_DoesNotThrow()
    {
        // Arrange
        var profile = new ResolvedProfile
        {
            Name = "test",
            Dotfiles = [],
            Install = null,
        };

        // Act
        var act = () => _renderer.RenderDryRunPreview(profile, "/repo");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderDryRunPreview_WithDotfilesOnly_DoesNotThrow()
    {
        // Arrange
        var profile = new ResolvedProfile
        {
            Name = "test",
            Dotfiles =
            [
                new DotfileEntry { Source = "bashrc", Target = "~/.bashrc" },
                new DotfileEntry { Source = "vimrc", Target = "~/.vimrc" },
            ],
            Install = null,
        };

        // Act
        var act = () => _renderer.RenderDryRunPreview(profile, "/tmp/repo");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderDryRunPreview_WithInstallBlockOnly_DoesNotThrow()
    {
        // Arrange
        var profile = new ResolvedProfile
        {
            Name = "test",
            Dotfiles = [],
            Install = new InstallBlock
            {
                Apt = ["curl", "git"],
                Github =
                [
                    new GithubReleaseItem { Repo = "owner/repo", Asset = "tool.tar.gz", Binary = "tool" },
                ],
                Scripts = ["scripts/setup.sh"],
                Fonts =
                [
                    new FontItem { Name = "JetBrains Mono", Url = "https://example.com/font.zip" },
                ],
                Snaps =
                [
                    new SnapItem { Name = "vscode", Classic = true },
                ],
                AptRepos =
                [
                    new AptRepoItem
                    {
                        Name = "docker",
                        KeyUrl = "https://example.com/key",
                        Repo = "deb [arch=amd64] https://example.com stable main",
                        Packages = ["docker-ce"],
                    },
                ],
            },
        };

        // Act
        var act = () => _renderer.RenderDryRunPreview(profile, "/tmp/repo");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderDryRunPreview_WithFullProfile_DoesNotThrow()
    {
        // Arrange
        var profile = new ResolvedProfile
        {
            Name = "full",
            Dotfiles =
            [
                new DotfileEntry { Source = "bashrc", Target = "~/.bashrc" },
            ],
            Install = new InstallBlock
            {
                Apt = ["git"],
            },
        };

        // Act
        var act = () => _renderer.RenderDryRunPreview(profile, "/tmp/repo");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithNullResult_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _renderer.RenderVerboseSummary(null!, "default");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RenderVerboseSummary_WithNotExecutedPhases_DoesNotThrow()
    {
        // Arrange
        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.NotExecuted(),
            InstallPhase = InstallPhaseResult.NotExecuted(),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithSuccessfulLinkPhase_DoesNotThrow()
    {
        // Arrange
        var entry = new DotfileEntry { Source = "bashrc", Target = "~/.bashrc" };
        var successLink = LinkResult.Success(entry, "/home/user/.bashrc");
        var linkOpResult = new LinkOperationResult
        {
            SuccessfulLinks = [successLink],
            SkippedLinks = [],
            FailedLinks = [],
        };
        var linkExecution = LinkExecutionResult.Completed(linkOpResult, []);

        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.Executed(linkExecution),
            InstallPhase = InstallPhaseResult.NotExecuted(),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithSkippedLinks_DoesNotThrow()
    {
        // Arrange
        var entry = new DotfileEntry { Source = "bashrc", Target = "~/.bashrc" };
        var skippedLink = LinkResult.Skipped(entry, "/home/user/.bashrc");
        var linkOpResult = new LinkOperationResult
        {
            SuccessfulLinks = [],
            SkippedLinks = [skippedLink],
            FailedLinks = [],
        };
        var linkExecution = LinkExecutionResult.Completed(linkOpResult, []);

        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.Executed(linkExecution),
            InstallPhase = InstallPhaseResult.NotExecuted(),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithFailedLinks_DoesNotThrow()
    {
        // Arrange
        var entry = new DotfileEntry { Source = "bashrc", Target = "~/.bashrc" };
        var failedLink = LinkResult.Failure(entry, "/home/user/.bashrc", "Permission denied");
        var linkOpResult = new LinkOperationResult
        {
            SuccessfulLinks = [],
            SkippedLinks = [],
            FailedLinks = [failedLink],
        };
        var linkExecution = LinkExecutionResult.Completed(linkOpResult, []);

        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.Executed(linkExecution),
            InstallPhase = InstallPhaseResult.NotExecuted(),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithBlockedLinkPhase_DoesNotThrow()
    {
        // Arrange
        var entry = new DotfileEntry { Source = "bashrc", Target = "~/.bashrc" };
        var conflict = new Conflict
        {
            Entry = entry,
            TargetPath = "/home/user/.bashrc",
            Type = ConflictType.File,
        };
        var conflictResult = new ConflictResult
        {
            Conflicts = [conflict],
            SafeEntries = [],
            AlreadyLinked = [],
        };
        var linkExecution = LinkExecutionResult.Blocked(conflictResult);

        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.Blocked(linkExecution),
            InstallPhase = InstallPhaseResult.NotExecuted(),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithBackupResults_DoesNotThrow()
    {
        // Arrange
        var entry = new DotfileEntry { Source = "bashrc", Target = "~/.bashrc" };
        var successLink = LinkResult.Success(entry, "/home/user/.bashrc");
        var linkOpResult = new LinkOperationResult
        {
            SuccessfulLinks = [successLink],
            SkippedLinks = [],
            FailedLinks = [],
        };
        var backup = new BackupResult
        {
            OriginalPath = "/home/user/.bashrc",
            BackupPath = "/home/user/.bashrc.backup.20260203",
            IsSuccess = true,
        };
        var linkExecution = LinkExecutionResult.Completed(linkOpResult, [backup]);

        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.Executed(linkExecution),
            InstallPhase = InstallPhaseResult.NotExecuted(),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithSuccessfulInstallPhase_DoesNotThrow()
    {
        // Arrange
        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.NotExecuted(),
            InstallPhase = InstallPhaseResult.Executed([
                InstallResult.Success("curl", InstallSourceType.AptPackage),
                InstallResult.Success("owner/repo", InstallSourceType.GithubRelease),
            ]),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithMixedInstallResults_DoesNotThrow()
    {
        // Arrange
        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.NotExecuted(),
            InstallPhase = InstallPhaseResult.Executed([
                InstallResult.Success("curl", InstallSourceType.AptPackage),
                InstallResult.Skipped("git", InstallSourceType.AptPackage, "Already installed"),
                InstallResult.Warning("docker-repo", InstallSourceType.AptRepo, "Sudo not available"),
                InstallResult.Failed("broken-pkg", InstallSourceType.SnapPackage, "Network error"),
            ]),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithAllSourceTypes_DoesNotThrow()
    {
        // Arrange
        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.NotExecuted(),
            InstallPhase = InstallPhaseResult.Executed([
                InstallResult.Success("tool", InstallSourceType.GithubRelease),
                InstallResult.Success("curl", InstallSourceType.AptPackage),
                InstallResult.Success("docker-repo", InstallSourceType.AptRepo),
                InstallResult.Success("setup.sh", InstallSourceType.Script),
                InstallResult.Success("JetBrains Mono", InstallSourceType.Font),
                InstallResult.Success("vscode", InstallSourceType.SnapPackage),
            ]),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithFullResult_DoesNotThrow()
    {
        // Arrange
        var entry = new DotfileEntry { Source = "bashrc", Target = "~/.bashrc" };
        var successLink = LinkResult.Success(entry, "/home/user/.bashrc");
        var linkOpResult = new LinkOperationResult
        {
            SuccessfulLinks = [successLink],
            SkippedLinks = [],
            FailedLinks = [],
        };
        var linkExecution = LinkExecutionResult.Completed(linkOpResult, []);

        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.Executed(linkExecution),
            InstallPhase = InstallPhaseResult.Executed([
                InstallResult.Success("curl", InstallSourceType.AptPackage),
            ]),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "full-profile");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithOverallFailure_DoesNotThrow()
    {
        // Arrange
        var entry = new DotfileEntry { Source = "bashrc", Target = "~/.bashrc" };
        var failedLink = LinkResult.Failure(entry, "/home/user/.bashrc", "Error");
        var linkOpResult = new LinkOperationResult
        {
            SuccessfulLinks = [],
            SkippedLinks = [],
            FailedLinks = [failedLink],
        };
        var linkExecution = LinkExecutionResult.Completed(linkOpResult, []);

        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.Executed(linkExecution),
            InstallPhase = InstallPhaseResult.Executed([
                InstallResult.Failed("pkg", InstallSourceType.AptPackage, "Network error"),
            ]),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithEmptyInstallResults_DoesNotThrow()
    {
        // Arrange
        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.NotExecuted(),
            InstallPhase = InstallPhaseResult.Executed([]),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RenderVerboseSummary_WithInstallResultsHavingMessages_DoesNotThrow()
    {
        // Arrange
        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.NotExecuted(),
            InstallPhase = InstallPhaseResult.Executed([
                InstallResult.Success("curl", InstallSourceType.AptPackage, message: "v7.88.0"),
                InstallResult.Skipped("git", InstallSourceType.AptPackage, "Already at version 2.40.0"),
            ]),
        };

        // Act
        var act = () => _renderer.RenderVerboseSummary(result, "default");

        // Assert
        act.Should().NotThrow();
    }
}
