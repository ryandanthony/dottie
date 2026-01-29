// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    [Fact]
    public void GetProfile_NoProfileSpecified_ReturnsErrorWithAvailableList()
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
        result.IsSuccess.Should().BeFalse();
        result.Profile.Should().BeNull();
        result.Error.Should().Contain("profile");
        result.AvailableProfiles.Should().Contain("default");
        result.AvailableProfiles.Should().Contain("work");
        result.AvailableProfiles.Should().Contain("minimal");
    }

    [Fact]
    public void GetProfile_EmptyProfileName_ReturnsErrorWithAvailableList()
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
        result.IsSuccess.Should().BeFalse();
        result.Profile.Should().BeNull();
        result.Error.Should().NotBeNullOrEmpty();
        result.AvailableProfiles.Should().HaveCount(3);
    }

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
}
