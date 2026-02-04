// -----------------------------------------------------------------------
// <copyright file="ApplyCommandSettingsTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Commands;
using FluentAssertions;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="ApplyCommandSettings"/>.
/// </summary>
public sealed class ApplyCommandSettingsTests
{
    [Fact]
    public void Validate_WithDryRunOnly_ReturnsSuccess()
    {
        // Arrange
        var settings = new ApplyCommandSettings
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
        var settings = new ApplyCommandSettings
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
        var settings = new ApplyCommandSettings
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
    public void Validate_WithDryRunAndForce_ReturnsSuccess()
    {
        // Arrange - Both flags can be used together (dry-run shows what force would do)
        var settings = new ApplyCommandSettings
        {
            DryRun = true,
            Force = true,
        };

        // Act
        var result = settings.Validate();

        // Assert - Both flags are allowed together
        result.Successful.Should().BeTrue();
    }

    [Fact]
    public void DryRun_DefaultsToFalse()
    {
        // Arrange & Act
        var settings = new ApplyCommandSettings();

        // Assert
        settings.DryRun.Should().BeFalse();
    }

    [Fact]
    public void Force_DefaultsToFalse()
    {
        // Arrange & Act
        var settings = new ApplyCommandSettings();

        // Assert
        settings.Force.Should().BeFalse();
    }

    [Fact]
    public void ProfileName_DefaultsToNull()
    {
        // Arrange & Act
        var settings = new ApplyCommandSettings();

        // Assert
        settings.ProfileName.Should().BeNull();
    }

    [Fact]
    public void ConfigPath_DefaultsToNull()
    {
        // Arrange & Act
        var settings = new ApplyCommandSettings();

        // Assert
        settings.ConfigPath.Should().BeNull();
    }
}
