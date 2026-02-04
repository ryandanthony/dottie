// -----------------------------------------------------------------------
// <copyright file="ApplyResultTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Models;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Linking;
using Dottie.Configuration.Models;
using FluentAssertions;
using Xunit;

namespace Dottie.Cli.Tests.Models;

/// <summary>
/// Unit tests for <see cref="ApplyResult"/>.
/// </summary>
public sealed class ApplyResultTests
{
    [Fact]
    public void OverallSuccess_WhenLinkAndInstallNotExecuted_ReturnsTrue()
    {
        // Arrange
        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.NotExecuted(),
            InstallPhase = InstallPhaseResult.NotExecuted(),
        };

        // Act & Assert
        result.OverallSuccess.Should().BeTrue();
    }

    [Fact]
    public void OverallSuccess_WhenLinkBlocked_ReturnsFalse()
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

        // Act & Assert
        result.OverallSuccess.Should().BeFalse();
    }

    [Fact]
    public void OverallSuccess_WhenLinkHasFailures_ReturnsFalse()
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

        // Act & Assert
        result.OverallSuccess.Should().BeFalse();
    }

    [Fact]
    public void OverallSuccess_WhenInstallHasFailures_ReturnsFalse()
    {
        // Arrange
        var result = new ApplyResult
        {
            LinkPhase = LinkPhaseResult.NotExecuted(),
            InstallPhase = InstallPhaseResult.Executed([
                InstallResult.Failed("test-package", InstallSourceType.AptPackage, "Package not found"),
            ]),
        };

        // Act & Assert
        result.OverallSuccess.Should().BeFalse();
    }

    [Fact]
    public void OverallSuccess_WhenBothPhasesExecutedSuccessfully_ReturnsTrue()
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
                InstallResult.Success("test-package", InstallSourceType.AptPackage),
            ]),
        };

        // Act & Assert
        result.OverallSuccess.Should().BeTrue();
    }
}
