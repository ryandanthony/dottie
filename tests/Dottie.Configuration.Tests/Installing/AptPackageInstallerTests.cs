// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Tests.Fakes;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Installing;

/// <summary>
/// Tests for <see cref="AptPackageInstaller"/>.
/// </summary>
public class AptPackageInstallerTests
{
    private readonly AptPackageInstaller _installer = new();

    [Fact]
    public void SourceType_ReturnsAptPackage()
    {
        // Act
        var result = _installer.SourceType;

        // Assert
        result.Should().Be(InstallSourceType.AptPackage);
    }

    [Fact]
    public async Task InstallAsync_WithEmptyPackageList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock();
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithValidContext_DoesNotThrow()
    {
        // Arrange
        var installBlock = new InstallBlock();
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        var action = async () => await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InstallAsync_WithDryRun_SkipsInstallation()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Apt = new List<string> { "git", "curl" }
        };
        var context = new InstallContext { RepoRoot = "/repo", DryRun = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithoutSudo_ReturnsWarningResults()
    {
        // Arrange
        var installBlock = new InstallBlock
        {
            Apt = new List<string> { "git", "curl" }
        };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = false
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Status.Should().Be(InstallStatus.Warning));
    }

    [Fact]
    public async Task InstallAsync_WithEmptyPackageList_ReturnsEmptyResults_WhenAptIsEmptyList()
    {
        // Arrange
        var installBlock = new InstallBlock { Apt = new List<string>() };
        var context = new InstallContext
        {
            RepoRoot = "/repo",
            HasSudo = true
        };

        // Act
        var results = await _installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InstallAsync_WithNullInstallBlock_ThrowsArgumentNullException()
    {
        // Arrange
        var context = new InstallContext { RepoRoot = "/repo" };

        // Act
        Func<Task> act = async () => await _installer.InstallAsync(null!, context);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("installBlock");
    }

    [Fact]
    public async Task InstallAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var installBlock = new InstallBlock();

        // Act
        Func<Task> act = async () => await _installer.InstallAsync(installBlock, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task InstallAsync_WithNullAptList_ReturnsEmptyResults()
    {
        // Arrange
        var installBlock = new InstallBlock { Apt = null };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await _installer.InstallAsync(installBlock, context);

        // Assert
        results.Should().BeEmpty();
    }

    #region Tests using FakeProcessRunner

    [Fact]
    public async Task InstallAsync_WithSudo_RunsAptGetUpdate()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult(); // apt-get update succeeds

        var installer = new AptPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Apt = new List<string> { "git" }
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        fakeRunner.CallCount.Should().BeGreaterThanOrEqualTo(1);
        fakeRunner.Calls[0].FileName.Should().Be("sudo");
        fakeRunner.Calls[0].Arguments.Should().Be("apt-get update");
    }

    [Fact]
    public async Task InstallAsync_WithSudo_InstallsEachPackage()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()  // apt-get update
            .WithSuccessResult()  // git install
            .WithSuccessResult(); // curl install

        var installer = new AptPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Apt = new List<string> { "git", "curl" }
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Status.Should().Be(InstallStatus.Success));
        fakeRunner.Calls.Should().Contain(c => c.Arguments == "apt-get install -y git");
        fakeRunner.Calls.Should().Contain(c => c.Arguments == "apt-get install -y curl");
    }

    [Fact]
    public async Task InstallAsync_WhenPackageInstallFails_ReturnsFailedResult()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()            // apt-get update
            .WithFailureResult(100, "E: Unable to locate package invalid-pkg"); // package install fails

        var installer = new AptPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Apt = new List<string> { "invalid-pkg" }
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Failed);
        results.First().ItemName.Should().Be("invalid-pkg");
        results.First().Message.Should().Contain("exit code 100");
    }

    [Fact]
    public async Task InstallAsync_WhenAptUpdateFails_StillAttemptsPackageInstall()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithFailureResult(1, "apt-get update failed")  // apt-get update fails
            .WithSuccessResult();                           // package install succeeds

        var installer = new AptPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Apt = new List<string> { "git" }
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results.First().Status.Should().Be(InstallStatus.Success);
        fakeRunner.CallCount.Should().Be(2); // Both update and install were called
    }

    [Fact]
    public async Task InstallAsync_WithMultiplePackages_ReportsPartialSuccess()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()                    // apt-get update
            .WithSuccessResult()                    // git succeeds
            .WithFailureResult(100, "not found")   // invalid-pkg fails
            .WithSuccessResult();                   // curl succeeds

        var installer = new AptPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Apt = new List<string> { "git", "invalid-pkg", "curl" }
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        var results = await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        results.Should().HaveCount(3);
        results.Where(r => r.Status == InstallStatus.Success).Should().HaveCount(2);
        results.Where(r => r.Status == InstallStatus.Failed).Should().HaveCount(1);
        results.Single(r => r.Status == InstallStatus.Failed).ItemName.Should().Be("invalid-pkg");
    }

    [Fact]
    public void Constructor_WithNullProcessRunner_CreatesDefaultRunner()
    {
        // Act
        var installer = new AptPackageInstaller(null);

        // Assert
        installer.Should().NotBeNull();
        installer.SourceType.Should().Be(InstallSourceType.AptPackage);
    }

    [Fact]
    public async Task InstallAsync_WithCustomProcessRunner_UsesProvidedRunner()
    {
        // Arrange
        var fakeRunner = new FakeProcessRunner()
            .WithSuccessResult()
            .WithSuccessResult();

        var installer = new AptPackageInstaller(fakeRunner);
        var installBlock = new InstallBlock
        {
            Apt = new List<string> { "vim" }
        };
        var context = new InstallContext { RepoRoot = "/repo", HasSudo = true };

        // Act
        await installer.InstallAsync(installBlock, context, CancellationToken.None);

        // Assert
        fakeRunner.CallCount.Should().Be(2); // Update + 1 package
    }

    #endregion
}
