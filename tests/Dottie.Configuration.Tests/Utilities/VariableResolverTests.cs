// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models;
using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Utilities;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Utilities;

/// <summary>
/// Tests for <see cref="VariableResolver"/>.
/// </summary>
public class VariableResolverTests
{
    private static readonly IReadOnlyDictionary<string, string> SampleVariables = new Dictionary<string, string>
    {
        ["VERSION_CODENAME"] = "noble",
        ["ID"] = "ubuntu",
        ["VERSION_ID"] = "24.04",
        ["ARCH"] = "x86_64",
        ["MS_ARCH"] = "amd64",
    };

    #region ResolveString Tests (T012)

    [Fact]
    public void ResolveString_NoVariables_ReturnsUnchanged()
    {
        // Arrange
        var input = "this is a plain string with no variables";

        // Act
        var result = VariableResolver.ResolveString(input, SampleVariables);

        // Assert
        result.ResolvedValue.Should().Be(input);
        result.HasErrors.Should().BeFalse();
        result.UnresolvedVariables.Should().BeEmpty();
    }

    [Fact]
    public void ResolveString_SingleVariable_ReturnsSubstituted()
    {
        // Arrange
        var input = "deb https://packages.example.com/${VERSION_CODENAME}/prod main";

        // Act
        var result = VariableResolver.ResolveString(input, SampleVariables);

        // Assert
        result.ResolvedValue.Should().Be("deb https://packages.example.com/noble/prod main");
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ResolveString_MultipleVariables_AllSubstituted()
    {
        // Arrange
        var input = "deb [arch=${MS_ARCH}] https://packages.example.com/${VERSION_CODENAME}/prod main";

        // Act
        var result = VariableResolver.ResolveString(input, SampleVariables);

        // Assert
        result.ResolvedValue.Should().Be("deb [arch=amd64] https://packages.example.com/noble/prod main");
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ResolveString_UnknownVariable_ReportsError()
    {
        // Arrange
        var input = "value-${UNKNOWN_VAR}-end";

        // Act
        var result = VariableResolver.ResolveString(input, SampleVariables);

        // Assert
        result.HasErrors.Should().BeTrue();
        result.UnresolvedVariables.Should().Contain("UNKNOWN_VAR");
    }

    [Fact]
    public void ResolveString_DeferredVariable_LeftAsIs()
    {
        // Arrange
        var input = "app-${MS_ARCH}-${RELEASE_VERSION}.tar.gz";
        var deferred = new HashSet<string> { "RELEASE_VERSION" };

        // Act
        var result = VariableResolver.ResolveString(input, SampleVariables, deferred);

        // Assert
        result.ResolvedValue.Should().Be("app-amd64-${RELEASE_VERSION}.tar.gz");
        result.HasErrors.Should().BeFalse();
        result.UnresolvedVariables.Should().BeEmpty();
    }

    [Fact]
    public void ResolveString_MixedResolvedDeferredUnresolved_HandlesAll()
    {
        // Arrange
        var input = "${MS_ARCH}-${RELEASE_VERSION}-${NONEXISTENT}";
        var deferred = new HashSet<string> { "RELEASE_VERSION" };

        // Act
        var result = VariableResolver.ResolveString(input, SampleVariables, deferred);

        // Assert
        result.ResolvedValue.Should().Be("amd64-${RELEASE_VERSION}-${NONEXISTENT}");
        result.HasErrors.Should().BeTrue();
        result.UnresolvedVariables.Should().Contain("NONEXISTENT");
        result.UnresolvedVariables.Should().NotContain("RELEASE_VERSION");
    }

    [Fact]
    public void ResolveString_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = VariableResolver.ResolveString(input, SampleVariables);

        // Assert
        result.ResolvedValue.Should().BeEmpty();
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ResolveString_NullInput_ReturnsEmpty()
    {
        // Act
        var result = VariableResolver.ResolveString(null!, SampleVariables);

        // Assert
        result.ResolvedValue.Should().BeEmpty();
        result.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ResolveString_WhitespaceInput_ReturnsUnchanged()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = VariableResolver.ResolveString(input, SampleVariables);

        // Assert
        result.ResolvedValue.Should().Be("   ");
        result.HasErrors.Should().BeFalse();
    }

    #endregion

    #region BuildVariableSet Tests (T014)

    [Fact]
    public void BuildVariableSet_CombinesOsReleaseAndArchitecture()
    {
        // Arrange
        var osReleaseVars = new Dictionary<string, string>
        {
            ["VERSION_CODENAME"] = "noble",
            ["ID"] = "ubuntu",
        };

        // Act
        var result = VariableResolver.BuildVariableSet(osReleaseVars);

        // Assert
        result.Should().ContainKey("VERSION_CODENAME").WhoseValue.Should().Be("noble");
        result.Should().ContainKey("ID").WhoseValue.Should().Be("ubuntu");
        result.Should().ContainKey("ARCH");
        result.Should().ContainKey("MS_ARCH");
    }

    [Fact]
    public void BuildVariableSet_ArchitectureOverridesConflictingOsReleaseKeys()
    {
        // Arrange - unlikely but test the override behavior
        var osReleaseVars = new Dictionary<string, string>
        {
            ["ARCH"] = "should-be-overridden",
            ["MS_ARCH"] = "should-be-overridden",
            ["ID"] = "ubuntu",
        };

        // Act
        var result = VariableResolver.BuildVariableSet(osReleaseVars);

        // Assert
        result["ARCH"].Should().NotBe("should-be-overridden");
        result["MS_ARCH"].Should().NotBe("should-be-overridden");
        result["ID"].Should().Be("ubuntu");
    }

    [Fact]
    public void BuildVariableSet_EmptyOsReleaseDict_StillHasArchVars()
    {
        // Arrange
        var osReleaseVars = new Dictionary<string, string>();

        // Act
        var result = VariableResolver.BuildVariableSet(osReleaseVars);

        // Assert
        result.Should().ContainKey("ARCH");
        result.Should().ContainKey("MS_ARCH");
        result.Should().HaveCount(2);
    }

    #endregion

    #region ResolveConfiguration Tests - AptRepo (T016-T017)

    [Fact]
    public void ResolveConfiguration_AptRepoWithVariables_ResolvesAllFields()
    {
        // Arrange
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["default"] = new ConfigProfile
                {
                    Install = new InstallBlock
                    {
                        AptRepos =
                        [
                            new AptRepoItem
                            {
                                Name = "microsoft-prod",
                                Repo = "deb [arch=${MS_ARCH}] https://packages.microsoft.com/${VERSION_CODENAME}/prod main",
                                KeyUrl = "https://packages.microsoft.com/${VERSION_CODENAME}/gpg.key",
                                Packages = ["code-${MS_ARCH}"],
                            },
                        ],
                    },
                },
            },
        };

        // Act
        var result = VariableResolver.ResolveConfiguration(config, SampleVariables);

        // Assert
        result.HasErrors.Should().BeFalse();
        var aptRepo = result.Configuration.Profiles["default"].Install!.AptRepos[0];
        aptRepo.Repo.Should().Be("deb [arch=amd64] https://packages.microsoft.com/noble/prod main");
        aptRepo.KeyUrl.Should().Be("https://packages.microsoft.com/noble/gpg.key");
        aptRepo.Packages[0].Should().Be("code-amd64");
    }

