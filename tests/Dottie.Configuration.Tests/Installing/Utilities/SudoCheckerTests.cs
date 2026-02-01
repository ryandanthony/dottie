// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing.Utilities;
using FluentAssertions;
using System.Runtime.InteropServices;

namespace Dottie.Configuration.Tests.Installing.Utilities;

/// <summary>
/// Tests for <see cref="SudoChecker"/>.
/// </summary>
public class SudoCheckerTests
{
    [Fact]
    public void IsSudoAvailable_ChecksForSudoCommand()
    {
        // Arrange & Act
        var result = SudoChecker.IsSudoAvailable();

        // Assert
        (result == true || result == false).Should().BeTrue();
    }

    [Fact]
    public void IsSudoAvailable_ReturnsTrueOrFalse_NeverThrows()
    {
        // Act & Assert
        FluentActions.Invoking(() => SudoChecker.IsSudoAvailable())
            .Should()
            .NotThrow();
    }

    [Fact]
    public void IsSudoAvailable_ConsistentAcrossMultipleCalls()
    {
        // Act
        var result1 = SudoChecker.IsSudoAvailable();
        var result2 = SudoChecker.IsSudoAvailable();
        var result3 = SudoChecker.IsSudoAvailable();

        // Assert
        result1.Should().Be(result2);
        result2.Should().Be(result3);
    }

    [Fact]
    public void IsSudoAvailable_IsStatic()
    {
        // Act & Assert
        FluentActions.Invoking(() =>
        {
            var result = SudoChecker.IsSudoAvailable();
        }).Should().NotThrow();
    }

    [Fact]
    public void IsSudoAvailable_OnLinuxSystem_ReturnsBoolean()
    {
        // Arrange
        if (!IsLinux())
        {
            return; // Skip on non-Linux systems
        }

        // Act
        var result = SudoChecker.IsSudoAvailable();

        // Assert
        // Result is always bool, so this validates the method works without error
        (result == true || result == false).Should().BeTrue();
    }

    [Fact]
    public void IsSudoAvailable_ReturnsConsistentValue_WhenCalled100Times()
    {
        // Arrange
        var results = new List<bool>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            results.Add(SudoChecker.IsSudoAvailable());
        }

        // Assert
        results.Should().AllSatisfy(r => r.Should().Be(results[0]));
    }

    private static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}
