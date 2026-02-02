// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models;
using Dottie.Configuration.Parsing;
using FluentAssertions;
using Xunit;

namespace Dottie.Configuration.Tests;

/// <summary>
/// Tests for <see cref="ProfileResolver"/>.
/// </summary>
public sealed class ProfileResolverTests
{
    private static readonly string FixturesPath = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "Fixtures");

#pragma warning disable SA1124 // Do not use regions

    #region Implicit Default Profile Tests (US1)

    [Fact]
    public void GetProfile_NullProfileName_ReturnsDefaultProfile()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var result = resolver.GetProfile(null);

        // Assert
        result.IsSuccess.Should().BeTrue("null profile name should use default");
        result.Profile.Should().NotBeNull();
        result.Profile!.Dotfiles.Should().HaveCount(2);
    }

    [Fact]
    public void GetProfile_EmptyProfileName_ReturnsDefaultProfile()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var result = resolver.GetProfile(string.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue("empty profile name should use default");
        result.Profile.Should().NotBeNull();
        result.Profile!.Dotfiles.Should().HaveCount(2);
    }

    [Fact]
    public void GetProfile_DefaultNotDefined_ReturnsImplicitEmptyDefault()
    {
        // Arrange - use fixture without explicit 'default' profile
        var fixturePath = Path.Combine(FixturesPath, "profile-implicit-default.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var result = resolver.GetProfile(null);

        // Assert
        result.IsSuccess.Should().BeTrue("should return implicit empty default when 'default' profile not defined");
        result.Profile.Should().NotBeNull();
        result.Profile!.Dotfiles.Should().BeEmpty("implicit default has no dotfiles");
        result.Profile!.Install.Should().BeNull("implicit default has no install block");
    }

    #endregion

#pragma warning restore SA1124

    [Fact]
    public void GetProfile_ExistingProfile_ReturnsProfile()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var result = resolver.GetProfile("work");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile.Should().NotBeNull();
        result.Profile!.Dotfiles.Should().HaveCount(2);
        result.Profile.Dotfiles[0].Source.Should().Be("dotfiles/work/aws-config");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void GetProfile_NonExistentProfile_ReturnsErrorWithAvailableList()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var result = resolver.GetProfile("nonexistent");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Profile.Should().BeNull();
        result.Error.Should().Contain("nonexistent");
        result.AvailableProfiles.Should().Contain("default");
        result.AvailableProfiles.Should().Contain("work");
        result.AvailableProfiles.Should().Contain("minimal");
    }

    // NOTE: Tests GetProfile_NoProfileSpecified_ReturnsErrorWithAvailableList and
    // GetProfile_EmptyProfileName_ReturnsErrorWithAvailableList removed.
    // The new behavior returns 'default' profile when no profile is specified.
    // See new tests: GetProfile_NullProfileName_ReturnsDefaultProfile,
    // GetProfile_EmptyProfileName_ReturnsDefaultProfile, and
    // GetProfile_DefaultNotDefined_ReturnsImplicitEmptyDefault
    [Fact]
    public void ListProfiles_ReturnsAllProfileNames()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var profiles = resolver.ListProfiles();

        // Assert
        profiles.Should().HaveCount(3);
        profiles.Should().Contain("default");
        profiles.Should().Contain("work");
        profiles.Should().Contain("minimal");
    }

    [Fact]
    public void ListProfiles_ReturnsProfilesInAlphabeticalOrder()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var profiles = resolver.ListProfiles();

        // Assert
        profiles.Should().BeInAscendingOrder();
    }

    [Fact]
    public void GetProfile_DefaultProfile_ReturnsDefaultProfile()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var result = resolver.GetProfile("default");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Profile.Should().NotBeNull();
        result.Profile!.Dotfiles.Should().HaveCount(2);
        result.Profile.Dotfiles[0].Source.Should().Be("dotfiles/bashrc");
        result.Profile.Install.Should().NotBeNull();
        result.Profile.Install!.Apt.Should().Contain("git");
    }

    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProfileResolver(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

#pragma warning disable SA1124 // Do not use regions

    #region ListProfilesWithInfo Tests (US4)

    [Fact]
    public void ListProfilesWithInfo_ReturnsProfileInfoWithExtends()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-inheritance.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var profiles = resolver.ListProfilesWithInfo();

        // Assert
        profiles.Should().NotBeEmpty();

        var defaultProfile = profiles.FirstOrDefault(p => string.Equals(p.Name, "default", StringComparison.Ordinal));
        defaultProfile.Should().NotBeNull();
        defaultProfile!.Extends.Should().BeNull("default has no parent");

        var workProfile = profiles.FirstOrDefault(p => string.Equals(p.Name, "work", StringComparison.Ordinal));
        workProfile.Should().NotBeNull();
        workProfile!.Extends.Should().Be("default", "work extends default");
    }

    [Fact]
    public void ListProfilesWithInfo_SortsAlphabetically()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var profiles = resolver.ListProfilesWithInfo();

        // Assert
        var names = profiles.Select(p => p.Name).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Fact]
    public void ListProfilesWithInfo_IncludesDotfileCountAndInstallBlock()
    {
        // Arrange
        var fixturePath = Path.Combine(FixturesPath, "valid-multiple-profiles.yaml");
        var loader = new ConfigurationLoader();
        var loadResult = loader.Load(fixturePath);
        loadResult.IsSuccess.Should().BeTrue("fixture should load successfully");

        var resolver = new ProfileResolver(loadResult.Configuration!);

        // Act
        var profiles = resolver.ListProfilesWithInfo();

        // Assert
        var defaultProfile = profiles.FirstOrDefault(p => string.Equals(p.Name, "default", StringComparison.Ordinal));
        defaultProfile.Should().NotBeNull();
        defaultProfile!.DotfileCount.Should().BeGreaterThan(0);
        defaultProfile.HasInstallBlock.Should().BeTrue("default has an install block");

        var minimalProfile = profiles.FirstOrDefault(p => string.Equals(p.Name, "minimal", StringComparison.Ordinal));
        minimalProfile.Should().NotBeNull();
        minimalProfile!.HasInstallBlock.Should().BeFalse("minimal has no install block");
    }

    #endregion

#pragma warning restore SA1124
}
