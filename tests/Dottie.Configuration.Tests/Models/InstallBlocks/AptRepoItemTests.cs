// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Models.InstallBlocks;

/// <summary>
/// Tests for <see cref="AptRepoItem"/>.
/// </summary>
public class AptRepoItemTests
{
    [Fact]
    public void AptRepoItem_CanBeCreated()
    {
        // Arrange & Act
        var item = new AptRepoItem
        {
            Name = "docker",
            KeyUrl = "https://download.docker.com/linux/ubuntu/gpg",
            Repo = "deb [arch=amd64] https://download.docker.com/linux/ubuntu jammy stable",
            Packages = new List<string> { "docker-ce", "docker-ce-cli" },
        };

        // Assert
        item.Should().NotBeNull();
        item.Name.Should().Be("docker");
        item.KeyUrl.Should().Be("https://download.docker.com/linux/ubuntu/gpg");
        item.Repo.Should().Be("deb [arch=amd64] https://download.docker.com/linux/ubuntu jammy stable");
        item.Packages.Should().HaveCount(2);
    }

    [Fact]
    public void MergeKey_ReturnsName()
    {
        // Arrange
        var item = new AptRepoItem
        {
            Name = "test-repo",
            KeyUrl = "https://example.com/key.gpg",
            Repo = "deb https://example.com stable main",
            Packages = new List<string> { "pkg1" },
        };

        // Act
        var mergeKey = GetMergeKey(item);

        // Assert
        mergeKey.Should().Be("test-repo");
    }

    // Helper method to access internal MergeKey property
    private static string GetMergeKey(AptRepoItem item)
    {
        var property = typeof(AptRepoItem).GetProperty(
            "MergeKey",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (string)property!.GetValue(item)!;
    }
}
