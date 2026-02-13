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
    public void MergeKey_UsesRepoAndBinaryAsKey()
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
        mergeKey.Should().Be("junegunn/fzf::fzf");
    }

    [Fact]
    public void MergeKey_FallsBackToAsset_WhenBinaryIsNull()
    {
        // Arrange
        var item = new GithubReleaseItem
        {
            Repo = "jgraph/drawio-desktop",
            Asset = "drawio-arm64-*.deb",
        };

        // Act
        var mergeKeyProperty = typeof(GithubReleaseItem).GetProperty("MergeKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mergeKey = mergeKeyProperty?.GetValue(item) as string;

        // Assert
        mergeKey.Should().Be("jgraph/drawio-desktop::drawio-arm64-*.deb");
    }

    [Fact]
    public void MergeKey_DifferentBinaries_SameRepo_ProduceDifferentKeys()
    {
        // Arrange — kubectx/kubens pattern
        var kubectx = new GithubReleaseItem
        {
            Repo = "ahmetb/kubectx",
            Asset = "kubectx_v0.9.5_linux_x86_64.tar.gz",
            Binary = "kubectx",
        };
        var kubens = new GithubReleaseItem
        {
            Repo = "ahmetb/kubectx",
            Asset = "kubens_v0.9.5_linux_x86_64.tar.gz",
            Binary = "kubens",
        };

        // Act
        var mergeKeyProperty = typeof(GithubReleaseItem).GetProperty("MergeKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var key1 = mergeKeyProperty?.GetValue(kubectx) as string;
        var key2 = mergeKeyProperty?.GetValue(kubens) as string;

        // Assert
        key1.Should().NotBe(key2);
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

    [Fact]
    public void Type_DefaultsToBinary_WhenNotSet()
    {
        // Arrange & Act
        var item = new GithubReleaseItem
        {
            Repo = "junegunn/fzf",
            Asset = "fzf-*-linux_amd64.tar.gz",
            Binary = "fzf",
        };

        // Assert
        item.Type.Should().Be(GithubReleaseAssetType.Binary);
    }

    [Fact]
    public void Type_CanBeSetToDeb()
    {
        // Arrange & Act
        var item = new GithubReleaseItem
        {
            Repo = "jgraph/drawio-desktop",
            Asset = "drawio-arm64-*.deb",
            Type = GithubReleaseAssetType.Deb,
        };

        // Assert
        item.Type.Should().Be(GithubReleaseAssetType.Deb);
    }

    [Fact]
    public void Equality_IncludesType()
    {
        // Arrange
        var item1 = new GithubReleaseItem
        {
            Repo = "jgraph/drawio-desktop",
            Asset = "drawio-arm64-*.deb",
            Type = GithubReleaseAssetType.Deb,
        };

        var item2 = new GithubReleaseItem
        {
            Repo = "jgraph/drawio-desktop",
            Asset = "drawio-arm64-*.deb",
            Type = GithubReleaseAssetType.Binary,
            Binary = "drawio",
        };

        // Assert — different Type means not equal
        item1.Should().NotBe(item2);
    }

    [Fact]
    public void Binary_IsOptional_ForDebType()
    {
        // Arrange & Act — should compile and work without Binary
        var item = new GithubReleaseItem
        {
            Repo = "jgraph/drawio-desktop",
            Asset = "drawio-arm64-*.deb",
            Type = GithubReleaseAssetType.Deb,
        };

        // Assert
        item.Binary.Should().BeNull();
        item.Type.Should().Be(GithubReleaseAssetType.Deb);
    }
}
