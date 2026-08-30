// -----------------------------------------------------------------------
// <copyright file="SudoPrimerTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Commands;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using FluentAssertions;
using Spectre.Console.Testing;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="SudoPrimer"/>.
/// </summary>
public sealed class SudoPrimerTests
{
    private static InstallBlock AptBlock() => new() { Apt = ["git"] };

    private static InstallBlock GithubOnlyBlock() =>
        new() { Github = [new GithubReleaseItem { Repo = "o/r", Asset = "a", Binary = "b" }] };

    [Fact]
    public void ShouldPrompt_WithNoPrivilegedItems_ReturnsFalse()
    {
        var primer = new SudoPrimer(new FakeSudoChecker(available: true, cached: false));

        primer.ShouldPrompt(GithubOnlyBlock()).Should().BeFalse();
    }

    [Fact]
    public void ShouldPrompt_WhenSudoUnavailable_ReturnsFalse()
    {
        var primer = new SudoPrimer(new FakeSudoChecker(available: false, cached: false));

        primer.ShouldPrompt(AptBlock()).Should().BeFalse();
    }

    [Fact]
    public void ShouldPrompt_WhenCredentialsCached_ReturnsFalse()
    {
        var primer = new SudoPrimer(new FakeSudoChecker(available: true, cached: true));

        primer.ShouldPrompt(AptBlock()).Should().BeFalse();
    }

    [Fact]
    public void ShouldPrompt_WhenPrivilegedAndUncached_ReturnsTrue()
    {
        var primer = new SudoPrimer(new FakeSudoChecker(available: true, cached: false));

        primer.ShouldPrompt(AptBlock()).Should().BeTrue();
    }

    [Fact]
    public void PrimeIfNeeded_WhenNotNeeded_WritesNothing()
    {
        var console = new TestConsole();
        var primer = new SudoPrimer(new FakeSudoChecker(available: true, cached: true), console);

        primer.PrimeIfNeeded(AptBlock());

        console.Output.Should().BeEmpty();
    }

    [Fact]
    public void PrimeIfNeeded_WhenPrimingSucceeds_ShowsGuidanceWithoutWarning()
    {
        var console = new TestConsole();
        var primer = new SudoPrimer(new FakeSudoChecker(available: true, cached: false, primeResult: true), console);

        primer.PrimeIfNeeded(AptBlock());

        console.Output.Should().Contain("require sudo");
        console.Output.Should().NotContain("Could not cache");
    }

    [Fact]
    public void PrimeIfNeeded_WhenPrimingFails_ShowsWarning()
    {
        var console = new TestConsole();
        var primer = new SudoPrimer(new FakeSudoChecker(available: true, cached: false, primeResult: false), console);

        primer.PrimeIfNeeded(AptBlock());

        console.Output.Should().Contain("Could not cache");
    }

    /// <summary>
    /// Test double for <see cref="SudoChecker"/> that returns canned answers.
    /// </summary>
    private sealed class FakeSudoChecker(bool available, bool cached, bool primeResult = true) : SudoChecker
    {
        public override bool IsSudoAvailable() => available;

        public override bool HasCachedCredentials() => cached;

        public override bool PrimeInteractive() => primeResult;
    }
}
