// -----------------------------------------------------------------------
// <copyright file="LinkCommandSettingsTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Commands;
using FluentAssertions;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="LinkCommandSettings"/>.
/// </summary>
public sealed class LinkCommandSettingsTests
{
    [Fact]
    public void Validate_WithDryRunOnly_ReturnsSuccess()
    {
        // Arrange
        var settings = new LinkCommandSettings
        {
            DryRun = true,
            Force = false,
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithForceOnly_ReturnsSuccess()
    {
        // Arrange
        var settings = new LinkCommandSettings
        {
            DryRun = false,
            Force = true,
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNeitherFlag_ReturnsSuccess()
    {
        // Arrange
        var settings = new LinkCommandSettings
        {
            DryRun = false,
            Force = false,
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithDryRunAndForce_ReturnsError()
    {
        // Arrange
        var settings = new LinkCommandSettings
        {
            DryRun = true,
            Force = true,
        };

        // Act
        var result = settings.Validate();

        // Assert
        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("--dry-run");
        result.Message.Should().Contain("--force");
        result.Message.Should().Contain("cannot be used together");
    }

    [Fact]
    public void DryRun_DefaultsToFalse()
    {
        // Arrange & Act
        var settings = new LinkCommandSettings();

        // Assert
        settings.DryRun.Should().BeFalse();
    }

    [Fact]
    public void Force_DefaultsToFalse()
    {
        // Arrange & Act
        var settings = new LinkCommandSettings();

        // Assert
        settings.Force.Should().BeFalse();
    }
}
