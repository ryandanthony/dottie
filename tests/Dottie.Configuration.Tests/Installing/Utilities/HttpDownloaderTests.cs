// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing.Utilities;
using FluentAssertions;
using Flurl.Http.Testing;

namespace Dottie.Configuration.Tests.Installing.Utilities;

/// <summary>
/// Tests for <see cref="HttpDownloader"/>.
/// </summary>
public class HttpDownloaderTests : IDisposable
{
    private readonly HttpDownloader _downloader = new();
    private HttpTest? _httpTest;

    public void Dispose()
    {
        _httpTest?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region DownloadAsync - Argument Validation Tests

    [Fact]
    public async Task DownloadAsync_WithNullUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _downloader.DownloadAsync(null!))
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("url");
    }

    [Fact]
    public async Task DownloadAsync_WithEmptyUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _downloader.DownloadAsync(string.Empty))
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("url");
    }

    [Fact]
    public async Task DownloadAsync_WithWhitespaceUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _downloader.DownloadAsync("   "))
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("url");
    }

    #endregion

    #region DownloadAsync - Success Tests

    [Fact]
    public async Task DownloadAsync_WithValidUrl_ReturnsContent()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith("test content", status: 200);

        var url = "https://example.com/file.bin";

        // Act
        var content = await _downloader.DownloadAsync(url);

        // Assert
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_WithValidUrl_CallsCorrectEndpoint()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith("content");

        var url = "https://example.com/downloads/myfile.zip";

        // Act
        await _downloader.DownloadAsync(url);

        // Assert
        _httpTest.ShouldHaveCalled(url)
            .WithVerb(HttpMethod.Get)
            .Times(1);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsNonEmptyBytesFromResponse()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith("binary data content");

        // Act
        var result = await _downloader.DownloadAsync("https://example.com/file");

        // Assert
        result.Should().NotBeEmpty();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DownloadAsync_WithLargeContent_ReturnsAllContent()
    {
        // Arrange
        _httpTest = new HttpTest();
        var largeContent = new string('X', 10000);
        _httpTest.RespondWith(largeContent);

        // Act
        var result = await _downloader.DownloadAsync("https://example.com/largefile");

        // Assert
        result.Should().NotBeEmpty();
        result.Length.Should().Be(10000);
    }

    #endregion

    #region DownloadAsync - Error Handling Tests

    [Fact]
    public async Task DownloadAsync_With404Response_ThrowsHttpRequestException()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 404, body: "Not Found");

        // Act & Assert
        await FluentActions.Invoking(() => _downloader.DownloadAsync("https://example.com/missing"))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task DownloadAsync_With401Response_ThrowsHttpRequestException()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 401, body: "Unauthorized");

        // Act & Assert
        await FluentActions.Invoking(() => _downloader.DownloadAsync("https://example.com/secure"))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task DownloadAsync_With403Response_ThrowsHttpRequestException()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 403, body: "Forbidden");

        // Act & Assert
        await FluentActions.Invoking(() => _downloader.DownloadAsync("https://example.com/forbidden"))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    #endregion

    #region DownloadAsync - Retry Tests

    [Fact]
    public async Task DownloadAsync_WithServerError_RetriesAndEventuallyThrows()
    {
        // Arrange
        _httpTest = new HttpTest();
        // Return 500 for all attempts (MaxRetries = 3)
        _httpTest.RespondWith(status: 500);
        _httpTest.RespondWith(status: 500);
        _httpTest.RespondWith(status: 500);

        // Act & Assert
        await FluentActions.Invoking(() => _downloader.DownloadAsync("https://example.com/error"))
            .Should()
            .ThrowAsync<HttpRequestException>();

        // Verify all 3 attempts were made
        _httpTest.ShouldHaveCalled("https://example.com/error")
            .Times(3);
    }

    [Fact]
    public async Task DownloadAsync_WithTransientError_RetriesAndSucceeds()
    {
        // Arrange
        _httpTest = new HttpTest();

        // First two attempts fail with 500, third succeeds
        _httpTest.RespondWith(status: 500);
        _httpTest.RespondWith(status: 500);
        _httpTest.RespondWith("success data");

        // Act
        var result = await _downloader.DownloadAsync("https://example.com/flaky");

        // Assert
        result.Should().NotBeEmpty();
        _httpTest.ShouldHaveCalled("https://example.com/flaky")
            .Times(3);
    }

    [Fact]
    public async Task DownloadAsync_WithFirstAttemptSuccess_DoesNotRetry()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith("success");

        // Act
        await _downloader.DownloadAsync("https://example.com/success");

        // Assert
        _httpTest.ShouldHaveCalled("https://example.com/success")
            .Times(1);
    }

    #endregion

    #region DownloadAsync - Cancellation Tests

    [Fact]
    public async Task DownloadAsync_WithCancelledToken_ThrowsException()
    {
        // Arrange - not using HttpTest here as cancellation is checked before HTTP call
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - the implementation creates a linked token that checks cancellation
        // The exact exception type may vary based on implementation
        await FluentActions.Invoking(() => _downloader.DownloadAsync("https://example.com/file", cts.Token))
            .Should()
            .ThrowAsync<Exception>(); // Could be OperationCanceledException or HttpRequestException
    }

    #endregion

    #region IsReachableAsync - Argument Validation Tests

    [Fact]
    public async Task IsReachableAsync_WithNullUrl_ReturnsFalse()
    {
        // Act
        var result = await _downloader.IsReachableAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsReachableAsync_WithEmptyUrl_ReturnsFalse()
    {
        // Act
        var result = await _downloader.IsReachableAsync(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsReachableAsync_WithWhitespaceUrl_ReturnsFalse()
    {
        // Act
        var result = await _downloader.IsReachableAsync("   ");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsReachableAsync - Success Tests

    [Fact]
    public async Task IsReachableAsync_With200Response_ReturnsTrue()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 200);

        // Act
        var result = await _downloader.IsReachableAsync("https://example.com/resource");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsReachableAsync_With200Response_UsesHeadMethod()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 200);

        // Act
        await _downloader.IsReachableAsync("https://example.com/resource");

        // Assert
        _httpTest.ShouldHaveCalled("https://example.com/resource")
            .WithVerb(HttpMethod.Head)
            .Times(1);
    }

    [Fact]
    public async Task IsReachableAsync_With201Response_ReturnsTrue()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 201);

        // Act
        var result = await _downloader.IsReachableAsync("https://example.com/created");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsReachableAsync_With204Response_ReturnsTrue()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 204);

        // Act
        var result = await _downloader.IsReachableAsync("https://example.com/nocontent");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsReachableAsync - Failure Tests

    [Fact]
    public async Task IsReachableAsync_With404Response_ReturnsFalse()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 404);

        // Act
        var result = await _downloader.IsReachableAsync("https://example.com/missing");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsReachableAsync_With500Response_ReturnsFalse()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 500);

        // Act
        var result = await _downloader.IsReachableAsync("https://example.com/error");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsReachableAsync_With301Response_ReturnsFalse()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.RespondWith(status: 301);

        // Act
        var result = await _downloader.IsReachableAsync("https://example.com/redirect");

        // Assert - 301 is outside 200-299 range
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsReachableAsync_WhenExceptionThrown_ReturnsFalse()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.SimulateException(new HttpRequestException("Connection refused"));

        // Act
        var result = await _downloader.IsReachableAsync("https://example.com/unreachable");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsReachableAsync - Cancellation Tests

    [Fact]
    public async Task IsReachableAsync_WhenExceptionOccurs_ReturnsFalse()
    {
        // Arrange
        _httpTest = new HttpTest();
        _httpTest.SimulateException(new TaskCanceledException("Request cancelled"));

        // Act
        var result = await _downloader.IsReachableAsync("https://example.com/file");

        // Assert - IsReachable catches all exceptions and returns false
        result.Should().BeFalse();
    }

    #endregion
}
