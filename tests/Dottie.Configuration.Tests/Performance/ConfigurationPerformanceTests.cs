// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using Dottie.Configuration.Parsing;
using Dottie.Configuration.Validation;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Performance;

/// <summary>
/// Performance tests for configuration loading (SC-004).
/// </summary>
public sealed class ConfigurationPerformanceTests
{
    private static readonly string FixturesPath = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "Fixtures");

    [Fact]
    public void Load_50EntryConfig_CompletesWithin2Seconds()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-performance-50entries.yaml");
        var loader = new ConfigurationLoader();
        var validator = new ConfigurationValidator();

        // Act
        var stopwatch = Stopwatch.StartNew();

        var loadResult = loader.Load(configPath);
        var errorMessage = loadResult.Errors.Count > 0 ? loadResult.Errors[0].Message : "unknown error";
        loadResult.IsSuccess.Should().BeTrue(because: $"config should load successfully, but got: {errorMessage}");

        _ = validator.Validate(loadResult.Configuration!);

        stopwatch.Stop();

        // Assert
        stopwatch.Elapsed.Should().BeLessThan(
            TimeSpan.FromSeconds(2),
            because: "SC-004 requires 50-entry config loads in < 2 seconds");

        // Verify the config was actually parsed correctly
        loadResult.Configuration!.Profiles.Should().ContainKey("default");
        loadResult.Configuration.Profiles["default"].Dotfiles.Should().HaveCount(50);
    }

    [Fact]
    public void Load_50EntryConfig_MultipleIterations_AverageUnder500ms()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-performance-50entries.yaml");
        var loader = new ConfigurationLoader();
        var iterations = 10;
        var totalElapsed = TimeSpan.Zero;

        // Warm up
        _ = loader.Load(configPath);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            _ = loader.Load(configPath);
            stopwatch.Stop();
            totalElapsed += stopwatch.Elapsed;
        }

        var averageMs = totalElapsed.TotalMilliseconds / iterations;

        // Assert
        averageMs.Should().BeLessThan(
            500,
            because: $"average load time should be well under 2 seconds (actual: {averageMs:F2}ms)");
    }
}
