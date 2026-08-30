// -----------------------------------------------------------------------
// <copyright file="RateLimitRetryPolicy.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using Flurl.Http;

namespace Dottie.Configuration.Installing.Utilities;

/// <summary>
/// Reusable retry policy for HTTP 429 (Too Many Requests) responses.
/// <para>
/// The wait between attempts is driven by whatever the server returns, in
/// priority order:
/// <list type="number">
/// <item><description><c>Retry-After</c> — either delta-seconds or an HTTP-date.</description></item>
/// <item><description><c>x-ratelimit-reset</c> — a Unix epoch (seconds) when the
///   quota resets, honoured when <c>x-ratelimit-remaining</c> is <c>0</c> (the
///   shape GitHub returns for primary rate limits).</description></item>
/// <item><description>An exponential fallback (<c>base * 2^(attempt-1)</c>) when the
///   response carries no usable timing hint.</description></item>
/// </list>
/// </para>
/// <para>
/// Designed to be shared across every caller that talks to a rate-limited API
/// (e.g. the GitHub release metadata and HEAD probe calls). The send delegate
/// must allow non-success statuses to return rather than throw (Flurl's
/// <see cref="AllowAnyHttpStatusExtensions"/>) so the policy can read the
/// 429 headers.
/// </para>
/// </summary>
public sealed class RateLimitRetryPolicy
{
    private const int DefaultMaxAttemptsValue = 4;
    private const int HttpTooManyRequests = 429;

    private static readonly TimeSpan DefaultFallbackBaseDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DefaultMaxDelay = TimeSpan.FromSeconds(60);

    private readonly int _maxAttempts;
    private readonly TimeSpan _fallbackBaseDelay;
    private readonly TimeSpan _maxDelay;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitRetryPolicy"/> class.
    /// </summary>
    /// <param name="maxAttempts">Total attempts (initial call + retries). Minimum 1.</param>
    /// <param name="fallbackBaseDelay">Base delay for the exponential fallback when no header hint is present.</param>
    /// <param name="maxDelay">Upper bound applied to any computed delay, so a hostile or stale header can't stall the run indefinitely.</param>
    /// <param name="delay">Delay function (seam for tests); defaults to <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</param>
    /// <param name="timeProvider">Clock (seam for tests); defaults to <see cref="TimeProvider.System"/>.</param>
    public RateLimitRetryPolicy(
        int maxAttempts = DefaultMaxAttemptsValue,
        TimeSpan? fallbackBaseDelay = null,
        TimeSpan? maxDelay = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null,
        TimeProvider? timeProvider = null)
    {
        _maxAttempts = Math.Max(1, maxAttempts);
        _fallbackBaseDelay = fallbackBaseDelay ?? DefaultFallbackBaseDelay;
        _maxDelay = maxDelay ?? DefaultMaxDelay;
        _delay = delay ?? ((d, ct) => Task.Delay(d, ct));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Gets the default number of attempts (1 initial + retries) before giving up.
    /// </summary>
    public static int DefaultMaxAttempts => DefaultMaxAttemptsValue;

    /// <summary>
    /// Executes <paramref name="send"/>, retrying on HTTP 429 with a header-driven
    /// backoff. The most recent response is always returned (even a final 429), so
    /// callers keep their existing success/failure handling.
    /// </summary>
    /// <param name="send">Sends the request and returns the response. Must not throw on non-success status (use <c>AllowAnyHttpStatus()</c>).</param>
    /// <param name="cancellationToken">Cancellation token; observed during both the request and the backoff wait.</param>
    /// <returns>The final <see cref="IFlurlResponse"/>.</returns>
    public async Task<IFlurlResponse> ExecuteAsync(
        Func<CancellationToken, Task<IFlurlResponse>> send,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(send);

        var attempt = 1;
        var response = await send(cancellationToken).ConfigureAwait(false);

        while (response.StatusCode == HttpTooManyRequests && attempt < _maxAttempts)
        {
            var wait = ComputeDelay(response, attempt);
            await _delay(wait, cancellationToken).ConfigureAwait(false);
            attempt++;
            response = await send(cancellationToken).ConfigureAwait(false);
        }

        return response;
    }

    /// <summary>
    /// Determines how long to wait before the next attempt, from the response
    /// headers, falling back to exponential backoff.
    /// </summary>
    /// <param name="response">The 429 response.</param>
    /// <param name="attempt">1-based attempt number that just failed.</param>
    /// <returns>The clamped delay to wait.</returns>
    private TimeSpan ComputeDelay(IFlurlResponse response, int attempt)
    {
        if (TryGetRetryAfterDelay(response, out var retryAfter))
        {
            return Clamp(retryAfter);
        }

        if (TryGetRateLimitResetDelay(response, out var resetDelay))
        {
            return Clamp(resetDelay);
        }

        // Exponential fallback: base * 2^(attempt-1).
        var backoffMs = _fallbackBaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        return Clamp(TimeSpan.FromMilliseconds(backoffMs));
    }

    /// <summary>
    /// Parses the <c>Retry-After</c> header, which may be an integer number of
    /// seconds or an HTTP-date.
    /// </summary>
    /// <param name="response">The response to inspect.</param>
    /// <param name="delay">The resulting delay when present.</param>
    /// <returns><see langword="true"/> if a usable value was found.</returns>
    private bool TryGetRetryAfterDelay(IFlurlResponse response, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;

        if (!TryGetHeader(response, "Retry-After", out var value))
        {
            return false;
        }

        // Delta-seconds form, e.g. "Retry-After: 120".
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            delay = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return true;
        }

        // HTTP-date form, e.g. "Retry-After: Wed, 21 Oct 2026 07:28:00 GMT".
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var when))
        {
            delay = when - _timeProvider.GetUtcNow();
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses the GitHub-style <c>x-ratelimit-reset</c> epoch, used only when
    /// <c>x-ratelimit-remaining</c> is <c>0</c> (i.e. the limit is actually
    /// exhausted, not merely reported).
    /// </summary>
    /// <param name="response">The response to inspect.</param>
    /// <param name="delay">The resulting delay when present.</param>
    /// <returns><see langword="true"/> if a usable value was found.</returns>
    private bool TryGetRateLimitResetDelay(IFlurlResponse response, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;

        if (TryGetHeader(response, "x-ratelimit-remaining", out var remainingRaw)
            && int.TryParse(remainingRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var remaining)
            && remaining > 0)
        {
            // Quota not exhausted; this header pair doesn't explain the 429.
            return false;
        }

        if (!TryGetHeader(response, "x-ratelimit-reset", out var resetRaw)
            || !long.TryParse(resetRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var resetEpoch))
        {
            return false;
        }

        var resetAt = DateTimeOffset.FromUnixTimeSeconds(resetEpoch);
        delay = resetAt - _timeProvider.GetUtcNow();
        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        return true;
    }

    /// <summary>
    /// Case-insensitively reads the first value of a header.
    /// </summary>
    /// <param name="response">The response to inspect.</param>
    /// <param name="name">Header name.</param>
    /// <param name="value">First value when present.</param>
    /// <returns><see langword="true"/> if the header exists and is non-empty.</returns>
    private static bool TryGetHeader(IFlurlResponse response, string name, out string value)
    {
        if (response.Headers.TryGetFirst(name, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            value = raw.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Bounds a delay to the non-negative <c>[0, maxDelay]</c> range.
    /// </summary>
    /// <param name="delay">The proposed delay.</param>
    /// <returns>The clamped delay.</returns>
    private TimeSpan Clamp(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return delay > _maxDelay ? _maxDelay : delay;
    }
}
