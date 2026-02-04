// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Utilities;
using FluentAssertions;
using System.Runtime.InteropServices;

namespace Dottie.Configuration.Tests.Utilities;

/// <summary>
/// Tests for <see cref="ArchitectureDetector"/>.
/// </summary>
public class ArchitectureDetectorTests
{
    [Fact]
    public void GetArchitecture_ReturnsCurrentSystemArchitecture()
    {
        // Act
        var arch = ArchitectureDetector.CurrentArchitecture;

        // Assert
        arch.Should().NotBeNullOrWhiteSpace();

        // Verify it matches expected runtime architecture
        var expected = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "amd64",
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            _ => "unknown",
        };
        arch.Should().Be(expected);
    }

    [Fact]
    public void MatchesArchitecture_Amd64Pattern_ReturnsTrue()
    {
        // Arrange - test patterns against filenames that actually contain those substrings
        var testCases = new (string Filename, string Pattern)[]
        {
            ("fzf-0.45.0-linux_amd64.tar.gz", "*amd64*"),
            ("tool-1.0-x86_64-linux.tar.gz", "*x86_64*"),
            ("app_linux_amd64.tar.gz", "*linux_amd64*"),
        };

        // Act & Assert
        foreach (var (filename, pattern) in testCases)
        {
            var result = ArchitectureDetector.MatchesPattern(filename, pattern);
            result.Should().BeTrue($"Pattern '{pattern}' should match '{filename}'");
        }
    }

    [Fact]
    public void MatchesArchitecture_Arm64Pattern_ReturnsTrue()
    {
        // Arrange
        var pattern = "*arm64*";

        // Act
        var result = ArchitectureDetector.MatchesPattern("fzf-0.45.0-linux_arm64.tar.gz", pattern);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesArchitecture_WrongArchitecture_ReturnsFalse()
    {
        // Arrange
        var pattern = "*amd64*";

        // Act
        var result = ArchitectureDetector.MatchesPattern("fzf-0.45.0-linux_arm64.tar.gz", pattern);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetArchitecturePatterns_ReturnsValidPatterns()
    {
        // Act
        var patterns = ArchitectureDetector.CurrentArchitecturePatterns;

        // Assert
        patterns.Should().NotBeEmpty();
        patterns.Should().AllSatisfy(p => p.Should().Contain("*", "Patterns should be glob patterns"));
    }

    [Fact]
    public void MatchesPattern_WithNullFilename_ThrowsArgumentException()
    {
        // Arrange
        string? filename = null;
        var pattern = "*amd64*";

        // Act & Assert
        FluentActions.Invoking(() => ArchitectureDetector.MatchesPattern(filename!, pattern))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void MatchesPattern_WithNullPattern_ThrowsArgumentException()
    {
        // Arrange
        var filename = "test.tar.gz";
        string? pattern = null;

        // Act & Assert
        FluentActions.Invoking(() => ArchitectureDetector.MatchesPattern(filename, pattern!))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void MatchesPattern_WithEmptyFilename_ThrowsArgumentException()
    {
        // Arrange
        var filename = string.Empty;
        var pattern = "*amd64*";

        // Act & Assert
        FluentActions.Invoking(() => ArchitectureDetector.MatchesPattern(filename, pattern))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void MatchesPattern_WithEmptyPattern_ThrowsArgumentException()
    {
        // Arrange
        var filename = "test.tar.gz";
        var pattern = string.Empty;

        // Act & Assert
        FluentActions.Invoking(() => ArchitectureDetector.MatchesPattern(filename, pattern))
            .Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void MatchesPattern_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var pattern = "*AMD64*";

        // Act
        var result = ArchitectureDetector.MatchesPattern("fzf-0.45.0-linux_amd64.tar.gz", pattern);

        // Assert
        result.Should().BeTrue("Pattern matching should be case-insensitive");
    }

    [Fact]
    public void MatchesPattern_ComplexPattern_MatchesCorrectly()
    {
        // Arrange - pattern with multiple wildcards
        var pattern = "*linux*amd64*.tar.gz";

        // Act
        var result = ArchitectureDetector.MatchesPattern("tool-linux-amd64-v1.0.tar.gz", pattern);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("*x86_64*", "fzf-0.45.0-linux_x86_64.tar.gz", true)]
    [InlineData("*x64*", "tool-linux-x64.zip", true)]
    [InlineData("*aarch64*", "binary-aarch64-linux.tar.gz", true)]
    [InlineData("*armv7*", "app-armv7-linux.tar.gz", true)]
    [InlineData("*i386*", "tool-i386.deb", true)]
    [InlineData("*i686*", "binary-i686.tar.gz", true)]
    [InlineData("*armhf*", "package-armhf.deb", true)]
    [InlineData("*amd64*", "tool-arm64.tar.gz", false)]
    [InlineData("*x86*", "tool-amd64.tar.gz", false)]
    public void MatchesPattern_VariousArchitecturePatterns_MatchesCorrectly(string pattern, string filename, bool expectedMatch)
    {
        // Act
        var result = ArchitectureDetector.MatchesPattern(filename, pattern);

        // Assert
        result.Should().Be(expectedMatch);
    }
}
