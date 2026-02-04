// -----------------------------------------------------------------------
// <copyright file="LinkResultTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Linking;
using Dottie.Configuration.Models;
using FluentAssertions;
using Xunit;

namespace Dottie.Configuration.Tests.Linking;

/// <summary>
/// Tests for the LinkResult record.
/// </summary>
public class LinkResultTests
{
    /// <summary>
    /// Tests that Success factory method creates a successful result.
    /// </summary>
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var expandedPath = "~/.bashrc";

        // Act
        var result = LinkResult.Success(entry, expandedPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Entry.Should().Be(entry);
        result.ExpandedTargetPath.Should().Be(expandedPath);
        result.BackupResult.Should().BeNull();
        result.Error.Should().BeNull();
        result.WasSkipped.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Success factory method creates a result with backup.
    /// </summary>
    [Fact]
    public void Success_WithBackupResult_CreatesSuccessfulResultWithBackup()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var expandedPath = "~/.bashrc";
        var backup = BackupResult.Success(
            "~/.bashrc",
            "~/.bashrc.backup.2024-01-01",
            DateTimeOffset.UtcNow);

        // Act
        var result = LinkResult.Success(entry, expandedPath, backup);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Entry.Should().Be(entry);
        result.ExpandedTargetPath.Should().Be(expandedPath);
        result.BackupResult.Should().Be(backup);
        result.Error.Should().BeNull();
        result.WasSkipped.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Skipped factory method creates a skipped result.
    /// </summary>
    [Fact]
    public void Skipped_CreatesSkippedResult()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var expandedPath = "~/.bashrc";

        // Act
        var result = LinkResult.Skipped(entry, expandedPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Entry.Should().Be(entry);
        result.ExpandedTargetPath.Should().Be(expandedPath);
        result.BackupResult.Should().BeNull();
        result.Error.Should().BeNull();
        result.WasSkipped.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Failure factory method creates a failed result.
    /// </summary>
    [Fact]
    public void Failure_CreatesFailedResult()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var expandedPath = "~/.bashrc";
        var error = "Permission denied";

        // Act
        var result = LinkResult.Failure(entry, expandedPath, error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Entry.Should().Be(entry);
        result.ExpandedTargetPath.Should().Be(expandedPath);
        result.BackupResult.Should().BeNull();
        result.Error.Should().Be(error);
        result.WasSkipped.Should().BeFalse();
    }

    /// <summary>
    /// Tests that record equality works for successful results.
    /// </summary>
    [Fact]
    public void RecordEquality_SuccessResults_WorksCorrectly()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var expandedPath = "~/.bashrc";

        var result1 = LinkResult.Success(entry, expandedPath);
        var result2 = LinkResult.Success(entry, expandedPath);

        // Act & Assert
        result1.Should().Be(result2);
    }

    /// <summary>
    /// Tests that record equality works for skipped results.
    /// </summary>
    [Fact]
    public void RecordEquality_SkippedResults_WorksCorrectly()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var expandedPath = "~/.bashrc";

        var result1 = LinkResult.Skipped(entry, expandedPath);
        var result2 = LinkResult.Skipped(entry, expandedPath);

        // Act & Assert
        result1.Should().Be(result2);
    }

    /// <summary>
    /// Tests that record equality works for failure results.
    /// </summary>
    [Fact]
    public void RecordEquality_FailureResults_WorksCorrectly()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var expandedPath = "~/.bashrc";
        var error = "Permission denied";

        var result1 = LinkResult.Failure(entry, expandedPath, error);
        var result2 = LinkResult.Failure(entry, expandedPath, error);

        // Act & Assert
        result1.Should().Be(result2);
    }

    /// <summary>
    /// Tests that record inequality works for different result types.
    /// </summary>
    [Fact]
    public void RecordInequality_DifferentResults_WorksCorrectly()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var expandedPath = "~/.bashrc";

        var result1 = LinkResult.Success(entry, expandedPath);
        var result2 = LinkResult.Skipped(entry, expandedPath);

        // Act & Assert
        result1.Should().NotBe(result2);
    }

    /// <summary>
    /// Tests that record inequality works for different errors.
    /// </summary>
    [Fact]
    public void RecordInequality_DifferentErrors_WorksCorrectly()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var expandedPath = "~/.bashrc";

        var result1 = LinkResult.Failure(entry, expandedPath, "Permission denied");
        var result2 = LinkResult.Failure(entry, expandedPath, "File not found");

        // Act & Assert
        result1.Should().NotBe(result2);
    }

    /// <summary>
    /// Tests that Success results with different expanded target paths are not equal.
    /// </summary>
    [Fact]
    public void Success_WithDifferentExpandedTargetPaths_AreNotEqual()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };

        var result1 = LinkResult.Success(entry, "~/.bashrc");
        var result2 = LinkResult.Success(entry, "/home/user/.bashrc");

        // Act & Assert
        result1.Should().NotBe(result2);
    }

    /// <summary>
    /// Tests failure results with various error messages.
    /// </summary>
    [Theory]
    [InlineData("Permission denied")]
    [InlineData("File not found")]
    [InlineData("Symlink already exists")]
    [InlineData("Target directory does not exist")]
    public void Failure_WithVariousErrors_CreatesFailedResult(string error)
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var expandedPath = "~/.bashrc";

        // Act
        var result = LinkResult.Failure(entry, expandedPath, error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }
}
