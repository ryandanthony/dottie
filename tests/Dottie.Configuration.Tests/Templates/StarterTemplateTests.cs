// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Templates;
using FluentAssertions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dottie.Configuration.Tests.Templates;

/// <summary>
/// Tests for <see cref="StarterTemplate"/>.
/// </summary>
public sealed class StarterTemplateTests : IDisposable
{
    private readonly string _tempDir;

    /// <summary>
    /// Initializes a new instance of the <see cref="StarterTemplateTests"/> class.
    /// </summary>
    public StarterTemplateTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dottie-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_ReturnsValidYamlString()
    {
        // Act
        var result = StarterTemplate.Generate();

        // Assert
        result.Should().NotBeNullOrWhiteSpace();

        // Verify it's valid YAML by parsing it
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var action = () => deserializer.Deserialize<object>(result);
        action.Should().NotThrow();
    }

    [Fact]
    public void Generate_IncludesDefaultProfile()
    {
        // Act
        var result = StarterTemplate.Generate();

        // Assert
        result.Should().Contain("profiles:");
        result.Should().Contain("default:");
    }

    [Fact]
    public void Generate_IncludesDotfilesSection()
    {
        // Act
        var result = StarterTemplate.Generate();

        // Assert
        result.Should().Contain("dotfiles:");
    }

    [Fact]
    public void Generate_IncludesCommentedExamples()
    {
        // Act
        var result = StarterTemplate.Generate();

        // Assert
        result.Should().Contain("#", because: "template should include helpful comments");
    }

    [Fact]
    public void Generate_IncludesCommentedInstallSection()
    {
        // Act
        var result = StarterTemplate.Generate();

        // Assert
        // The install section should be commented out as an optional example
        result.Should().Contain("install", because: "template should show install section example");
    }

    [Fact]
    public void Generate_IncludesSourceAndTargetInDotfileExample()
    {
        // Act
        var result = StarterTemplate.Generate();

        // Assert
        result.Should().Contain("source:");
        result.Should().Contain("target:");
    }

    [Fact]
    public void Generate_ProducesConsistentOutput()
    {
        // Act
        var result1 = StarterTemplate.Generate();
        var result2 = StarterTemplate.Generate();

        // Assert
        result1.Should().Be(result2, because: "template generation should be deterministic");
    }

    [Fact]
    public void Generate_IncludesHeaderComment()
    {
        // Act
        var result = StarterTemplate.Generate();

        // Assert
        result.Should().StartWith("#", because: "template should start with a descriptive comment");
        result.Should().Contain("dottie", because: "template should mention dottie in the header");
    }

    [Fact]
    public void DefaultFileName_ReturnsDottieYaml()
    {
        // Act & Assert
        StarterTemplate.DefaultFileName.Should().Be("dottie.yaml");
    }

    [Fact]
    public void WriteTo_CreatesFileWithContent()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "test-config.yaml");

        // Act
        StarterTemplate.WriteTo(filePath);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var content = File.ReadAllText(filePath);
        content.Should().Contain("profiles:");
        content.Should().Contain("default:");
    }

    [Fact]
    public void WriteTo_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => StarterTemplate.WriteTo(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("path");
    }

    [Fact]
    public void WriteToDirectory_CreatesFileWithDefaultName()
    {
        // Act
        StarterTemplate.WriteToDirectory(_tempDir);

        // Assert
        var expectedPath = Path.Combine(_tempDir, "dottie.yaml");
        File.Exists(expectedPath).Should().BeTrue();
        var content = File.ReadAllText(expectedPath);
        content.Should().Contain("profiles:");
    }

    [Fact]
    public void WriteToDirectory_NullDirectory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => StarterTemplate.WriteToDirectory(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("directory");
    }
}
