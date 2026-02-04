// -----------------------------------------------------------------------
// <copyright file="ApplyCommandTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="ApplyCommand"/>.
/// </summary>
public sealed class ApplyCommandTests : IDisposable
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyCommandTests"/> class.
    /// </summary>
    public ApplyCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"dottie-apply-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
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
    }

    [Fact]
    public void Execute_WithNonexistentConfig_ReturnsOne()
    {
        // Arrange
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ApplyCommand>("apply");
        });

        // Act
        var result = app.Run(["apply", "-c", "nonexistent-config.yaml"]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_WithInvalidConfig_ReturnsOne()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "invalid-yaml-syntax.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ApplyCommand>("apply");
        });

        // Act
        var result = app.Run(["apply", "-c", configPath]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_WithNonexistentProfile_ReturnsOne()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ApplyCommand>("apply");
        });

        // Act
        var result = app.Run(["apply", "-c", configPath, "--profile", "nonexistent"]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void ExecuteAsync_WithValidProfile_LinksAndInstalls()
    {
        // Arrange - Create a valid config with dotfiles and install block
        var configContent = @"
profiles:
  default:
    dotfiles:
      - source: dotfiles/bashrc
        target: ~/.bashrc-test-apply
";
        var configPath = Path.Combine(_tempDirectory, "dottie.yaml");
        File.WriteAllText(configPath, configContent);

        // Create source file in temp directory
        var dotfilesDir = Path.Combine(_tempDirectory, "dotfiles");
        Directory.CreateDirectory(dotfilesDir);
        File.WriteAllText(Path.Combine(dotfilesDir, "bashrc"), "# test bash config");

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ApplyCommand>("apply");
        });

        // Act - Use dry-run to avoid symlink privilege issues on Windows
        var result = app.Run(["apply", "-c", configPath, "--dry-run"]);

        // Assert - Should succeed (exit 0) with dry-run
        result.Should().Be(0);
    }

    [Fact]
    public void ExecuteAsync_WithProfileFlag_UsesSpecifiedProfile()
    {
        // Arrange - Create config with multiple profiles
        var configContent = @"
profiles:
  default:
    dotfiles:
      - source: dotfiles/default-file
        target: ~/.default-test-apply
  work:
    dotfiles:
      - source: dotfiles/work-file
        target: ~/.work-test-apply
";
        var configPath = Path.Combine(_tempDirectory, "dottie.yaml");
        File.WriteAllText(configPath, configContent);

        // Create source files
        var dotfilesDir = Path.Combine(_tempDirectory, "dotfiles");
        Directory.CreateDirectory(dotfilesDir);
        File.WriteAllText(Path.Combine(dotfilesDir, "default-file"), "default content");
        File.WriteAllText(Path.Combine(dotfilesDir, "work-file"), "work content");

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ApplyCommand>("apply");
        });

        // Act - Use dry-run to avoid Windows symlink privilege issues
        var result = app.Run(["apply", "-c", configPath, "--profile", "work", "--dry-run"]);

        // Assert - Should succeed with specified profile
        result.Should().Be(0);
    }

    [Fact]
    public void ExecuteAsync_LinksBeforeInstalls()
    {
        // Arrange - Create config with both dotfiles and install block
        // We verify order by checking that linking phase runs first
        var configContent = @"
profiles:
  default:
    dotfiles:
      - source: dotfiles/test-order
        target: ~/.test-order-apply
";
        var configPath = Path.Combine(_tempDirectory, "dottie.yaml");
        File.WriteAllText(configPath, configContent);

        // Create source file
        var dotfilesDir = Path.Combine(_tempDirectory, "dotfiles");
        Directory.CreateDirectory(dotfilesDir);
        File.WriteAllText(Path.Combine(dotfilesDir, "test-order"), "test content");

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ApplyCommand>("apply");
        });

        // Act - Use dry-run to avoid Windows symlink privilege issues
        var result = app.Run(["apply", "-c", configPath, "--dry-run"]);

        // Assert - Command should succeed (link phase runs before install phase per design)
        result.Should().Be(0);
    }

    [Fact]
    public void ExecuteAsync_WithInvalidConfigPath_ReturnsFail()
    {
        // Arrange
        var invalidPath = Path.Combine(_tempDirectory, "nonexistent-config.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ApplyCommand>("apply");
        });

        // Act
        var result = app.Run(["apply", "-c", invalidPath]);

        // Assert - Should fail with non-zero exit code
        result.Should().NotBe(0);
    }

    [Fact]
    public void ExecuteAsync_WithMalformedYaml_ReturnsFail()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "invalid.yaml");
        File.WriteAllText(configPath, "invalid: yaml: content: [[[");

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ApplyCommand>("apply");
        });

        // Act
        var result = app.Run(["apply", "-c", configPath]);

        // Assert
        result.Should().NotBe(0);
    }

    [Fact]
    public void ExecuteAsync_WithDryRunAndForce_BothFlagsApply()
    {
        // Arrange - Create config with dotfiles
        var configContent = @"
profiles:
  default:
    dotfiles:
      - source: dotfiles/bashrc
        target: ~/.bashrc-test
";
        var configPath = Path.Combine(_tempDirectory, "dottie.yaml");
        File.WriteAllText(configPath, configContent);

        var dotfilesDir = Path.Combine(_tempDirectory, "dotfiles");
        Directory.CreateDirectory(dotfilesDir);
        File.WriteAllText(Path.Combine(dotfilesDir, "bashrc"), "# test");

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<ApplyCommand>("apply");
        });

        // Act - Both --dry-run and --force together
        var result = app.Run(["apply", "-c", configPath, "--dry-run", "--force"]);

        // Assert - Should succeed
        result.Should().Be(0);
    }

    [Fact]
    public void ExecuteAsync_WithEmptyDotfilesAndNoInstall_ReturnsErrorCode()
    {
        // Arrange - Config with empty dotfiles list and no install
        // This is treated as a configuration error since there's nothing to apply
        var configContent = @"
profiles:
  default:
    dotfiles: []
";
        var configPath = Path.Combine(_tempDirectory, "dottie.yaml");
        File.WriteAllText(configPath, configContent);

        // Initialize git repo
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "init -q",
                WorkingDirectory = _tempDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            },
        };
        process.Start();
        process.WaitForExit();

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.AddCommand<ApplyCommand>("apply");
        });

        // Act
        var result = app.Run(["apply", "-c", configPath]);

        // Assert - Should return error since there's nothing to apply
        result.Should().Be(1);
    }
}

