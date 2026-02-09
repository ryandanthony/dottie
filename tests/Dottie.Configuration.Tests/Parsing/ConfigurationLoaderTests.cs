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

    [Fact]
    public void Load_VariableAptRepoYaml_ResolvesArchitectureVariablesInAptRepoFields()
    {
        // Arrange - use a fixture with only architecture variables (available on all platforms)
        var yaml = @"
profiles:
  default:
    install:
      aptRepos:
        - name: test-repo
          key_url: https://example.com/gpg.key
          repo: ""deb [arch=${MS_ARCH}] https://example.com/stable main""
          packages:
            - test-${MS_ARCH}
";
        var loader = new ConfigurationLoader();

        // Act
        var result = loader.LoadFromString(yaml);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var aptRepos = result.Configuration!.Profiles["default"].Install!.AptRepos;
        aptRepos.Should().HaveCount(1);

        // Architecture variables should be resolved on all platforms
        aptRepos[0].Repo.Should().NotContain("${MS_ARCH}");
        aptRepos[0].Packages[0].Should().NotContain("${MS_ARCH}");
    }

    [Fact]
    public void Load_VariableDotfilesYaml_ResolvesArchitectureVariablesInDotfileFields()
    {
        // Arrange - use a fixture with only architecture variables (available on all platforms)
        var yaml = @"
profiles:
  default:
    dotfiles:
      - source: dotfiles/${MS_ARCH}/bashrc
        target: ~/.bashrc
";
        var loader = new ConfigurationLoader();

        // Act
        var result = loader.LoadFromString(yaml);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dotfiles = result.Configuration!.Profiles["default"].Dotfiles;
        dotfiles.Should().HaveCount(1);

        // Architecture variables should be resolved on all platforms
        dotfiles[0].Source.Should().NotContain("${MS_ARCH}");
        dotfiles[0].Source.Should().Contain(Dottie.Configuration.Utilities.ArchitectureDetector.CurrentArchitecture);
    }

    [Fact]
    public void Load_MissingOsRelease_NoOsVarsUsed_SucceedsWithWarning()
    {
        // Arrange - T034: missing OS release + no OS vars → success with warning
        var yaml = @"
profiles:
  default:
    dotfiles:
      - source: dotfiles/bashrc
        target: ~/.bashrc
";
        var loader = new ConfigurationLoader(osReleasePath: "/nonexistent/os-release");

        // Act
        var result = loader.LoadFromString(yaml);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Warnings.Should().ContainSingle()
            .Which.Message.Should().Contain("os-release");
    }

    [Fact]
    public void Load_MissingOsRelease_OsVarsReferenced_ReturnsError()
    {
        // Arrange - T035: missing OS release + ${VERSION_CODENAME} → error
        var yaml = @"
profiles:
  default:
    install:
      aptRepos:
        - name: test-repo
          key_url: https://example.com/gpg.key
          repo: ""deb [arch=amd64] https://example.com/${VERSION_CODENAME} stable main""
          packages:
            - test-pkg
";
        var loader = new ConfigurationLoader(osReleasePath: "/nonexistent/os-release");

        // Act
        var result = loader.LoadFromString(yaml);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("VERSION_CODENAME"));
    }

    [Fact]
    public void Load_VariableMixedYaml_ResolvesAllSourceTypes()
    {
        // Arrange - T041: end-to-end test with all source types using architecture variables
        var yaml = @"
profiles:
  default:
    dotfiles:
      - source: dotfiles/${MS_ARCH}/bashrc
        target: ~/.bashrc
    install:
      aptRepos:
        - name: test-repo
          key_url: https://packages.example.com/gpg.key
          repo: ""deb [arch=${MS_ARCH}] https://packages.example.com/stable main""
          packages:
            - test-${MS_ARCH}
      github:
        - repo: example/tool
          asset: ""tool-${MS_ARCH}-${RELEASE_VERSION}.tar.gz""
          binary: tool
";
        var loader = new ConfigurationLoader();

        // Act
        var result = loader.LoadFromString(yaml);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var profile = result.Configuration!.Profiles["default"];
        var arch = Dottie.Configuration.Utilities.ArchitectureDetector.CurrentArchitecture;

        // Dotfile: ${MS_ARCH} resolved
        profile.Dotfiles[0].Source.Should().Be($"dotfiles/{arch}/bashrc");

        // AptRepo: ${MS_ARCH} resolved
        profile.Install!.AptRepos[0].Repo.Should().Contain(arch);
        profile.Install.AptRepos[0].Packages[0].Should().Be($"test-{arch}");

        // GitHub: ${MS_ARCH} resolved, ${RELEASE_VERSION} deferred
        profile.Install.Github[0].Asset.Should().Contain(arch);
        profile.Install.Github[0].Asset.Should().Contain("${RELEASE_VERSION}");
    }
}
