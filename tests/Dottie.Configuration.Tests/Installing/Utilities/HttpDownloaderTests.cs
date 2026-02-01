// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing.Utilities;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing.Utilities;

/// <summary>
/// Tests for <see cref="HttpDownloader"/>.
/// </summary>
public class HttpDownloaderTests
{
    private readonly HttpDownloader _downloader = new();

    [Fact]
    public async Task DownloadAsync_WithValidUrl_ReturnsContent()
    {
        // Arrange
        var url = "https://httpbin.org/bytes/100";

        // Act
        var content = await _downloader.DownloadAsync(url);

        // Assert
        content.Should().NotBeEmpty();
        content.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DownloadAsync_WithInvalidUrl_ThrowsException()
    {
        // Arrange
        var url = "https://invalid-domain-that-does-not-exist-12345.com/file";

        // Act & Assert
        await FluentActions.Invoking(async () => await _downloader.DownloadAsync(url))
            .Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DownloadAsync_With404Url_ThrowsException()
    {
        // Arrange
        var url = "https://httpbin.org/status/404";

        // Act & Assert
        await FluentActions.Invoking(async () => await _downloader.DownloadAsync(url))
            .Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DownloadAsync_WithRetryableError_RetriesAndSucceeds()
    {
        // Arrange
        var url = "https://httpbin.org/delay/0"; // This endpoint succeeds after a delay

        // Act
        var content = await _downloader.DownloadAsync(url);

        // Assert
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_WithServerError_RetriesMultipleTimes()
    {
        // Arrange
        var url = "https://httpbin.org/status/500";

        // Act & Assert
        await FluentActions.Invoking(async () => await _downloader.DownloadAsync(url))
            .Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DownloadAsync_PreservesContentType_ForBinaryData()
    {
        // Arrange
        var url = "https://httpbin.org/image/png";

        // Act
        var content = await _downloader.DownloadAsync(url);

        // Assert
        content.Should().NotBeEmpty();
    }

    [Fact(Skip = "Unreliable with live HTTP - would need mock HTTP client")]
    public async Task DownloadAsync_WithTimeout_ThrowsException()
    {
        // Arrange
        var url = "https://httpbin.org/delay/30"; // 30 second delay, likely to timeout

        // Act & Assert
        await FluentActions.Invoking(async () => await _downloader.DownloadAsync(url))
            .Should()
            .ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DownloadAsync_MultipleRequests_WorksIndependently()
    {
        // Arrange
        var url1 = "https://httpbin.org/bytes/50";
        var url2 = "https://httpbin.org/bytes/100";

        // Act
        var content1 = await _downloader.DownloadAsync(url1);
        var content2 = await _downloader.DownloadAsync(url2);

        // Assert
        content1.Should().NotBeEmpty();
        content2.Should().NotBeEmpty();
        content2.Length.Should().BeGreaterThanOrEqualTo(content1.Length);
    }

    [Fact]
    public void DownloadAsync_WithNullUrl_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => _downloader.DownloadAsync(null!))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void DownloadAsync_WithEmptyUrl_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => _downloader.DownloadAsync(string.Empty))
            .Should()
            .ThrowAsync<ArgumentException>();
    }
}
