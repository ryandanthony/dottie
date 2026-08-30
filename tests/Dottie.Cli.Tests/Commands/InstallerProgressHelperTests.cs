// -----------------------------------------------------------------------
// <copyright file="InstallerProgressHelperTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Commands;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="InstallerProgressHelper"/>.
/// </summary>
public sealed class InstallerProgressHelperTests
{
    private static InstallBlock FullBlock() => new()
    {
        Github = [new GithubReleaseItem { Repo = "o/rg", Asset = "a", Binary = "rg" }],
        Apt = ["git", "curl"],
        AptRepos = [new AptRepoItem { Name = "docker", KeyUrl = "https://x/key", Repo = "deb https://x stable main", Packages = ["docker-ce"] }],
        Scripts = ["scripts/setup.sh"],
        Fonts = [new FontItem { Name = "FiraCode", Url = "https://x/font.zip" }],
        Snaps = [new SnapItem { Name = "code" }],
    };

    [Fact]
    public void GetTotalItemCount_WithNull_ReturnsZero()
    {
        InstallerProgressHelper.GetTotalItemCount(null).Should().Be(0);
    }

    [Fact]
    public void GetTotalItemCount_SumsAllSources()
    {
        // 1 github + 2 apt + 1 aptRepo + 1 script + 1 font + 1 snap = 7.
        InstallerProgressHelper.GetTotalItemCount(FullBlock()).Should().Be(7);
    }

    [Fact]
    public void GetInstallerItems_ReturnsAllSourcesWithCounts()
    {
        var plans = InstallerProgressHelper.GetInstallerItems(FullBlock());

        plans.Should().HaveCount(6);
        plans.Sum(p => p.Count).Should().Be(7);
        plans.Should().Contain(p => p.Name == "GitHub releases" && p.Count == 1);
        plans.Should().Contain(p => p.Name == "APT packages" && p.Count == 2);
    }

    [Theory]
    [InlineData(InstallSourceType.GithubRelease, "GitHub Releases")]
    [InlineData(InstallSourceType.AptPackage, "APT Packages")]
    [InlineData(InstallSourceType.AptRepo, "APT Repositories")]
    [InlineData(InstallSourceType.Script, "Shell Scripts")]
    [InlineData(InstallSourceType.Font, "Fonts")]
    [InlineData(InstallSourceType.SnapPackage, "Snap Packages")]
    public void GetSourceTypeName_ReturnsStableName(InstallSourceType type, string expected)
    {
        InstallerProgressHelper.GetSourceTypeName(type).Should().Be(expected);
    }

    [Fact]
    public void GetSourceTypeOrder_OrdersSourcesDeterministically()
    {
        InstallerProgressHelper.GetSourceTypeOrder(InstallSourceType.GithubRelease)
            .Should().BeLessThan(InstallerProgressHelper.GetSourceTypeOrder(InstallSourceType.AptPackage));
        InstallerProgressHelper.GetSourceTypeOrder(InstallSourceType.AptPackage)
            .Should().BeLessThan(InstallerProgressHelper.GetSourceTypeOrder(InstallSourceType.SnapPackage));
    }

    [Fact]
    public void GetAllItemNames_ReturnsNamesInProcessingOrder()
    {
        var names = InstallerProgressHelper.GetAllItemNames(FullBlock()).ToList();

        names.Should().Equal("rg", "git", "curl", "docker", "scripts/setup.sh", "FiraCode", "code");
    }

    [Fact]
    public void GetAllItemNames_UsesBinaryThenRepoForGithub()
    {
        var withBinary = new InstallBlock { Github = [new GithubReleaseItem { Repo = "o/r", Asset = "a", Binary = "mybin" }] };
        InstallerProgressHelper.GetAllItemNames(withBinary).Should().ContainSingle().Which.Should().Be("mybin");

        var withoutBinary = new InstallBlock { Github = [new GithubReleaseItem { Repo = "o/r", Asset = "a" }] };
        InstallerProgressHelper.GetAllItemNames(withoutBinary).Should().ContainSingle().Which.Should().Be("o/r");
    }
}
