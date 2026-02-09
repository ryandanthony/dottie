// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Validation;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Validation;

/// <summary>
/// Tests for install block validators.
/// </summary>
public class InstallBlockValidatorTests
{
    private readonly InstallBlockValidator _validator = new();

    [Fact]
    public void Validate_GithubMissingRepo_ReturnsError()
    {
        // Arrange
        var item = new GithubReleaseItem
        {
            Repo = string.Empty,
            Asset = "*.deb",
            Binary = "myapp",
        };

        // Act
        var result = _validator.ValidateGithubRelease(item, "profiles.default.install.github[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("repo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_GithubMissingAsset_ReturnsError()
    {
        // Arrange
        var item = new GithubReleaseItem
        {
            Repo = "owner/repo",
            Asset = string.Empty,
            Binary = "myapp",
        };

        // Act
        var result = _validator.ValidateGithubRelease(item, "profiles.default.install.github[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("asset", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_GithubMissingBinary_ReturnsError()
    {
        // Arrange
        var item = new GithubReleaseItem
        {
            Repo = "owner/repo",
            Asset = "*.deb",
            Binary = string.Empty,
        };

        // Act
        var result = _validator.ValidateGithubRelease(item, "profiles.default.install.github[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("binary", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_GithubValid_ReturnsSuccess()
    {
        // Arrange
        var item = new GithubReleaseItem
        {
            Repo = "owner/repo",
            Asset = "*.deb",
            Binary = "myapp",
        };

        // Act
        var result = _validator.ValidateGithubRelease(item, "profiles.default.install.github[0]");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AptRepoMissingKeyUrl_ReturnsError()
    {
        // Arrange
        var item = new AptRepoItem
        {
            Name = "docker",
            KeyUrl = string.Empty,
            Repo = "deb [arch=amd64] https://download.docker.com/linux/ubuntu jammy stable",
            Packages = ["docker-ce"],
        };

        // Act
        var result = _validator.ValidateAptRepo(item, "profiles.default.install.aptRepo[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("keyUrl", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_AptRepoMissingRepoLine_ReturnsError()
    {
        // Arrange
        var item = new AptRepoItem
        {
            Name = "docker",
            KeyUrl = "https://download.docker.com/linux/ubuntu/gpg",
            Repo = string.Empty,
            Packages = ["docker-ce"],
        };

        // Act
        var result = _validator.ValidateAptRepo(item, "profiles.default.install.aptRepo[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("repo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_SnapMissingName_ReturnsError()
    {
        // Arrange
        var item = new SnapItem
        {
            Name = string.Empty,
            Classic = true,
        };

        // Act
        var result = _validator.ValidateSnap(item, "profiles.default.install.snap[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_FontMissingUrl_ReturnsError()
    {
        // Arrange
        var item = new FontItem
        {
            Name = "JetBrains Mono",
            Url = string.Empty,
        };

        // Act
        var result = _validator.ValidateFont(item, "profiles.default.install.fonts[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("url", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_FontValid_ReturnsSuccess()
    {
        // Arrange
        var item = new FontItem
        {
            Name = "JetBrains Mono",
            Url = "https://example.com/font.zip",
        };

        // Act
        var result = _validator.ValidateFont(item, "profiles.default.install.fonts[0]");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GithubTypeBinary_MissingBinary_ReturnsError()
    {
        // Arrange — type: binary requires binary field
        var item = new GithubReleaseItem
        {
            Repo = "owner/repo",
            Asset = "*.tar.gz",
            Type = GithubReleaseAssetType.Binary,
        };

        // Act
        var result = _validator.ValidateGithubRelease(item, "profiles.default.install.github[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("binary", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_GithubTypeOmitted_MissingBinary_ReturnsError()
    {
        // Arrange — default type (Binary) requires binary field
        var item = new GithubReleaseItem
        {
            Repo = "owner/repo",
            Asset = "*.tar.gz",
        };

        // Act
        var result = _validator.ValidateGithubRelease(item, "profiles.default.install.github[0]");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Path.Contains("binary", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_GithubTypeDeb_NoBinary_ReturnsSuccess()
    {
        // Arrange — type: deb does NOT require binary
        var item = new GithubReleaseItem
        {
            Repo = "jgraph/drawio-desktop",
            Asset = "drawio-arm64-*.deb",
            Type = GithubReleaseAssetType.Deb,
        };

        // Act
        var result = _validator.ValidateGithubRelease(item, "profiles.default.install.github[0]");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_GithubTypeDeb_WithBinary_ReturnsSuccess()
    {
        // Arrange — type: deb with binary field present should still pass (ignored)
        var item = new GithubReleaseItem
        {
            Repo = "jgraph/drawio-desktop",
            Asset = "drawio-arm64-*.deb",
            Type = GithubReleaseAssetType.Deb,
            Binary = "drawio",
        };

        // Act
        var result = _validator.ValidateGithubRelease(item, "profiles.default.install.github[0]");

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
