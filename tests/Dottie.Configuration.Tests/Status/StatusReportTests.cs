// -----------------------------------------------------------------------
// <copyright file="StatusReportTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing;
using Dottie.Configuration.Models;
using Dottie.Configuration.Status;
using FluentAssertions;
using Xunit;

namespace Dottie.Configuration.Tests.Status;

/// <summary>
/// Tests for the StatusReport record.
/// </summary>
public class StatusReportTests
{
    /// <summary>
    /// Tests that HasDotfiles returns false when no dotfile statuses are provided.
    /// </summary>
    [Fact]
    public void HasDotfiles_WhenEmpty_ReturnsFalse()
    {
        // Arrange
        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            Array.Empty<DotfileStatusEntry>(),
            Array.Empty<SoftwareStatusEntry>());

        // Act
        var hasDotfiles = report.HasDotfiles;

        // Assert
        hasDotfiles.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasDotfiles returns true when dotfile statuses are provided.
    /// </summary>
    [Fact]
    public void HasDotfiles_WhenPresent_ReturnsTrue()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var status = new DotfileStatusEntry(
            entry,
            DotfileLinkState.Linked,
            null,
            "~/.bashrc");

        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            new[] { status },
            Array.Empty<SoftwareStatusEntry>());

        // Act
        var hasDotfiles = report.HasDotfiles;

        // Assert
        hasDotfiles.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasSoftware returns false when no software statuses are provided.
    /// </summary>
    [Fact]
    public void HasSoftware_WhenEmpty_ReturnsFalse()
    {
        // Arrange
        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            Array.Empty<DotfileStatusEntry>(),
            Array.Empty<SoftwareStatusEntry>());

        // Act
        var hasSoftware = report.HasSoftware;

        // Assert
        hasSoftware.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasSoftware returns true when software statuses are provided.
    /// </summary>
    [Fact]
    public void HasSoftware_WhenPresent_ReturnsTrue()
    {
        // Arrange
        var status = new SoftwareStatusEntry(
            "nodejs",
            InstallSourceType.GithubRelease,
            SoftwareInstallState.Installed,
            "20.0.0",
            "20.0.0",
            "/usr/bin/node",
            null);

        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            Array.Empty<DotfileStatusEntry>(),
            new[] { status });

        // Act
        var hasSoftware = report.HasSoftware;

        // Assert
        hasSoftware.Should().BeTrue();
    }

    /// <summary>
    /// Tests that LinkedCount returns the correct count of linked dotfiles.
    /// </summary>
    [Fact]
    public void LinkedCount_ReturnsCorrectCount()
    {
        // Arrange
        var entry1 = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var entry2 = new DotfileEntry { Source = ".vimrc", Target = "~/.vimrc" };
        var entry3 = new DotfileEntry { Source = ".zshrc", Target = "~/.zshrc" };

        var linkedStatus = new DotfileStatusEntry(
            entry1,
            DotfileLinkState.Linked,
            null,
            "~/.bashrc");

        var missingStatus = new DotfileStatusEntry(
            entry2,
            DotfileLinkState.Missing,
            null,
            "~/.vimrc");

        var brokenStatus = new DotfileStatusEntry(
            entry3,
            DotfileLinkState.Broken,
            null,
            "~/.zshrc");

        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            new[] { linkedStatus, missingStatus, brokenStatus },
            Array.Empty<SoftwareStatusEntry>());

        // Act
        var count = report.LinkedCount;

        // Assert
        count.Should().Be(1);
    }

    /// <summary>
    /// Tests that MissingDotfilesCount returns the correct count.
    /// </summary>
    [Fact]
    public void MissingDotfilesCount_ReturnsCorrectCount()
    {
        // Arrange
        var entry1 = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var entry2 = new DotfileEntry { Source = ".vimrc", Target = "~/.vimrc" };

        var linkedStatus = new DotfileStatusEntry(
            entry1,
            DotfileLinkState.Linked,
            null,
            "~/.bashrc");

        var missingStatus = new DotfileStatusEntry(
            entry2,
            DotfileLinkState.Missing,
            null,
            "~/.vimrc");

        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            new[] { linkedStatus, missingStatus },
            Array.Empty<SoftwareStatusEntry>());

        // Act
        var count = report.MissingDotfilesCount;

        // Assert
        count.Should().Be(1);
    }

    /// <summary>
    /// Tests that BrokenCount returns the correct count.
    /// </summary>
    [Fact]
    public void BrokenCount_ReturnsCorrectCount()
    {
        // Arrange
        var entry1 = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var entry2 = new DotfileEntry { Source = ".vimrc", Target = "~/.vimrc" };
        var entry3 = new DotfileEntry { Source = ".zshrc", Target = "~/.zshrc" };

        var linkedStatus = new DotfileStatusEntry(
            entry1,
            DotfileLinkState.Linked,
            null,
            "~/.bashrc");

        var brokenStatus1 = new DotfileStatusEntry(
            entry2,
            DotfileLinkState.Broken,
            null,
            "~/.vimrc");

        var brokenStatus2 = new DotfileStatusEntry(
            entry3,
            DotfileLinkState.Broken,
            null,
            "~/.zshrc");

        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            new[] { linkedStatus, brokenStatus1, brokenStatus2 },
            Array.Empty<SoftwareStatusEntry>());

        // Act
        var count = report.BrokenCount;

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Tests that ConflictingCount returns the correct count.
    /// </summary>
    [Fact]
    public void ConflictingCount_ReturnsCorrectCount()
    {
        // Arrange
        var entry1 = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var entry2 = new DotfileEntry { Source = ".vimrc", Target = "~/.vimrc" };

        var linkedStatus = new DotfileStatusEntry(
            entry1,
            DotfileLinkState.Linked,
            null,
            "~/.bashrc");

        var conflictStatus = new DotfileStatusEntry(
            entry2,
            DotfileLinkState.Conflicting,
            null,
            "~/.vimrc");

        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            new[] { linkedStatus, conflictStatus },
            Array.Empty<SoftwareStatusEntry>());

        // Act
        var count = report.ConflictingCount;

        // Assert
        count.Should().Be(1);
    }

    /// <summary>
    /// Tests that InstalledCount returns the correct count of installed software.
    /// </summary>
    [Fact]
    public void InstalledCount_ReturnsCorrectCount()
    {
        // Arrange
        var status1 = new SoftwareStatusEntry(
            "nodejs",
            InstallSourceType.GithubRelease,
            SoftwareInstallState.Installed,
            "20.0.0",
            "20.0.0",
            "/usr/bin/node",
            null);

        var status2 = new SoftwareStatusEntry(
            "python",
            InstallSourceType.AptPackage,
            SoftwareInstallState.Missing,
            null,
            null,
            null,
            null);

        var status3 = new SoftwareStatusEntry(
            "ruby",
            InstallSourceType.AptPackage,
            SoftwareInstallState.Installed,
            "3.0.0",
            "3.0.0",
            "/usr/bin/ruby",
            null);

        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            Array.Empty<DotfileStatusEntry>(),
            new[] { status1, status2, status3 });

        // Act
        var count = report.InstalledCount;

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Tests that MissingSoftwareCount returns the correct count.
    /// </summary>
    [Fact]
    public void MissingSoftwareCount_ReturnsCorrectCount()
    {
        // Arrange
        var status1 = new SoftwareStatusEntry(
            "nodejs",
            InstallSourceType.GithubRelease,
            SoftwareInstallState.Installed,
            "20.0.0",
            "20.0.0",
            "/usr/bin/node",
            null);

        var status2 = new SoftwareStatusEntry(
            "python",
            InstallSourceType.AptPackage,
            SoftwareInstallState.Missing,
            null,
            null,
            null,
            null);

        var status3 = new SoftwareStatusEntry(
            "ruby",
            InstallSourceType.AptPackage,
            SoftwareInstallState.Missing,
            null,
            null,
            null,
            null);

        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            Array.Empty<DotfileStatusEntry>(),
            new[] { status1, status2, status3 });

        // Act
        var count = report.MissingSoftwareCount;

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Tests that OutdatedCount returns the correct count.
    /// </summary>
    [Fact]
    public void OutdatedCount_ReturnsCorrectCount()
    {
        // Arrange
        var status1 = new SoftwareStatusEntry(
            "nodejs",
            InstallSourceType.GithubRelease,
            SoftwareInstallState.Installed,
            "20.0.0",
            "21.0.0",
            "/usr/bin/node",
            null);

        var status2 = new SoftwareStatusEntry(
            "python",
            InstallSourceType.AptPackage,
            SoftwareInstallState.Installed,
            "3.9.0",
            "3.9.0",
            "/usr/bin/python",
            null);

        var status3 = new SoftwareStatusEntry(
            "ruby",
            InstallSourceType.AptPackage,
            SoftwareInstallState.Outdated,
            "2.7.0",
            "3.0.0",
            "/usr/bin/ruby",
            null);

        var report = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            Array.Empty<DotfileStatusEntry>(),
            new[] { status1, status2, status3 });

        // Act
        var count = report.OutdatedCount;

        // Assert
        count.Should().Be(1);
    }

    /// <summary>
    /// Tests that record inequality works correctly.
    /// </summary>
    [Fact]
    public void RecordInequality_WorksCorrectly()
    {
        // Arrange
        var entry = new DotfileEntry { Source = ".bashrc", Target = "~/.bashrc" };
        var dotfileStatus = new DotfileStatusEntry(
            entry,
            DotfileLinkState.Linked,
            null,
            "~/.bashrc");

        var report1 = new StatusReport(
            "test-profile",
            Array.Empty<string>(),
            new[] { dotfileStatus },
            Array.Empty<SoftwareStatusEntry>());

        var report2 = new StatusReport(
            "different-profile",
            Array.Empty<string>(),
            new[] { dotfileStatus },
            Array.Empty<SoftwareStatusEntry>());

        // Act & Assert
        report1.Should().NotBe(report2);
    }
}

