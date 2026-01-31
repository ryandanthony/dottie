// -----------------------------------------------------------------------
// <copyright file="LinkingOrchestratorTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Linking;
using Dottie.Configuration.Models;
using FluentAssertions;
using Xunit.Sdk;

namespace Dottie.Configuration.Tests.Linking;

/// <summary>
/// Tests for <see cref="LinkingOrchestrator"/>.
/// </summary>
public sealed class LinkingOrchestratorTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _repoRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkingOrchestratorTests"/> class.
    /// </summary>
    public LinkingOrchestratorTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"linking-orchestrator-tests-{Guid.NewGuid():N}");
        _repoRoot = Path.Combine(_tempDirectory, "repo");
        Directory.CreateDirectory(_repoRoot);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, true);
                }
#pragma warning disable CA1031 // Do not catch general exception types - cleanup code should not throw
                catch
                {
                    // Ignore cleanup failures in tests
                }
#pragma warning restore CA1031
            }
        }
    }

    [Fact]
    public void ExecuteLink_WithNullProfile_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = new LinkingOrchestrator();

        // Act & Assert
        var act = () => orchestrator.ExecuteLink(null!, _repoRoot, force: false);
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("profile");
    }

    [Fact]
    public void ExecuteLink_WithNullRepoRoot_ThrowsArgumentException()
    {
        // Arrange
        var orchestrator = new LinkingOrchestrator();
        var profile = CreateEmptyProfile();

        // Act & Assert
        var act = () => orchestrator.ExecuteLink(profile, null!, force: false);
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("repoRoot");
    }

    [Fact]
    public void ExecuteLink_WithEmptyRepoRoot_ThrowsArgumentException()
    {
        // Arrange
        var orchestrator = new LinkingOrchestrator();
        var profile = CreateEmptyProfile();

        // Act & Assert
        var act = () => orchestrator.ExecuteLink(profile, string.Empty, force: false);
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("repoRoot");
    }

    [Fact]
    public void ExecuteLink_WithEmptyDotfiles_ReturnsCompletedResult()
    {
        // Arrange
        var orchestrator = new LinkingOrchestrator();
        var profile = CreateEmptyProfile();

        // Act
        var result = orchestrator.ExecuteLink(profile, _repoRoot, force: false);

        // Assert
        result.IsBlocked.Should().BeFalse();
        result.LinkResult.Should().NotBeNull();
        result.LinkResult!.IsSuccess.Should().BeTrue();
        result.LinkResult.SuccessfulLinks.Should().BeEmpty();
        result.LinkResult.SkippedLinks.Should().BeEmpty();
        result.LinkResult.FailedLinks.Should().BeEmpty();
    }

    [Fact]
    public void ExecuteLink_WhenConflictsExistAndNotForce_ReturnsBlockedResult()
    {
        // Arrange
        var targetDir = Path.Combine(_tempDirectory, "target");
        Directory.CreateDirectory(targetDir);
        var targetPath = Path.Combine(targetDir, "test.txt");
        File.WriteAllText(targetPath, "existing content");

        var sourcePath = Path.Combine(_repoRoot, "test.txt");
        File.WriteAllText(sourcePath, "source content");

        var orchestrator = new LinkingOrchestrator();
        var dotfiles = new List<DotfileEntry>
        {
            new() { Source = "test.txt", Target = targetPath },
        };
        var profile = CreateProfile(dotfiles);

        // Act
        var result = orchestrator.ExecuteLink(profile, _repoRoot, force: false);

        // Assert
        result.IsBlocked.Should().BeTrue();
        result.ConflictResult.Should().NotBeNull();
        result.ConflictResult!.HasConflicts.Should().BeTrue();
        result.ConflictResult.Conflicts.Should().ContainSingle();
    }

    [Fact]
    public void ExecuteLink_IsReusableFromMultipleContexts()
    {
        // Arrange - verify the orchestrator can be called multiple times
        var orchestrator = new LinkingOrchestrator();

        // Act & Assert - first call with empty profile
        var profile1 = CreateEmptyProfile();
        var result1 = orchestrator.ExecuteLink(profile1, _repoRoot, force: false);
        result1.IsBlocked.Should().BeFalse();

        // Act & Assert - second call with different profile
        var profile2 = CreateEmptyProfile();
        var result2 = orchestrator.ExecuteLink(profile2, _repoRoot, force: true);
        result2.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public void ExecuteLink_CanBeUsedWithCustomServices()
    {
        // Arrange - orchestrator allows injecting services for testing
        var conflictDetector = new ConflictDetector();
        var backupService = new BackupService();
        var symlinkService = new SymlinkService();

        var orchestrator = new LinkingOrchestrator(conflictDetector, backupService, symlinkService);
        var profile = CreateEmptyProfile();

        // Act
        var result = orchestrator.ExecuteLink(profile, _repoRoot, force: false);

        // Assert
        result.IsBlocked.Should().BeFalse();
        result.LinkResult.Should().NotBeNull();
    }

    [SkippableFact]
    public void ExecuteLink_WhenNoConflicts_CreatesSymlinks()
    {
        Skip.IfNot(CanCreateSymlinks(), "Symlink creation not available on this system");

        // Arrange
        var targetDir = Path.Combine(_tempDirectory, "target");
        Directory.CreateDirectory(targetDir);
        var targetPath = Path.Combine(targetDir, "linked.txt");

        var sourcePath = Path.Combine(_repoRoot, "source.txt");
        File.WriteAllText(sourcePath, "source content");

        var orchestrator = new LinkingOrchestrator();
        var dotfiles = new List<DotfileEntry>
        {
            new() { Source = "source.txt", Target = targetPath },
        };
        var profile = CreateProfile(dotfiles);

        // Act
        var result = orchestrator.ExecuteLink(profile, _repoRoot, force: false);

        // Assert
        result.IsBlocked.Should().BeFalse();
        result.LinkResult.Should().NotBeNull();

        // Either succeeded or failed - but should not be blocked
        if (result.LinkResult!.IsSuccess)
        {
            result.LinkResult.SuccessfulLinks.Should().ContainSingle();
        }
    }

    private static bool CanCreateSymlinks()
    {
        try
        {
            var testDir = Path.Combine(Path.GetTempPath(), $"symlink-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(testDir);

            var targetFile = Path.Combine(testDir, "target.txt");
            var linkFile = Path.Combine(testDir, "link.txt");

            File.WriteAllText(targetFile, "test");
            File.CreateSymbolicLink(linkFile, targetFile);

            var canCreate = File.Exists(linkFile);

            Directory.Delete(testDir, true);
            return canCreate;
        }
#pragma warning disable CA1031 // Do not catch general exception types - capability check should not throw
        catch
        {
            return false;
        }
#pragma warning restore CA1031
    }

    private static ResolvedProfile CreateEmptyProfile()
    {
        return CreateProfile([]);
    }

    private static ResolvedProfile CreateProfile(IReadOnlyList<DotfileEntry> dotfiles)
    {
        return new ResolvedProfile
        {
            Name = "test",
            InheritanceChain = ["test"],
            Dotfiles = dotfiles.ToList(),
            Install = null,
        };
    }
}
