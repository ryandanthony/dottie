// -----------------------------------------------------------------------
// <copyright file="LiveInstallRendererTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Commands;
using Dottie.Cli.Output;
using Dottie.Configuration.Installing;
using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Dottie.Cli.Tests.Output;

/// <summary>
/// Tests for <see cref="LiveInstallRenderer"/>. These drive the renderer through
/// a local interactive <see cref="TestConsole"/> (injected, not the global
/// <c>AnsiConsole.Console</c>) so the live session renders in isolation without
/// racing other tests, and assert on the captured output plus returned results.
/// </summary>
public sealed class LiveInstallRendererTests : IDisposable
{
    private readonly TestConsole _console;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveInstallRendererTests"/> class.
    /// </summary>
    public LiveInstallRendererTests()
    {
        _console = new TestConsole().Interactive();
        _console.Profile.Width = 100;
        _console.Profile.Height = 40;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _console.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The renderer returns exactly the results produced by the work delegate.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task RunAsync_ReturnsResultsFromWorkAsync()
    {
        var renderer = new LiveInstallRenderer(_console);
        var expected = new List<InstallResult>
        {
            InstallResult.Success("rg", InstallSourceType.GithubRelease),
            InstallResult.Failed("git", InstallSourceType.AptPackage, "boom"),
        };

        var actual = await renderer.RunAsync(observer =>
        {
            observer.OnStart(["GitHub releases"], [1], 1);
            observer.OnItemProgress("GitHub releases", 1, 1);
            observer.OnResults(expected);
            return Task.FromResult(expected);
        });

        actual.Should().BeEquivalentTo(expected);
    }

    /// <summary>
    /// The dashboard shows the progress header and each result line, in the same
    /// status format as the final summary.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task RunAsync_RendersProgressHeaderAndResultsAsync()
    {
        var renderer = new LiveInstallRenderer(_console);

        await renderer.RunAsync(observer =>
        {
            observer.OnStart(["GitHub releases", "Scripts"], [1, 1], 2);
            observer.OnItemProgress("GitHub releases", 1, 1);
            observer.OnResults([InstallResult.Success("rg", InstallSourceType.GithubRelease)]);
            observer.OnItemProgress("Scripts", 1, 2);
            observer.OnResults([InstallResult.Failed("setup.sh", InstallSourceType.Script, "exit 1")]);
            return Task.FromResult(new List<InstallResult>());
        });

        var output = _console.Output;
        output.Should().Contain("Installing");
        output.Should().Contain("Overall");
        output.Should().Contain("Results");
        output.Should().Contain("Installed");
        output.Should().Contain("rg");
        output.Should().Contain("Failed");
        output.Should().Contain("setup.sh");
    }

    /// <summary>
    /// An empty run (no plans, no results) still renders without throwing and
    /// returns an empty list.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task RunAsync_WithNoWork_RendersWaitingAndReturnsEmptyAsync()
    {
        var renderer = new LiveInstallRenderer(_console);

        var results = await renderer.RunAsync(observer =>
        {
            observer.OnStart([], [], 0);
            return Task.FromResult(new List<InstallResult>());
        });

        results.Should().BeEmpty();
        _console.Output.Should().Contain("Waiting for results...");
    }

    /// <summary>
    /// When more results arrive than the visible window, the header reports the
    /// total and the truncation, and the newest result stays visible.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task RunAsync_WithManyResults_ShowsTailWindowAsync()
    {
        var renderer = new LiveInstallRenderer(_console);
        const int count = 25;

        await renderer.RunAsync(observer =>
        {
            observer.OnStart(["APT packages"], [count], count);
            for (var i = 0; i < count; i++)
            {
                observer.OnItemProgress("APT packages", i + 1, i + 1);
                observer.OnResults([InstallResult.Success($"pkg-{i:D2}", InstallSourceType.AptPackage)]);
            }

            return Task.FromResult(new List<InstallResult>());
        });

        var output = _console.Output;
        // Header shows the running total and that older rows are hidden.
        output.Should().Contain($"{count} total");
        // The most recent item is within the visible tail window.
        output.Should().Contain("pkg-24");
    }

    /// <summary>
    /// A null work delegate is rejected.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task RunAsync_WithNullWork_ThrowsAsync()
    {
        var renderer = new LiveInstallRenderer(_console);

        await FluentActions.Invoking(() => renderer.RunAsync(null!))
            .Should()
            .ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Observer arguments are validated.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous test.</returns>
    [Fact]
    public async Task Observer_WithNullArguments_ThrowsAsync()
    {
        var renderer = new LiveInstallRenderer(_console);

        await renderer.RunAsync(observer =>
        {
            FluentActions.Invoking(() => observer.OnStart(null!, [], 0)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => observer.OnStart([], null!, 0)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => observer.OnItemProgress(null!, 0, 0)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => observer.OnResults(null!)).Should().Throw<ArgumentNullException>();
            return Task.FromResult(new List<InstallResult>());
        });
    }
}
