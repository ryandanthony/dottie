// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Linking;
using Dottie.Configuration.Models;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Linking;

/// <summary>
/// Tests for <see cref="ConflictDetector"/>.
/// </summary>
public sealed class ConflictDetectorTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _repoRoot;
    private readonly bool _canCreateSymlinks;
    private bool _disposed;

    public ConflictDetectorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "dottie-conflict-tests", Guid.NewGuid().ToString());
        _repoRoot = Path.Combine(_testDir, "repo");
        Directory.CreateDirectory(_repoRoot);
        _canCreateSymlinks = CanCreateSymlinks();
    }

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
            Directory.Delete(_testDir, recursive: true);
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

    [Fact]
    public void DetectConflicts_WhenNoTargetsExist_ReturnsNoConflicts()
    {
        // Arrange
        var sourceDir = Path.Combine(_repoRoot, "dotfiles");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "bashrc"), "# test bashrc");

        var targetPath = Path.Combine(_testDir, "target", ".bashrc");
        var dotfiles = new List<DotfileEntry>
        {
            new() { Source = "dotfiles/bashrc", Target = targetPath },
        };
        var detector = new ConflictDetector();

        // Act
        var result = detector.DetectConflicts(dotfiles, _repoRoot);

        // Assert
        result.HasConflicts.Should().BeFalse();
        result.Conflicts.Should().BeEmpty();
        result.SafeEntries.Should().HaveCount(1);
    }

    [Fact]
    public void DetectConflicts_WhenTargetFileExists_ReturnsFileConflict()
    {
        // Arrange
        var sourceDir = Path.Combine(_repoRoot, "dotfiles");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "bashrc"), "# repo bashrc");

        var targetDir = Path.Combine(_testDir, "home");
        Directory.CreateDirectory(targetDir);
        var targetPath = Path.Combine(targetDir, ".bashrc");
        File.WriteAllText(targetPath, "# existing bashrc");

        var dotfiles = new List<DotfileEntry>
        {
            new() { Source = "dotfiles/bashrc", Target = targetPath },
        };
        var detector = new ConflictDetector();

        // Act
        var result = detector.DetectConflicts(dotfiles, _repoRoot);

        // Assert
        result.HasConflicts.Should().BeTrue();
        result.Conflicts.Should().HaveCount(1);
        result.Conflicts[0].Type.Should().Be(ConflictType.File);
        result.Conflicts[0].TargetPath.Should().Be(targetPath);
    }

    [Fact]
    public void DetectConflicts_WhenTargetDirectoryExists_ReturnsDirectoryConflict()
    {
        // Arrange
        var sourceDir = Path.Combine(_repoRoot, "dotfiles", "config");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "settings.json"), "{}");

        var targetDir = Path.Combine(_testDir, "home");
        var targetPath = Path.Combine(targetDir, ".config");
        Directory.CreateDirectory(targetPath);
        File.WriteAllText(Path.Combine(targetPath, "existing.txt"), "existing");

        var dotfiles = new List<DotfileEntry>
        {
            new() { Source = "dotfiles/config", Target = targetPath },
        };
        var detector = new ConflictDetector();

        // Act
        var result = detector.DetectConflicts(dotfiles, _repoRoot);

        // Assert
        result.HasConflicts.Should().BeTrue();
        result.Conflicts.Should().HaveCount(1);
        result.Conflicts[0].Type.Should().Be(ConflictType.Directory);
        result.Conflicts[0].TargetPath.Should().Be(targetPath);
    }

    [SkippableFact]
    public void DetectConflicts_WhenTargetIsCorrectSymlink_ReturnsNoConflict()
    {
        Skip.IfNot(_canCreateSymlinks, "Symlink creation not available on this system");

        // Arrange
        var sourceDir = Path.Combine(_repoRoot, "dotfiles");
        Directory.CreateDirectory(sourceDir);
        var sourcePath = Path.Combine(sourceDir, "bashrc");
        File.WriteAllText(sourcePath, "# repo bashrc");

        var targetDir = Path.Combine(_testDir, "home");
        Directory.CreateDirectory(targetDir);
        var targetPath = Path.Combine(targetDir, ".bashrc");

        // Create correct symlink
        File.CreateSymbolicLink(targetPath, sourcePath);

        var dotfiles = new List<DotfileEntry>
        {
            new() { Source = "dotfiles/bashrc", Target = targetPath },
        };
        var detector = new ConflictDetector();

        // Act
        var result = detector.DetectConflicts(dotfiles, _repoRoot);

        // Assert
        result.HasConflicts.Should().BeFalse();
        result.AlreadyLinked.Should().HaveCount(1);
    }

    [SkippableFact]
    public void DetectConflicts_WhenTargetIsMismatchedSymlink_ReturnsMismatchedSymlinkConflict()
    {
        Skip.IfNot(_canCreateSymlinks, "Symlink creation not available on this system");

        // Arrange
        var sourceDir = Path.Combine(_repoRoot, "dotfiles");
        Directory.CreateDirectory(sourceDir);
        var sourcePath = Path.Combine(sourceDir, "bashrc");
        File.WriteAllText(sourcePath, "# repo bashrc");

        var wrongSource = Path.Combine(_testDir, "wrong-bashrc");
        File.WriteAllText(wrongSource, "# wrong file");

        var targetDir = Path.Combine(_testDir, "home");
        Directory.CreateDirectory(targetDir);
        var targetPath = Path.Combine(targetDir, ".bashrc");

        // Create mismatched symlink
        File.CreateSymbolicLink(targetPath, wrongSource);

        var dotfiles = new List<DotfileEntry>
        {
            new() { Source = "dotfiles/bashrc", Target = targetPath },
        };
        var detector = new ConflictDetector();

        // Act
        var result = detector.DetectConflicts(dotfiles, _repoRoot);

        // Assert
        result.HasConflicts.Should().BeTrue();
        result.Conflicts.Should().HaveCount(1);
        result.Conflicts[0].Type.Should().Be(ConflictType.MismatchedSymlink);
        result.Conflicts[0].ExistingTarget.Should().Be(wrongSource);
    }

    [Fact]
    public void DetectConflicts_WithMultipleConflicts_ReturnsAllConflicts()
    {
        // Arrange
        var sourceDir = Path.Combine(_repoRoot, "dotfiles");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "bashrc"), "# bashrc");
        File.WriteAllText(Path.Combine(sourceDir, "gitconfig"), "# gitconfig");
        File.WriteAllText(Path.Combine(sourceDir, "vimrc"), "# vimrc");

        var targetDir = Path.Combine(_testDir, "home");
        Directory.CreateDirectory(targetDir);

        // Create conflicts
        var bashrcPath = Path.Combine(targetDir, ".bashrc");
        var gitconfigPath = Path.Combine(targetDir, ".gitconfig");
        var vimrcPath = Path.Combine(targetDir, ".vimrc");

        File.WriteAllText(bashrcPath, "existing bashrc");
        File.WriteAllText(gitconfigPath, "existing gitconfig");

        // Note: vimrc doesn't exist - no conflict
        var dotfiles = new List<DotfileEntry>
        {
            new() { Source = "dotfiles/bashrc", Target = bashrcPath },
            new() { Source = "dotfiles/gitconfig", Target = gitconfigPath },
            new() { Source = "dotfiles/vimrc", Target = vimrcPath },
        };
        var detector = new ConflictDetector();

        // Act
        var result = detector.DetectConflicts(dotfiles, _repoRoot);

        // Assert
        result.HasConflicts.Should().BeTrue();
        result.Conflicts.Should().HaveCount(2);
        result.SafeEntries.Should().HaveCount(1);
        result.Conflicts.Should().Contain(c => c.TargetPath == bashrcPath);
        result.Conflicts.Should().Contain(c => c.TargetPath == gitconfigPath);
    }
}
