// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Models.InstallBlocks;

/// <summary>
/// Tests for <see cref="FontItem"/>.
/// </summary>
public class FontItemTests
{
    [Fact]
    public void FontItem_CanBeCreated()
    {
        // Arrange & Act
        var item = new FontItem
        {
            Name = "JetBrains Mono",
            Url = "https://github.com/JetBrains/JetBrainsMono/releases/download/v2.304/JetBrainsMono-2.304.zip"
        };

        // Assert
        item.Should().NotBeNull();
        item.Name.Should().Be("JetBrains Mono");
        item.Url.Should().Be("https://github.com/JetBrains/JetBrainsMono/releases/download/v2.304/JetBrainsMono-2.304.zip");
    }

    [Fact]
    public void MergeKey_ReturnsName()
    {
        // Arrange
        var item = new FontItem
        {
            Name = "Ubuntu Font",
            Url = "https://example.com/ubuntu-font.zip"
        };

        // Act
        var mergeKey = GetMergeKey(item);

        // Assert
        mergeKey.Should().Be("Ubuntu Font");
    }

    // Helper method to access internal MergeKey property
    private static string GetMergeKey(FontItem item)
    {
        var property = typeof(FontItem).GetProperty("MergeKey", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return (string)property!.GetValue(item)!;
    }
}
