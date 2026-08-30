// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing.Utilities;
using FluentAssertions;
using Flurl.Http;
using Flurl.Http.Testing;

namespace Dottie.Configuration.Tests.Installing.Utilities;

/// <summary>
/// Tests for <see cref="RateLimitRetryPolicy"/>.
/// </summary>
public class RateLimitRetryPolicyTests : IDisposable
{
    private const int HttpTooManyRequests = 429;
    private const int HttpOk = 200;

    private readonly HttpTest _httpTest = new();
    private readonly List<TimeSpan> _recordedDelays = [];

    public void Dispose()
    {
        _httpTest.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// A non-429 response is returned immediately with no retry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_WhenFirstResponseSucceeds_DoesNotRetryAsync()
    {
        _httpTest.RespondWith("ok", HttpOk);
        var policy = CreatePolicy();

        var response = await policy.ExecuteAsync(SendAsync);

        response.StatusCode.Should().Be(HttpOk);
        _recordedDelays.Should().BeEmpty();
        _httpTest.ShouldHaveCalled("*").Times(1);
    }

    /// <summary>
    /// A 429 followed by a success retries once and returns the success.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_When429ThenSuccess_RetriesAndReturnsSuccessAsync()
    {
        _httpTest.RespondWith("slow down", HttpTooManyRequests, new { retry_after = "2" });
        _httpTest.RespondWith("ok", HttpOk);
        var policy = CreatePolicy();

        var response = await policy.ExecuteAsync(SendAsync);

        response.StatusCode.Should().Be(HttpOk);
        _recordedDelays.Should().ContainSingle();
        _httpTest.ShouldHaveCalled("*").Times(2);
    }

    /// <summary>
    /// The Retry-After header in delta-seconds form drives the wait duration.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithRetryAfterSeconds_UsesHeaderDelayAsync()
    {
        _httpTest.RespondWith("slow down", HttpTooManyRequests, new Dictionary<string, string> { ["Retry-After"] = "7" });
        _httpTest.RespondWith("ok", HttpOk);
        var policy = CreatePolicy();

        await policy.ExecuteAsync(SendAsync);

        _recordedDelays.Should().ContainSingle()
            .Which.Should().Be(TimeSpan.FromSeconds(7));
    }

    /// <summary>
    /// The Retry-After header in HTTP-date form is converted to a delay relative
    /// to the (fixed) clock.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithRetryAfterHttpDate_UsesDeltaFromClockAsync()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var retryAt = now.AddSeconds(30);
        _httpTest.RespondWith(
            "slow down",
            HttpTooManyRequests,
            new Dictionary<string, string> { ["Retry-After"] = retryAt.ToString("R") });
        _httpTest.RespondWith("ok", HttpOk);
        var policy = CreatePolicy(timeProvider: new FixedTimeProvider(now));

        await policy.ExecuteAsync(SendAsync);

        _recordedDelays.Should().ContainSingle()
            .Which.Should().BeCloseTo(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// When Retry-After is absent, an exhausted x-ratelimit-remaining=0 pair with
    /// an x-ratelimit-reset epoch drives the wait.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithRateLimitResetAndZeroRemaining_UsesResetEpochAsync()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var resetAt = now.AddSeconds(45);
        _httpTest.RespondWith(
            "rate limited",
            HttpTooManyRequests,
            new Dictionary<string, string>
            {
                ["x-ratelimit-remaining"] = "0",
                ["x-ratelimit-reset"] = resetAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
            });
        _httpTest.RespondWith("ok", HttpOk);
        var policy = CreatePolicy(timeProvider: new FixedTimeProvider(now));

        await policy.ExecuteAsync(SendAsync);

        _recordedDelays.Should().ContainSingle()
            .Which.Should().BeCloseTo(TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// x-ratelimit-reset is ignored while remaining quota is greater than zero,
    /// so the exponential fallback applies instead.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithRateLimitResetButRemainingPositive_UsesFallbackAsync()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _httpTest.RespondWith(
            "rate limited",
            HttpTooManyRequests,
            new Dictionary<string, string>
            {
                ["x-ratelimit-remaining"] = "5",
                ["x-ratelimit-reset"] = now.AddSeconds(999).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
            });
        _httpTest.RespondWith("ok", HttpOk);
        var policy = CreatePolicy(fallbackBaseDelay: TimeSpan.FromSeconds(1), timeProvider: new FixedTimeProvider(now));

        await policy.ExecuteAsync(SendAsync);

        // First retry fallback: base * 2^(1-1) = 1s (not the 999s reset).
        _recordedDelays.Should().ContainSingle()
            .Which.Should().Be(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// With no timing headers, the delay grows exponentially per attempt.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithNoHeaders_UsesExponentialBackoffAsync()
    {
        // Four 429s: with maxAttempts=4 that is 3 retries (delays), then a final 429 returned.
        for (var i = 0; i < 4; i++)
        {
            _httpTest.RespondWith("rate limited", HttpTooManyRequests);
        }

        var policy = CreatePolicy(maxAttempts: 4, fallbackBaseDelay: TimeSpan.FromSeconds(1));

        var response = await policy.ExecuteAsync(SendAsync);

        response.StatusCode.Should().Be(HttpTooManyRequests);
        _recordedDelays.Should().Equal(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4));
        _httpTest.ShouldHaveCalled("*").Times(4);
    }

    /// <summary>
    /// Computed delays never exceed the configured maximum.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithHugeRetryAfter_ClampsToMaxDelayAsync()
    {
        _httpTest.RespondWith("slow down", HttpTooManyRequests, new Dictionary<string, string> { ["Retry-After"] = "100000" });
        _httpTest.RespondWith("ok", HttpOk);
        var policy = CreatePolicy(maxDelay: TimeSpan.FromSeconds(30));

        await policy.ExecuteAsync(SendAsync);

        _recordedDelays.Should().ContainSingle()
            .Which.Should().Be(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Retries stop once the attempt budget is exhausted, returning the last 429.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_WhenAttemptsExhausted_ReturnsLast429Async()
    {
        _httpTest.RespondWith("rate limited", HttpTooManyRequests);
        _httpTest.RespondWith("rate limited", HttpTooManyRequests);
        var policy = CreatePolicy(maxAttempts: 2);

        var response = await policy.ExecuteAsync(SendAsync);

        response.StatusCode.Should().Be(HttpTooManyRequests);
        _recordedDelays.Should().ContainSingle();
        _httpTest.ShouldHaveCalled("*").Times(2);
    }

    /// <summary>
    /// A null send delegate is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task ExecuteAsync_WithNullSend_ThrowsAsync()
    {
        var policy = CreatePolicy();

        await FluentActions.Invoking(() => policy.ExecuteAsync(null!))
            .Should()
            .ThrowAsync<ArgumentNullException>();
    }

    private static Task<IFlurlResponse> SendAsync(CancellationToken cancellationToken)
        => "https://api.github.com/test".AllowAnyHttpStatus().SendAsync(HttpMethod.Get, cancellationToken: cancellationToken);

    private RateLimitRetryPolicy CreatePolicy(
        int maxAttempts = 4,
        TimeSpan? fallbackBaseDelay = null,
        TimeSpan? maxDelay = null,
        TimeProvider? timeProvider = null)
    {
        return new RateLimitRetryPolicy(
            maxAttempts,
            fallbackBaseDelay ?? TimeSpan.FromSeconds(1),
            maxDelay ?? TimeSpan.FromSeconds(60),
            delay: (d, _) =>
            {
                _recordedDelays.Add(d);
                return Task.CompletedTask;
            },
            timeProvider: timeProvider);
    }

    /// <summary>
    /// Minimal fixed-clock <see cref="TimeProvider"/> for deterministic date math.
    /// </summary>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
