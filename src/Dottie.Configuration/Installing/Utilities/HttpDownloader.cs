// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Dottie.Configuration.Installing.Utilities;

/// <summary>
/// Downloads files from HTTP(S) URLs with retry logic.
/// </summary>
public class HttpDownloader
{
    private static readonly HttpClient _httpClient = new();
    private const int MaxRetries = 3;
    private const int TimeoutSeconds = 30;

    /// <summary>
    /// Downloads content from the specified URL with retry logic.
    /// </summary>
    /// <param name="url">The URL to download from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The downloaded content as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown when URL is null or empty.</exception>
    /// <exception cref="HttpRequestException">Thrown when the download fails after retries.</exception>
    public async Task<byte[]> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        int attempt = 0;
        while (attempt < MaxRetries)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, cts.Token);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(cts.Token);
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries - 1 && IsRetryable(ex))
            {
                attempt++;
                await Task.Delay(1000 * attempt, cts.Token); // Exponential backoff
                continue;
            }
            catch (TaskCanceledException)
            {
                throw new HttpRequestException($"Download timeout after {TimeoutSeconds} seconds for URL: {url}");
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Failed to download from {url}: {ex.Message}", ex);
            }
        }

        throw new HttpRequestException($"Failed to download from {url} after {MaxRetries} attempts.");
    }

    /// <summary>
    /// Checks if a URL is reachable (for dry-run validation).
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the URL is reachable; otherwise, false.</returns>
    public async Task<bool> IsReachableAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, url),
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsRetryable(HttpRequestException ex)
    {
        // Retry on transient errors (server errors, timeouts)
        return ex.InnerException is TimeoutException
            || (ex.StatusCode.HasValue && (int)ex.StatusCode >= 500);
    }
}
