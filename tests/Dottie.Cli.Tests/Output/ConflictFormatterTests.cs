// -----------------------------------------------------------------------
// <copyright file="ConflictFormatterTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Output;
using Dottie.Configuration.Linking;
using Dottie.Configuration.Models;
using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Dottie.Cli.Tests.Output;

/// <summary>
/// Tests for <see cref="ConflictFormatter"/>.
/// </summary>
public sealed class ConflictFormatterTests
{
    [Fact]
    public void WriteConflicts_WithFileConflict_OutputsStructuredList()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var conflict = new Conflict { Entry = entry, TargetPath = "/home/user/.bashrc", Type = ConflictType.File };
        var conflicts = new List<Conflict> { conflict };

        // Act
        ConflictFormatter.WriteConflicts(conflicts);

        // Assert
        var output = console.Output;
        output.Should().Contain("Error:");
        output.Should().Contain("Conflicting files detected");
        output.Should().Contain("--force");
        output.Should().Contain(".bashrc");
        output.Should().Contain("1 conflict");
    }

    [Fact]
    public void WriteConflicts_WithDirectoryConflict_OutputsStructuredList()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var entry = new DotfileEntry { Source = ".config/nvim", Target = "~/.config/nvim" };
        var conflict = new Conflict { Entry = entry, TargetPath = "/home/user/.config/nvim", Type = ConflictType.Directory };
        var conflicts = new List<Conflict> { conflict };

        // Act
        ConflictFormatter.WriteConflicts(conflicts);

        // Assert
        var output = console.Output;
        output.Should().Contain("Conflicts:");
        output.Should().Contain("directory");
        output.Should().Contain(".config/nvim");
    }

    [Fact]
    public void WriteConflicts_WithMismatchedSymlink_OutputsTargetPath()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var entry = new DotfileEntry { Source = ".vimrc", Target = "~/.vimrc" };
        var conflict = new Conflict
        {
            Entry = entry,
            TargetPath = "/home/user/.vimrc",
            Type = ConflictType.MismatchedSymlink,
            ExistingTarget = "/other/path/.vimrc",
        };
        var conflicts = new List<Conflict> { conflict };

        // Act
        ConflictFormatter.WriteConflicts(conflicts);

        // Assert
        var output = console.Output;
        output.Should().Contain("symlink");
        output.Should().Contain("/other/path/.vimrc");
    }

    [Fact]
    public void WriteConflicts_WithMultipleConflicts_OutputsAllConflicts()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var entry1 = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var entry2 = new DotfileEntry { Source = ".vimrc", Target = "~/.vimrc" };
        var entry3 = new DotfileEntry { Source = ".config/nvim", Target = "~/.config/nvim" };

        var conflicts = new List<Conflict>
        {
            new() { Entry = entry1, TargetPath = "/home/user/.bashrc", Type = ConflictType.File },
            new() { Entry = entry2, TargetPath = "/home/user/.vimrc", Type = ConflictType.MismatchedSymlink, ExistingTarget = "/wrong/.vimrc" },
            new() { Entry = entry3, TargetPath = "/home/user/.config/nvim", Type = ConflictType.Directory },
        };

        // Act
        ConflictFormatter.WriteConflicts(conflicts);

        // Assert
        var output = console.Output;
        output.Should().Contain(".bashrc");
        output.Should().Contain(".vimrc");
        output.Should().Contain(".config/nvim");
        output.Should().Contain("3 conflict");
    }

    [Fact]
    public void WriteBackupResults_WithSuccessfulBackups_OutputsPathMapping()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var timestamp = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var results = new List<BackupResult>
        {
            BackupResult.Success("/home/user/.bashrc", "/home/user/.bashrc.backup.20240115-103000", timestamp),
            BackupResult.Success("/home/user/.vimrc", "/home/user/.vimrc.backup.20240115-103000", timestamp),
        };

        // Act
        ConflictFormatter.WriteBackupResults(results);

        // Assert
        var output = console.Output;
        output.Should().Contain("Backed up 2 file");
        output.Should().Contain(".bashrc");
        output.Should().Contain(".bashrc.backup.20240115-103000");
        output.Should().Contain(".vimrc");
        output.Should().Contain(".vimrc.backup.20240115-103000");
    }

    [Fact]
    public void WriteBackupResults_WithFailedBackup_OutputsError()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var results = new List<BackupResult>
        {
            BackupResult.Failure("/home/user/.bashrc", "Permission denied"),
        };

        // Act
        ConflictFormatter.WriteBackupResults(results);

        // Assert
        var output = console.Output;
        output.Should().Contain("Failed to backup");
        output.Should().Contain(".bashrc");
        output.Should().Contain("Permission denied");
    }

    [Fact]
    public void WriteLinkResults_WithSuccessfulLinks_OutputsCount()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var linkResult = LinkResult.Success(entry, "/home/user/.bashrc");
        var result = new LinkOperationResult
        {
            SuccessfulLinks = [linkResult],
            FailedLinks = [],
            SkippedLinks = [],
        };

        // Act
        ConflictFormatter.WriteLinkResults(result);

        // Assert
        var output = console.Output;
        output.Should().Contain("Created 1 symlink");
    }

    [Fact]
    public void WriteLinkResults_WithSkippedLinks_OutputsSkippedCount()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var linkResult = LinkResult.Skipped(entry, "/home/user/.bashrc");
        var result = new LinkOperationResult
        {
            SuccessfulLinks = [],
            FailedLinks = [],
            SkippedLinks = [linkResult],
        };

        // Act
        ConflictFormatter.WriteLinkResults(result);

        // Assert
        var output = console.Output;
        output.Should().Contain("Skipped 1 file");
        output.Should().Contain("already linked");
    }

    [Fact]
    public void WriteLinkResults_WithFailedLinks_OutputsErrors()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var linkResult = LinkResult.Failure(entry, "/home/user/.bashrc", "Access denied");
        var result = new LinkOperationResult
        {
            SuccessfulLinks = [],
            FailedLinks = [linkResult],
            SkippedLinks = [],
        };

        // Act
        ConflictFormatter.WriteLinkResults(result);

        // Assert
        var output = console.Output;
        output.Should().Contain("Failed to link");
        output.Should().Contain(".bashrc");
        output.Should().Contain("Access denied");
    }

    [Fact]
    public void WriteConflicts_WithNullConflicts_ThrowsArgumentNullException()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        // Act & Assert
        var act = () => ConflictFormatter.WriteConflicts(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WriteBackupResults_WithNullResults_ThrowsArgumentNullException()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        // Act & Assert
        var act = () => ConflictFormatter.WriteBackupResults(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WriteLinkResults_WithNullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        // Act & Assert
        var act = () => ConflictFormatter.WriteLinkResults(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
