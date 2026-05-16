// -----------------------------------------------------------------------
// <copyright file="InstallPhaseResultTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Models;
using Dottie.Configuration.Installing;
using FluentAssertions;
using Xunit;

namespace Dottie.Cli.Tests.Models;

/// <summary>
/// Unit tests for <see cref="InstallPhaseResult"/>.
/// </summary>
public sealed class InstallPhaseResultTests
{
    [Fact]
    public void NotExecuted_ReturnsCorrectResult()
    {
        // Act
        var result = InstallPhaseResult.NotExecuted();

        // Assert
        result.WasExecuted.Should().BeFalse();
        result.Results.Should().BeEmpty();
        result.HasFailures.Should().BeFalse();
    }

    [Fact]
    public void Executed_WithEmptyResults_ReturnsCorrectResult()
    {
        // Act
        var result = InstallPhaseResult.Executed([]);

        // Assert
        result.WasExecuted.Should().BeTrue();
        result.Results.Should().BeEmpty();
        result.HasFailures.Should().BeFalse();
    }

    [Fact]
    public void Executed_WithSuccessfulResults_ReturnsCorrectResult()
    {
        // Arrange
        var installResults = new List<InstallResult>
        {
            InstallResult.Success("package1", InstallSourceType.AptPackage),
            InstallResult.Skipped("package2", InstallSourceType.AptPackage, "Already installed"),
        };

        // Act
        var result = InstallPhaseResult.Executed(installResults);

        // Assert
        result.WasExecuted.Should().BeTrue();
        result.Results.Should().HaveCount(2);
        result.HasFailures.Should().BeFalse();
    }

    [Fact]
    public void HasFailures_WhenHasFailedInstall_ReturnsTrue()
    {
        // Arrange
        var installResults = new List<InstallResult>
        {
            InstallResult.Success("package1", InstallSourceType.AptPackage),
            InstallResult.Failed("package2", InstallSourceType.AptPackage, "Package not found"),
        };

        // Act
        var result = InstallPhaseResult.Executed(installResults);

        // Assert
        result.HasFailures.Should().BeTrue();
    }

    [Fact]
    public void HasFailures_WhenAllSkipped_ReturnsFalse()
    {
        // Arrange
        var installResults = new List<InstallResult>
        {
            InstallResult.Skipped("package1", InstallSourceType.AptPackage, "Already installed"),
        };

        // Act
        var result = InstallPhaseResult.Executed(installResults);

        // Assert
        result.HasFailures.Should().BeFalse();
    }

    [Fact]
    public void Results_DefaultsToEmptyList()
    {
        // Arrange
        var result = new InstallPhaseResult { WasExecuted = true };

        // Assert
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public void SummaryProperties_WithMixedResults_ReturnExpectedCounts()
    {
        // Arrange
        var result = InstallPhaseResult.Executed(
        [
            InstallResult.Success("package1", InstallSourceType.AptPackage),
            InstallResult.Success("package2", InstallSourceType.GithubRelease),
            InstallResult.Skipped("package3", InstallSourceType.Script, "Already current"),
            InstallResult.Warning("package4", InstallSourceType.Font, "Needs attention"),
            InstallResult.Failed("package5", InstallSourceType.SnapPackage, "Failed"),
        ]);

        // Assert
        result.TotalCount.Should().Be(5);
        result.InstalledCount.Should().Be(2);
        result.SkippedCount.Should().Be(1);
        result.CompletedCount.Should().Be(3);
        result.WarningCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.RemainingCount.Should().Be(2);
        result.CompletionPercentage.Should().Be(60);
    }
}
