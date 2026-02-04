// -----------------------------------------------------------------------
// <copyright file="LinkPhaseResultTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Models;
using Dottie.Configuration.Linking;
using Dottie.Configuration.Models;
using FluentAssertions;
using Xunit;

namespace Dottie.Cli.Tests.Models;

/// <summary>
/// Unit tests for <see cref="LinkPhaseResult"/>.
/// </summary>
public sealed class LinkPhaseResultTests
{
    [Fact]
    public void NotExecuted_ReturnsCorrectResult()
    {
        // Act
        var result = LinkPhaseResult.NotExecuted();

        // Assert
        result.WasExecuted.Should().BeFalse();
        result.WasBlocked.Should().BeFalse();
        result.ExecutionResult.Should().BeNull();
        result.HasFailures.Should().BeFalse();
    }

    [Fact]
    public void Blocked_ReturnsCorrectResult()
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
        var executionResult = LinkExecutionResult.Blocked(conflictResult);

        // Act
        var result = LinkPhaseResult.Blocked(executionResult);

        // Assert
        result.WasExecuted.Should().BeTrue();
        result.WasBlocked.Should().BeTrue();
        result.ExecutionResult.Should().BeSameAs(executionResult);
    }

    [Fact]
    public void Executed_ReturnsCorrectResult()
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
        var executionResult = LinkExecutionResult.Completed(linkOpResult, []);

        // Act
        var result = LinkPhaseResult.Executed(executionResult);

        // Assert
        result.WasExecuted.Should().BeTrue();
        result.WasBlocked.Should().BeFalse();
        result.ExecutionResult.Should().BeSameAs(executionResult);
    }

    [Fact]
    public void HasFailures_WhenNoFailedLinks_ReturnsFalse()
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
        var executionResult = LinkExecutionResult.Completed(linkOpResult, []);

        // Act
        var result = LinkPhaseResult.Executed(executionResult);

        // Assert
        result.HasFailures.Should().BeFalse();
    }

    [Fact]
    public void HasFailures_WhenHasFailedLinks_ReturnsTrue()
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
        var executionResult = LinkExecutionResult.Completed(linkOpResult, []);

        // Act
        var result = LinkPhaseResult.Executed(executionResult);

        // Assert
        result.HasFailures.Should().BeTrue();
    }

    [Fact]
    public void HasFailures_WhenNoExecutionResult_ReturnsFalse()
    {
        // Act
        var result = LinkPhaseResult.NotExecuted();

        // Assert
        result.HasFailures.Should().BeFalse();
    }

    [Fact]
    public void HasFailures_WhenExecutionResultHasNoLinkResult_ReturnsFalse()
    {
        // Arrange - blocked result has no LinkResult
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
        var executionResult = LinkExecutionResult.Blocked(conflictResult);

        // Act
        var result = LinkPhaseResult.Blocked(executionResult);

        // Assert
        result.HasFailures.Should().BeFalse();
    }
}
