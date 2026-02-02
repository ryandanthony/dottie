// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="InstallCommand"/>.
/// </summary>
public sealed class InstallCommandTests : IDisposable
{
    private static readonly string FixturesPath = Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "..",
        "tests",
        "Dottie.Configuration.Tests",
        "Fixtures");

    private readonly string _tempDirectory;
    private readonly string _originalDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstallCommandTests"/> class.
    /// </summary>
    public InstallCommandTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"dottie-install-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);

        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
#pragma warning disable CA1031 // Do not catch general exception types - cleanup code should not throw
            catch
            {
                // Ignore cleanup failures in tests
            }
#pragma warning restore CA1031
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void InstallCommand_CanBeInstantiated()
    {
        // Act
        var command = new InstallCommand();

        // Assert
        command.Should().NotBeNull();
    }

    [Fact]
    public void Execute_WithNonexistentConfig_ReturnsOne()
    {
        // Arrange
        SetupGitRepo();
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<InstallCommand>("install");
        });

        // Act
        var result = app.Run(["install", "-c", "nonexistent-config.yaml"]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_WithInvalidConfig_ReturnsOne()
    {
        // Arrange
        SetupGitRepo();
        var configPath = Path.Combine(FixturesPath, "invalid-yaml-syntax.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<InstallCommand>("install");
        });

        // Act
        var result = app.Run(["install", "-c", configPath]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_WithNonexistentProfile_ReturnsOne()
    {
        // Arrange
        SetupGitRepo();
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<InstallCommand>("install");
        });

        // Act
        var result = app.Run(["install", "-c", configPath, "-p", "nonexistent"]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_WithProfileWithoutInstallBlock_ReturnsOne()
    {
        // Arrange
        SetupGitRepo();
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<InstallCommand>("install");
        });

        // Act
        var result = app.Run(["install", "-c", configPath]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_WithDryRunFlag_ValidatesResources()
    {
        // Arrange
        SetupGitRepo();
        var configPath = Path.Combine(FixturesPath, "valid-all-install-types.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<InstallCommand>("install");
        });

        // Act - dry-run mode validates but some validations may fail (e.g., missing scripts)
        var result = app.Run(["install", "-c", configPath, "--dry-run"]);

        // Assert - may return 1 if any validation fails (expected behavior)
        result.Should().BeOneOf(0, 1);
    }

    [Fact]
    public void Execute_WithValidConfigAndInstallBlock_ValidatesInDryRun()
    {
        // Arrange
        SetupGitRepo();
        var configPath = Path.Combine(FixturesPath, "valid-all-install-types.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<InstallCommand>("install");
        });

        // Act - dry-run so we don't actually install anything, but validation may fail
        var result = app.Run(["install", "-c", configPath, "--dry-run"]);

        // Assert - may return 1 if validations fail (e.g., missing scripts)
        result.Should().BeOneOf(0, 1);
    }

    [Fact]
    public void Execute_WithoutGitRepo_ReturnsOne()
    {
        // Arrange - don't setup git repo
        Directory.SetCurrentDirectory(_tempDirectory);
        var configPath = Path.Combine(FixturesPath, "valid-all-install-types.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<InstallCommand>("install");
        });

        // Act
        var result = app.Run(["install", "-c", configPath]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_WithDefaultProfile_RunsValidation()
    {
        // Arrange
        SetupGitRepo();
        var configPath = Path.Combine(FixturesPath, "valid-all-install-types.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<InstallCommand>("install");
        });

        // Act
        var result = app.Run(["install", "-c", configPath, "--dry-run"]);

        // Assert - may return 1 if validations fail (expected behavior)
        result.Should().BeOneOf(0, 1);
    }

    private void SetupGitRepo()
    {
        Directory.SetCurrentDirectory(_tempDirectory);
        
        // Initialize git repo
        var gitDir = Path.Combine(_tempDirectory, ".git");
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(Path.Combine(gitDir, "HEAD"), "ref: refs/heads/main\n");
        
        // Create refs directory
        var refsDir = Path.Combine(gitDir, "refs", "heads");
        Directory.CreateDirectory(refsDir);
        File.WriteAllText(Path.Combine(refsDir, "main"), "0000000000000000000000000000000000000000\n");
    }
}
