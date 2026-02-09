// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Inheritance;
using Dottie.Configuration.Parsing;
using FluentAssertions;
using Xunit;

namespace Dottie.Configuration.Tests.Inheritance;

/// <summary>
/// Tests for <see cref="ProfileMerger"/>.
/// </summary>
public sealed class ProfileMergerTests
{
    private static readonly string FixturesPath = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "Fixtures");

    [Fact]
    public void Resolve_ValidInheritance_ReturnsMergedProfile()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("work");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile.Should().NotBeNull();
        result.Profile!.Name.Should().Be("work");
        result.Profile.InheritanceChain.Should().Equal("default", "work");
    }

    [Fact]
    public void Resolve_DotfilesAppendChildAfterParent()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("work");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile!.Dotfiles.Should().HaveCount(3);

        // Parent dotfiles first
        result.Profile.Dotfiles[0].Source.Should().Be("dotfiles/bashrc");
        result.Profile.Dotfiles[1].Source.Should().Be("dotfiles/vimrc");

        // Child dotfiles last
        result.Profile.Dotfiles[2].Source.Should().Be("dotfiles/work/aws-config");
    }

    [Fact]
    public void Resolve_AptPackagesAppend()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("work");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile!.Install.Should().NotBeNull();
        result.Profile.Install!.Apt.Should().Contain("git"); // parent
        result.Profile.Install.Apt.Should().Contain("curl"); // parent
        result.Profile.Install.Apt.Should().Contain("vim"); // parent
        result.Profile.Install.Apt.Should().Contain("awscli"); // child
        result.Profile.Install.Apt.Should().Contain("kubectl"); // child
        result.Profile.Install.Apt.Should().HaveCount(5);
    }

    [Fact]
    public void Resolve_GithubItemsByRepoKey_ChildOverrides()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("work");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile!.Install.Should().NotBeNull();
        result.Profile.Install!.Github.Should().HaveCount(2);

        // Child overrides parent's fzf entry (same repo key)
        var fzf = result.Profile.Install.Github.Single(g => string.Equals(g.Repo, "junegunn/fzf", StringComparison.Ordinal));
        fzf.Asset.Should().Be("fzf-*-linux_arm64.tar.gz"); // child's value
        fzf.Version.Should().Be("0.45.0"); // child's value

        // Child adds new k9s entry
        var k9s = result.Profile.Install.Github.Single(g => string.Equals(g.Repo, "derailed/k9s", StringComparison.Ordinal));
        k9s.Asset.Should().Be("k9s_Linux_amd64.tar.gz");
    }

    [Fact]
    public void Resolve_SnapItemsByNameKey_ChildOverrides()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("work");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile!.Install.Should().NotBeNull();
        result.Profile.Install!.Snaps.Should().HaveCount(2);

        // Parent's code snap preserved
        result.Profile.Install.Snaps.Should().Contain(s => s.Name == "code");

        // Child adds slack snap
        result.Profile.Install.Snaps.Should().Contain(s => s.Name == "slack");
    }

    [Fact]
    public void Resolve_CircularInheritance_ReturnsError()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "invalid-circular-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("alpha");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Circular inheritance");
        result.Error.Should().Contain("alpha");
        result.Error.Should().Contain("beta");
    }

    [Fact]
    public void Resolve_ExtendsMissingProfile_ReturnsError()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "invalid-extends-missing.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("work");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("nonexistent");
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void Resolve_ProfileWithNoInheritance_ReturnsUnchanged()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("default");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile.Should().NotBeNull();
        result.Profile!.Name.Should().Be("default");
        result.Profile.InheritanceChain.Should().Equal("default");
        result.Profile.Dotfiles.Should().HaveCount(2);
    }

    [Fact]
    public void Resolve_NonexistentProfile_ReturnsError()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("doesnotexist");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("doesnotexist");
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProfileMerger(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void Resolve_NullProfileName_ThrowsArgumentException()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var act = () => merger.Resolve(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Resolve_EmptyProfileName_ThrowsArgumentException()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var act = () => merger.Resolve(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Resolve_PersonalProfile_MergesCorrectly()
    {
        // Arrange - tests a different child profile
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("personal");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile!.Name.Should().Be("personal");
        result.Profile.InheritanceChain.Should().Equal("default", "personal");

        // Dotfiles merged
        result.Profile.Dotfiles.Should().HaveCount(3);
        result.Profile.Dotfiles[2].Target.Should().Be("~/.gitconfig");

        // Install blocks merged
        result.Profile.Install.Should().NotBeNull();
        result.Profile.Install!.Apt.Should().Contain("htop"); // child
        result.Profile.Install.Fonts.Should().HaveCount(1);
        result.Profile.Install.Fonts[0].Name.Should().Be("JetBrains Mono");
    }

#pragma warning disable SA1124 // Do not use regions

    #region Phase 4: User Story 2 - Multi-level Inheritance Chain Tests

    [Fact]
    public void Resolve_ThreeLevelChain_MergesInCorrectOrder()
    {
        // Arrange - use profile-dedup.yaml which has base → personal → deep-child
        var fixturePath = Path.Combine(FixturesPath, "profile-dedup.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("deep-child");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile.Should().NotBeNull();
        result.Profile!.Name.Should().Be("deep-child");

        // Inheritance chain should be base → personal → deep-child
        result.Profile.InheritanceChain.Should().Equal("base", "personal", "deep-child");
    }

    [Fact]
    public void Resolve_ChildOverridesParentSetting_ChildWins()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("work");

        // Assert
        result.IsSuccess.Should().BeTrue();

        // GitHub fzf entry should have child's (work) values, not parent's (default)
        var fzf = result.Profile!.Install!.Github.Single(g =>
            string.Equals(g.Repo, "junegunn/fzf", StringComparison.Ordinal));

        // Child overrides - should have arm64 and version 0.45.0
        fzf.Asset.Should().Be("fzf-*-linux_arm64.tar.gz");
        fzf.Version.Should().Be("0.45.0");
    }

    #endregion

    #region Phase 5: User Story 3 - Dotfile Deduplication Tests

    [Fact]
    public void MergeDotfiles_SameTarget_ChildOverridesParent()
    {
        // Arrange - profile-dedup.yaml has base with gitconfig, personal overrides gitconfig
        var fixturePath = Path.Combine(FixturesPath, "profile-dedup.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("personal");

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should have 4 unique dotfiles:
        // - ~/.bashrc from base
        // - ~/.gitconfig from personal (overriding base)
        // - ~/.vimrc from base
        // - ~/.ssh/config from personal (new)
        result.Profile!.Dotfiles.Should().HaveCount(4);

        // gitconfig should be personal's version, not base's
        var gitconfig = result.Profile.Dotfiles.Single(d => string.Equals(d.Target, "~/.gitconfig", StringComparison.Ordinal));
        gitconfig.Source.Should().Be("dotfiles/personal/gitconfig");
    }

    [Fact]
    public void MergeDotfiles_DifferentTargets_BothIncluded()
    {
        // Arrange - profile-dedup.yaml has base with bashrc, personal adds ssh-config
        var fixturePath = Path.Combine(FixturesPath, "profile-dedup.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("personal");

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Base's bashrc should still be present
        result.Profile!.Dotfiles.Should().Contain(d =>
            d.Target == "~/.bashrc" && d.Source == "dotfiles/bashrc");

        // Personal's ssh-config should be added
        result.Profile.Dotfiles.Should().Contain(d =>
            d.Target == "~/.ssh/config" && d.Source == "dotfiles/personal/ssh-config");
    }

    [Fact]
    public void MergeDotfiles_ThreeLevelChain_DeepestChildWins()
    {
        // Arrange - profile-dedup.yaml: base → personal → deep-child
        // All three have gitconfig targeting ~/.gitconfig
        var fixturePath = Path.Combine(FixturesPath, "profile-dedup.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("deep-child");

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should have 5 unique dotfiles:
        // - ~/.bashrc from base
        // - ~/.gitconfig from deep-child (overriding personal, which overrode base)
        // - ~/.vimrc from base
        // - ~/.ssh/config from personal
        // - ~/.tmux.conf from deep-child
        result.Profile!.Dotfiles.Should().HaveCount(5);

        // gitconfig should be deep-child's version
        var gitconfig = result.Profile.Dotfiles.Single(d => string.Equals(d.Target, "~/.gitconfig", StringComparison.Ordinal));
        gitconfig.Source.Should().Be("dotfiles/deep/gitconfig");
    }

    #endregion

    #region Type Field Inheritance Tests (T040)

    [Fact]
    public void Resolve_GithubTypeField_ChildCanOverrideParentType()
    {
        // Arrange — inline YAML with parent having type: binary, child overriding to type: deb
        var yaml = @"
profiles:
  parent:
    install:
      github:
        - repo: jgraph/drawio-desktop
          asset: ""drawio-arm64-*.tar.gz""
          binary: drawio
          type: binary
  child:
    extends: parent
    install:
      github:
        - repo: jgraph/drawio-desktop
          asset: ""drawio-arm64-*.deb""
          type: deb
";
        var loader = new ConfigurationLoader();
        var loadResult = loader.LoadFromString(yaml);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var merger = new ProfileMerger(loadResult.Configuration!);

        // Act
        var result = merger.Resolve("child");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile.Should().NotBeNull();
        var github = result.Profile!.Install!.Github;

        // The child should override the parent's github entry for the same repo
        github.Should().HaveCount(1);
        github[0].Repo.Should().Be("jgraph/drawio-desktop");
        github[0].Type.Should().Be(Dottie.Configuration.Models.InstallBlocks.GithubReleaseAssetType.Deb);
        github[0].Asset.Should().Contain(".deb");
    }

    #endregion

#pragma warning restore SA1124
}
