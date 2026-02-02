// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Models.InstallBlocks;

/// <summary>
/// Tests for <see cref="SnapItem"/>.
/// </summary>
public class SnapItemTests
{
    [Fact]
    public void SnapItem_CanBeCreated()
    {
        // Arrange & Act
        var item = new SnapItem
        {
            Name = "blender",
            Classic = false,
        };

        // Assert
        item.Should().NotBeNull();
        item.Name.Should().Be("blender");
        item.Classic.Should().BeFalse();
    }

    [Fact]
    public void SnapItem_WithClassic_SetsProperty()
    {
        // Arrange & Act
        var item = new SnapItem
        {
            Name = "code",
            Classic = true,
        };

        // Assert
        item.Classic.Should().BeTrue();
    }

    [Fact]
    public void MergeKey_ReturnsName()
    {
        // Arrange
        var item = new SnapItem
        {
            Name = "test-snap",
            Classic = false,
        };

        // Act
        var mergeKey = GetMergeKey(item);

        // Assert
        mergeKey.Should().Be("test-snap");
    }

    // Helper method to access internal MergeKey property
    private static string GetMergeKey(SnapItem item)
    {
        var property = typeof(SnapItem).GetProperty(
            "MergeKey",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (string)property!.GetValue(item)!;
    }
}
