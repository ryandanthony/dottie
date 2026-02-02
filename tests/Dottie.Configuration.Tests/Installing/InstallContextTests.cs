// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="InstallContext"/>.
/// </summary>
public class InstallContextTests
{
    [Fact]
    public void Create_WithRequiredFields_Succeeds()
    {
        // Arrange
        var repoRoot = "/home/user/dotfiles";

        // Act
        var context = new InstallContext { RepoRoot = repoRoot };

        // Assert
        context.RepoRoot.Should().Be(repoRoot);
        context.BinDirectory.Should().Contain("bin");
        context.FontDirectory.Should().Contain(".local");
        context.FontDirectory.Should().Contain("share");
        context.FontDirectory.Should().Contain("fonts");
        context.GithubToken.Should().BeNull();
        context.HasSudo.Should().BeFalse();
        context.DryRun.Should().BeFalse();
    }

    [Fact]
    public void Create_WithCustomBinDirectory_OverridesDefault()
    {
        // Arrange
        var repoRoot = "/home/user/dotfiles";
        var customBin = "/usr/local/bin";

        // Act
        var context = new InstallContext
        {
            RepoRoot = repoRoot,
            BinDirectory = customBin,
        };

        // Assert
        context.BinDirectory.Should().Be(customBin);
    }

    [Fact]
    public void Create_WithCustomFontDirectory_OverridesDefault()
    {
        // Arrange
        var repoRoot = "/home/user/dotfiles";
        var customFonts = "/usr/share/fonts";

        // Act
        var context = new InstallContext
        {
            RepoRoot = repoRoot,
            FontDirectory = customFonts,
        };

        // Assert
        context.FontDirectory.Should().Be(customFonts);
    }

    [Fact]
    public void Create_WithAllFields_Succeeds()
    {
        // Arrange
        var repoRoot = "/home/user/dotfiles";
        var binDir = "/custom/bin";
        var fontDir = "/custom/fonts";
        var token = "ghp_test_token";

        // Act
        var context = new InstallContext
        {
            RepoRoot = repoRoot,
            BinDirectory = binDir,
            FontDirectory = fontDir,
            GithubToken = token,
            HasSudo = true,
            DryRun = true,
        };

        // Assert
        context.RepoRoot.Should().Be(repoRoot);
        context.BinDirectory.Should().Be(binDir);
        context.FontDirectory.Should().Be(fontDir);
        context.GithubToken.Should().Be(token);
        context.HasSudo.Should().BeTrue();
        context.DryRun.Should().BeTrue();
    }

    [Fact]
    public void BinDirectory_DefaultValue_PointsToUserHome()
    {
        // Arrange
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var repoRoot = "/home/user/dotfiles";

        // Act
        var context = new InstallContext { RepoRoot = repoRoot };

        // Assert
        context.BinDirectory.Should().StartWith(homeDir);
        context.BinDirectory.Should().EndWith("bin");
    }

    [Fact]
    public void FontDirectory_DefaultValue_PointsToUserHome()
    {
        // Arrange
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var repoRoot = "/home/user/dotfiles";

        // Act
        var context = new InstallContext { RepoRoot = repoRoot };

        // Assert
        context.FontDirectory.Should().StartWith(homeDir);
        context.FontDirectory.Should().Contain(".local");
        context.FontDirectory.Should().EndWith("fonts");
    }

    [Fact]
    public void Create_WithDryRunTrue_AllowsPreview()
    {
        // Arrange
        var repoRoot = "/home/user/dotfiles";

        // Act
        var context = new InstallContext
        {
            RepoRoot = repoRoot,
            DryRun = true,
        };

        // Assert
        context.DryRun.Should().BeTrue();
    }

    [Fact]
    public void Create_WithSudoTrue_IndicatesPrivilegeAvailability()
    {
        // Arrange
        var repoRoot = "/home/user/dotfiles";

        // Act
        var context = new InstallContext
        {
            RepoRoot = repoRoot,
            HasSudo = true,
        };

        // Assert
        context.HasSudo.Should().BeTrue();
    }

    [Fact]
    public void InstallContext_IsRecord_SupportedEquality()
    {
        // Arrange
        var context1 = new InstallContext
        {
            RepoRoot = "/repo",
            BinDirectory = "/bin",
            HasSudo = true,
        };
        var context2 = new InstallContext
        {
            RepoRoot = "/repo",
            BinDirectory = "/bin",
            HasSudo = true,
        };
        var context3 = new InstallContext
        {
            RepoRoot = "/repo",
            BinDirectory = "/bin",
            HasSudo = false,
        };

        // Act & Assert
        context1.Should().Be(context2);
        context1.Should().NotBe(context3);
    }
}