    [Fact]
    public void ResolveConfiguration_AptRepoWithNoVariables_OutputEqualsInput()
    {
        // Arrange (FR-009 backwards compatibility)
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["default"] = new ConfigProfile
                {
                    Install = new InstallBlock
                    {
                        AptRepos =
                        [
                            new AptRepoItem
                            {
                                Name = "plain-repo",
                                Repo = "deb https://example.com/stable main",
                                KeyUrl = "https://example.com/gpg.key",
                                Packages = ["package1", "package2"],
                            },
                        ],
                    },
                },
            },
        };

        // Act
        var result = VariableResolver.ResolveConfiguration(config, SampleVariables);

        // Assert
        result.HasErrors.Should().BeFalse();
        var aptRepo = result.Configuration.Profiles["default"].Install!.AptRepos[0];
        aptRepo.Repo.Should().Be("deb https://example.com/stable main");
        aptRepo.KeyUrl.Should().Be("https://example.com/gpg.key");
        aptRepo.Packages.Should().BeEquivalentTo(["package1", "package2"]);
    }

    #endregion

    #region ResolveConfiguration Tests - GithubRelease (T023)

    [Fact]
    public void ResolveConfiguration_GithubWithVariables_ResolvesGlobalAndDefersReleaseVersion()
    {
        // Arrange
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["default"] = new ConfigProfile
                {
                    Install = new InstallBlock
                    {
                        Github =
                        [
                            new GithubReleaseItem
                            {
                                Repo = "owner/tool",
                                Asset = "tool-${MS_ARCH}-${RELEASE_VERSION}.tar.gz",
                                Binary = "tool-${ARCH}",
                            },
                        ],
                    },
                },
            },
        };

        // Act
        var result = VariableResolver.ResolveConfiguration(config, SampleVariables);

        // Assert
        result.HasErrors.Should().BeFalse();
        var github = result.Configuration.Profiles["default"].Install!.Github[0];
        github.Asset.Should().Be("tool-amd64-${RELEASE_VERSION}.tar.gz");
        github.Binary.Should().Be("tool-x86_64");
    }

    #endregion

    #region ResolveConfiguration Tests - Dotfiles (T029-T030)

    [Fact]
    public void ResolveConfiguration_DotfileWithVariables_ResolvesSourceAndTarget()
    {
        // Arrange
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["default"] = new ConfigProfile
                {
                    Dotfiles =
                    [
                        new DotfileEntry
                        {
                            Source = "dotfiles/${VERSION_CODENAME}/bashrc",
                            Target = "~/.config/${ARCH}/bashrc",
                        },
                    ],
                },
            },
        };

        // Act
        var result = VariableResolver.ResolveConfiguration(config, SampleVariables);

        // Assert
        result.HasErrors.Should().BeFalse();
        var dotfile = result.Configuration.Profiles["default"].Dotfiles[0];
        dotfile.Source.Should().Be("dotfiles/noble/bashrc");
        dotfile.Target.Should().Be("~/.config/x86_64/bashrc");
    }

    [Fact]
    public void ResolveConfiguration_DotfileWithReleaseVersion_ReportsError()
    {
        // Arrange - ${RELEASE_VERSION} is NOT deferred in dotfile context
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["default"] = new ConfigProfile
                {
                    Dotfiles =
                    [
                        new DotfileEntry
                        {
                            Source = "dotfiles/${RELEASE_VERSION}/bashrc",
                            Target = "~/.bashrc",
                        },
                    ],
                },
            },
        };

        // Act
        var result = VariableResolver.ResolveConfiguration(config, SampleVariables);

        // Assert - RELEASE_VERSION should be an error in dotfile context (not deferred)
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(e => e.VariableName == "RELEASE_VERSION");
    }

    #endregion

    #region Unsupported Architecture (T036)

    [Fact]
    public void ResolveConfiguration_UnknownArchitecture_ReportsErrorForMsArchReference()
    {
        // Arrange - FR-004: unknown architecture + ${MS_ARCH} reference → error
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["VERSION_CODENAME"] = "noble",
        };

        // Deliberately omit MS_ARCH and ARCH (simulating BuildVariableSet with unknown arch)
        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["default"] = new ConfigProfile
                {
                    Install = new InstallBlock
                    {
                        AptRepos =
                        [
                            new AptRepoItem
                            {
                                Name = "test-repo",
                                Repo = "deb [arch=${MS_ARCH}] https://example.com/noble main",
                                KeyUrl = "https://example.com/gpg.key",
                                Packages = ["test-pkg"],
                            },
                        ],
                    },
                },
            },
        };

        // Act
        var result = VariableResolver.ResolveConfiguration(config, variables);

        // Assert - MS_ARCH should be unresolvable
        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(e => e.VariableName == "MS_ARCH");
    }

    #endregion

    #region Error Message Format (T039)

    [Fact]
    public void ResolveConfiguration_UnresolvableVariable_ErrorContainsAllContext()
    {
        // Arrange - T039: verify error includes variable name, profile, entry, field
        var variables = new Dictionary<string, string>(StringComparer.Ordinal);

        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["work"] = new ConfigProfile
                {
                    Install = new InstallBlock
                    {
                        AptRepos =
                        [
                            new AptRepoItem
                            {
                                Name = "my-repo",
                                Repo = "deb https://example.com/${MISSING_VAR} main",
                                KeyUrl = "https://example.com/gpg.key",
                                Packages = ["pkg"],
                            },
                        ],
                    },
                },
            },
        };

        // Act
        var result = VariableResolver.ResolveConfiguration(config, variables);

        // Assert - error message must contain all context per FR-011
        result.HasErrors.Should().BeTrue();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.VariableName.Should().Be("MISSING_VAR");
        error.ProfileName.Should().Be("work");
        error.EntryIdentifier.Should().Be("my-repo");
        error.FieldName.Should().Be("repo");
        error.Message.Should().Contain("${MISSING_VAR}");
        error.Message.Should().Contain("work");
        error.Message.Should().Contain("my-repo");
        error.Message.Should().Contain("repo");
    }

    #endregion

    #region Performance (T043)

    [Fact]
    public void ResolveConfiguration_30Items_CompletesUnderOneSecond()
    {
        // Arrange - SC-005: performance sanity check
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["VERSION_CODENAME"] = "noble",
            ["ID"] = "ubuntu",
            ["VERSION_ID"] = "24.04",
            ["ARCH"] = "x86_64",
            ["MS_ARCH"] = "amd64",
        };

        var aptRepos = new List<AptRepoItem>();
        var githubItems = new List<GithubReleaseItem>();
        var dotfiles = new List<DotfileEntry>();

        for (int i = 0; i < 10; i++)
        {
            aptRepos.Add(new AptRepoItem
            {
                Name = $"repo-{i}",
                Repo = $"deb [arch=${{MS_ARCH}}] https://example.com/${{VERSION_CODENAME}} main",
                KeyUrl = $"https://example.com/{i}/gpg.key",
                Packages = [$"pkg-${{MS_ARCH}}-{i}"],
            });

            githubItems.Add(new GithubReleaseItem
            {
                Repo = $"org/tool-{i}",
                Asset = $"tool-{i}-${{MS_ARCH}}-${{RELEASE_VERSION}}.tar.gz",
                Binary = $"tool-{i}",
            });

            dotfiles.Add(new DotfileEntry
            {
                Source = $"dotfiles/${{VERSION_CODENAME}}/{i}/config",
                Target = $"~/.config/${{ARCH}}/{i}/config",
            });
        }

        var config = new DottieConfiguration
        {
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["default"] = new ConfigProfile
                {
                    Dotfiles = dotfiles,
                    Install = new InstallBlock
                    {
                        AptRepos = aptRepos,
                        Github = githubItems,
                    },
                },
            },
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = VariableResolver.ResolveConfiguration(config, variables);
        stopwatch.Stop();

        // Assert
        result.HasErrors.Should().BeFalse();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    #endregion
}
