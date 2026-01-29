// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models;
using Dottie.Configuration.Parsing;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Parsing;

/// <summary>
/// Tests for <see cref="ConfigurationLoader"/>.
/// </summary>
public class ConfigurationLoaderTests
{
    private static readonly string FixturesPath = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "Fixtures");

    [Fact]
    public void Load_ValidMinimalConfig_ReturnsConfiguration()
    {
        // Arrange
        var filePath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var loader = new ConfigurationLoader();

        // Act
        var result = loader.Load(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Configuration.Should().NotBeNull();
        result.Configuration!.Profiles.Should().ContainKey("default");
        result.Configuration.Profiles["default"].Dotfiles.Should().HaveCount(1);
        result.Configuration.Profiles["default"].Dotfiles[0].Source.Should().Be("dotfiles/bashrc");
        result.Configuration.Profiles["default"].Dotfiles[0].Target.Should().Be("~/.bashrc");
    }

    [Fact]
    public void Load_ValidFullConfig_ReturnsConfigurationWithAllFields()
    {
        // Arrange
        var filePath = Path.Combine(FixturesPath, "valid-full.yaml");
        var loader = new ConfigurationLoader();

        // Act
        var result = loader.Load(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Configuration.Should().NotBeNull();
        result.Configuration!.Profiles.Should().ContainKey("default");
        result.Configuration.Profiles.Should().ContainKey("work");
    }

    [Fact]
    public void Load_InvalidYamlSyntax_ReturnsErrorWithLineNumber()
    {
        // Arrange
        var filePath = Path.Combine(FixturesPath, "invalid-yaml-syntax.yaml");
        var loader = new ConfigurationLoader();

        // Act
        var result = loader.Load(filePath);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Line.Should().NotBeNull("YAML parse errors should include line numbers");
    }

    [Fact]
    public void Load_MissingProfilesKey_ReturnsValidationError()
    {
        // Arrange - create a temp file with empty profiles
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "# Empty config\n");
            var loader = new ConfigurationLoader();

            // Act
            var result = loader.Load(tempFile);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Message.Contains("profiles", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_EmptyProfiles_ReturnsValidationError()
    {
        // Arrange - create a temp file with empty profiles dictionary
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "profiles: {}\n");
            var loader = new ConfigurationLoader();

            // Act
            var result = loader.Load(tempFile);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Message.Contains("at least one profile", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_FileNotFound_ReturnsError()
    {
        // Arrange
        var loader = new ConfigurationLoader();

        // Act
        var result = loader.Load("/nonexistent/path/dottie.yaml");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void LoadFromString_ValidYaml_ReturnsConfiguration()
    {
        // Arrange
        var yaml = @"
profiles:
  default:
    dotfiles:
      - source: dotfiles/bashrc
        target: ~/.bashrc
";
        var loader = new ConfigurationLoader();

        // Act
        var result = loader.LoadFromString(yaml);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Configuration.Should().NotBeNull();
    }

    [Fact]
    public void LoadFromString_WithSourcePath_IncludesPathInErrors()
    {
        // Arrange
        var yaml = "invalid: yaml: syntax:";
        var loader = new ConfigurationLoader();

        // Act
        var result = loader.LoadFromString(yaml, "custom/path.yaml");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Path.Should().Be("custom/path.yaml");
    }
}
