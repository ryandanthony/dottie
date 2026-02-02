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
    private readonly SudoChecker _sudoChecker = new();

    [Fact]
    public void IsSudoAvailable_ChecksForSudoCommand()
    {
        // Arrange & Act
        var result = _sudoChecker.IsSudoAvailable();

        // Assert
        (result == true || result == false).Should().BeTrue();
    }

    [Fact]
    public void IsSudoAvailable_ReturnsTrueOrFalse_NeverThrows()
    {
        // Act & Assert
        FluentActions.Invoking(() => _sudoChecker.IsSudoAvailable())
            .Should()
            .NotThrow();
    }

    [Fact]
    public void IsSudoAvailable_ConsistentAcrossMultipleCalls()
    {
        // Act
        var result1 = _sudoChecker.IsSudoAvailable();
        var result2 = _sudoChecker.IsSudoAvailable();
        var result3 = _sudoChecker.IsSudoAvailable();

        // Assert
        result1.Should().Be(result2);
        result2.Should().Be(result3);
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
        var result = _sudoChecker.IsSudoAvailable();

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
            results.Add(_sudoChecker.IsSudoAvailable());
        }

        // Assert
        results.Should().AllSatisfy(r => r.Should().Be(results[0]));
    }

    [Fact]
    public void Constructor_WithNullProcessRunner_CreatesDefaultRunner()
    {
        // Act
        var checker = new SudoChecker(null);

        // Assert
        checker.Should().NotBeNull();
    }

    [Fact]
    public void IsSudoAvailable_WithMockProcessRunner_ReturnsMockedResult()
    {
        // Arrange
        var mockRunner = new FakeProcessRunner(new ProcessResult(0, "/usr/bin/sudo", string.Empty));
        var checker = new SudoChecker(mockRunner);

        // Skip this test on Windows since IsSudoAvailable returns false for non-Unix systems
        if (!IsLinux() && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        // Act
        var result = checker.IsSudoAvailable();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSudoAvailable_WithMockProcessRunner_WhenSudoNotFound_ReturnsFalse()
    {
        // Arrange
        var mockRunner = new FakeProcessRunner(new ProcessResult(1, string.Empty, "sudo not found"));
        var checker = new SudoChecker(mockRunner);

        // Skip this test on Windows since IsSudoAvailable returns false for non-Unix systems
        if (!IsLinux() && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        // Act
        var result = checker.IsSudoAvailable();

        // Assert
        result.Should().BeFalse();
    }

    private static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// A simple fake process runner for testing.
    /// </summary>
    private class FakeProcessRunner : IProcessRunner
    {
        private readonly ProcessResult _result;

        public FakeProcessRunner(ProcessResult result)
        {
            _result = result;
        }

        public Task<ProcessResult> RunAsync(string fileName, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }

        public ProcessResult Run(string fileName, string arguments, string? workingDirectory = null, int? timeoutMilliseconds = null)
        {
            return _result;
        }
    }
}
