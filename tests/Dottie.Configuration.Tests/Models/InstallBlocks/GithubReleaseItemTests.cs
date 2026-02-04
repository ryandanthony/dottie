// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Models.InstallBlocks;

/// <summary>
/// Tests for <see cref="GithubReleaseItem"/>.
/// </summary>
public class GithubReleaseItemTests
{
    [Fact]
    public void Constructor_WithRequiredProperties_CreatesInstance()
    {
        // Act
        var item = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
        };

        // Assert
        item.Repo.Should().Be("junegunn/fzf");
        item.Asset.Should().Be("fzf-*-linux_amd64.tar.gz");
        item.Binary.Should().Be("fzf");
        item.Version.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithVersion_SetsVersionProperty()
    {
        // Act
        var item = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
            Version = "0.45.0",
        };

        // Assert
        item.Version.Should().Be("0.45.0");
    }

    [Fact]
    public void MergeKey_UsesRepoAsKey()
    {
        // Arrange
        var item = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
        };

        // Act - Use reflection to access internal property
        var mergeKeyProperty = typeof(GithubReleaseItem).GetProperty("MergeKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mergeKey = mergeKeyProperty?.GetValue(item) as string;

        // Assert
        mergeKey.Should().Be("junegunn/fzf");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var item1 = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
            Version = "0.45.0",
        };

        var item2 = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
            Version = "0.45.0",
        };

        // Assert - Record types have value-based equality
        item1.Should().Be(item2);
    }

    [Fact]
    public void Equality_DifferentRepo_AreNotEqual()
    {
        // Arrange
        var item1 = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
        };

        var item2 = new GithubReleaseItem
        {
            Repo = "sharkdp/fd",
            Asset = "fd-*-linux_amd64.tar.gz",
            Binary = "fd",
        };

        // Assert
        item1.Should().NotBe(item2);
    }

    [Fact]
    public void Equality_DifferentVersion_AreNotEqual()
    {
        // Arrange
        var item1 = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
            Version = "0.45.0",
        };

        var item2 = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
            Version = "0.46.0",
        };

        // Assert
        item1.Should().NotBe(item2);
    }

    [Fact]
    public void With_Expression_CreatesNewInstance()
    {
        // Arrange
        var original = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
        };

        // Act - Use with expression to create modified copy
        var modified = original with { Version = "0.45.0" };

        // Assert
        modified.Version.Should().Be("0.45.0");
        modified.Repo.Should().Be(original.Repo);
        modified.Asset.Should().Be(original.Asset);
        modified.Binary.Should().Be(original.Binary);
        original.Version.Should().BeNull();
    }

    [Fact]
    public void ToString_IncludesRepoName()
    {
        // Arrange
        var item = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
        };

        // Act
        var result = item.ToString();

        // Assert
        result.Should().Contain("junegunn/fzf");
    }
}
