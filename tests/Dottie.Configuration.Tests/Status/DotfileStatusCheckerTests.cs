// -----------------------------------------------------------------------
// <copyright file="DotfileStatusCheckerTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Models;
using Dottie.Configuration.Status;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Status;

/// <summary>
/// Tests for <see cref="DotfileStatusChecker"/>.
/// </summary>
public sealed class DotfileStatusCheckerTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _repoRoot;
    private readonly bool _canCreateSymlinks;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotfileStatusCheckerTests"/> class.
    /// </summary>
    public DotfileStatusCheckerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "dottie-status-tests", Guid.NewGuid().ToString());
        _repoRoot = Path.Combine(_testDir, "repo");
        Directory.CreateDirectory(_repoRoot);
        _canCreateSymlinks = CanCreateSymlinks();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        _disposed = true;
    }

    private bool CanCreateSymlinks()
    {
        var testFile = Path.Combine(_testDir, "symlink-test-source.txt");
        var testLink = Path.Combine(_testDir, "symlink-test-link.txt");

        try
        {
            File.WriteAllText(testFile, "test");
            File.CreateSymbolicLink(testLink, testFile);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            if (File.Exists(testLink))
            {
                File.Delete(testLink);
            }

            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    [SkippableFact]
    public void CheckStatus_WhenSymlinkPointsToCorrectSource_ReturnsLinkedState()
    {
        Skip.IfNot(_canCreateSymlinks, "Cannot create symlinks on this system");

        // Arrange
        var sourceFile = Path.Combine(_repoRoot, "dotfiles", "bashrc");
        var targetPath = Path.Combine(_testDir, "home", ".bashrc");

        Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(sourceFile, "# bashrc content");
        File.CreateSymbolicLink(targetPath, sourceFile);

        var entry = new DotfileEntry { Source = "dotfiles/bashrc", Target = targetPath };
        var checker = new DotfileStatusChecker();

        // Act
        var results = checker.CheckStatus([entry], _repoRoot);

        // Assert
        results.Should().HaveCount(1);
        results[0].State.Should().Be(DotfileLinkState.Linked);
        results[0].Entry.Should().Be(entry);
    }

    [Fact]
    public void CheckStatus_WhenTargetDoesNotExist_ReturnsMissingState()
    {
        // Arrange
        var sourceFile = Path.Combine(_repoRoot, "dotfiles", "vimrc");
        Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
        File.WriteAllText(sourceFile, "\" vimrc content");

        var targetPath = Path.Combine(_testDir, "home", ".vimrc"); // Does not exist

        var entry = new DotfileEntry { Source = "dotfiles/vimrc", Target = targetPath };
        var checker = new DotfileStatusChecker();

        // Act
        var results = checker.CheckStatus([entry], _repoRoot);

        // Assert
        results.Should().HaveCount(1);
        results[0].State.Should().Be(DotfileLinkState.Missing);
        results[0].Entry.Should().Be(entry);
    }

    [SkippableFact]
    public void CheckStatus_WhenSymlinkTargetDoesNotExist_ReturnsBrokenState()
    {
        Skip.IfNot(_canCreateSymlinks, "Cannot create symlinks on this system");

        // Arrange
        var sourceFile = Path.Combine(_repoRoot, "dotfiles", "old");
        var targetPath = Path.Combine(_testDir, "home", ".old");

        Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        // Create a symlink to a non-existent file (broken link)
        File.CreateSymbolicLink(targetPath, sourceFile); // sourceFile doesn't exist

        var entry = new DotfileEntry { Source = "dotfiles/old", Target = targetPath };
        var checker = new DotfileStatusChecker();

        // Act
        var results = checker.CheckStatus([entry], _repoRoot);

        // Assert
        results.Should().HaveCount(1);
        results[0].State.Should().Be(DotfileLinkState.Broken);
        results[0].Entry.Should().Be(entry);
    }

    [Fact]
    public void CheckStatus_WhenFileExistsButIsNotSymlink_ReturnsConflictingState()
    {
        // Arrange
        var sourceFile = Path.Combine(_repoRoot, "dotfiles", "config");
        var targetPath = Path.Combine(_testDir, "home", ".config");

        Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(sourceFile, "source content");
        File.WriteAllText(targetPath, "existing file content"); // Regular file, not symlink

        var entry = new DotfileEntry { Source = "dotfiles/config", Target = targetPath };
        var checker = new DotfileStatusChecker();

        // Act
        var results = checker.CheckStatus([entry], _repoRoot);

        // Assert
        results.Should().HaveCount(1);
        results[0].State.Should().Be(DotfileLinkState.Conflicting);
        results[0].Entry.Should().Be(entry);
    }

    [Fact]
    public void CheckStatus_WhenDirectoryExistsButIsNotSymlink_ReturnsConflictingState()
    {
        // Arrange
        var sourceFile = Path.Combine(_repoRoot, "dotfiles", "nvim");
        var targetPath = Path.Combine(_testDir, "home", ".config", "nvim");

        Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
        Directory.CreateDirectory(targetPath); // Create as directory, not symlink

        var entry = new DotfileEntry { Source = "dotfiles/nvim", Target = targetPath };
        var checker = new DotfileStatusChecker();

        // Act
        var results = checker.CheckStatus([entry], _repoRoot);

        // Assert
        results.Should().HaveCount(1);
        results[0].State.Should().Be(DotfileLinkState.Conflicting);
        results[0].Entry.Should().Be(entry);
        results[0].Message.Should().Contain("directory");
    }

    [SkippableFact]
    public void CheckStatus_WhenSymlinkPointsToWrongSource_ReturnsConflictingState()
    {
        Skip.IfNot(_canCreateSymlinks, "Cannot create symlinks on this system");

        // Arrange
        var expectedSource = Path.Combine(_repoRoot, "dotfiles", "bashrc");
        var wrongSource = Path.Combine(_testDir, "other", "bashrc");
        var targetPath = Path.Combine(_testDir, "home", ".bashrc");

        Directory.CreateDirectory(Path.GetDirectoryName(expectedSource)!);
        Directory.CreateDirectory(Path.GetDirectoryName(wrongSource)!);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        File.WriteAllText(expectedSource, "# expected content");
        File.WriteAllText(wrongSource, "# wrong content");
        File.CreateSymbolicLink(targetPath, wrongSource); // Points to wrong source

        var entry = new DotfileEntry { Source = "dotfiles/bashrc", Target = targetPath };
        var checker = new DotfileStatusChecker();

        // Act
        var results = checker.CheckStatus([entry], _repoRoot);

        // Assert
        results.Should().HaveCount(1);
        results[0].State.Should().Be(DotfileLinkState.Conflicting);
        results[0].Message.Should().Contain("wrong");
    }

    [Fact]
    public void CheckStatus_WithMultipleDotfiles_ReturnsCorrectStatesForEach()
    {
        // Arrange
        var sourceLinked = Path.Combine(_repoRoot, "dotfiles", "linked");
        var sourceMissing = Path.Combine(_repoRoot, "dotfiles", "missing");
        var sourceConflict = Path.Combine(_repoRoot, "dotfiles", "conflict");

        var targetLinked = Path.Combine(_testDir, "home", ".linked");
        var targetMissing = Path.Combine(_testDir, "home", ".missing");
        var targetConflict = Path.Combine(_testDir, "home", ".conflict");

        Directory.CreateDirectory(Path.GetDirectoryName(sourceLinked)!);
        Directory.CreateDirectory(Path.GetDirectoryName(targetLinked)!);

        File.WriteAllText(sourceLinked, "linked");
        File.WriteAllText(sourceMissing, "missing");
        File.WriteAllText(sourceConflict, "conflict");

        // Create conflict file (regular file instead of symlink)
        File.WriteAllText(targetConflict, "existing");

        var entries = new[]
        {
            new DotfileEntry { Source = "dotfiles/missing", Target = targetMissing },
            new DotfileEntry { Source = "dotfiles/conflict", Target = targetConflict },
        };

        var checker = new DotfileStatusChecker();

        // Act
        var results = checker.CheckStatus(entries, _repoRoot);

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.State == DotfileLinkState.Missing);
        results.Should().Contain(r => r.State == DotfileLinkState.Conflicting);
    }

    [Fact]
    public void CheckStatus_WithTildeExpansion_ExpandsTargetPath()
    {
        // Arrange
        var sourceFile = Path.Combine(_repoRoot, "dotfiles", "bashrc");
        Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
        File.WriteAllText(sourceFile, "# content");

        var entry = new DotfileEntry { Source = "dotfiles/bashrc", Target = "~/.bashrc" };
        var checker = new DotfileStatusChecker();

        // Act
        var results = checker.CheckStatus([entry], _repoRoot);

        // Assert
        results.Should().HaveCount(1);
        results[0].ExpandedTarget.Should().NotStartWith("~");
        results[0].ExpandedTarget.Should().Contain(".bashrc");
    }

    [Fact]
    public void CheckStatus_WithEmptyList_ReturnsEmptyResults()
    {
        // Arrange
        var checker = new DotfileStatusChecker();

        // Act
        var results = checker.CheckStatus([], _repoRoot);

        // Assert
        results.Should().BeEmpty();
    }
}
