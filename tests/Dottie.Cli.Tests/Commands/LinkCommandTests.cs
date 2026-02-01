// -----------------------------------------------------------------------
// <copyright file="LinkCommandTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Cli.Commands;
using FluentAssertions;
using Spectre.Console.Cli;

namespace Dottie.Cli.Tests.Commands;

/// <summary>
/// Tests for <see cref="LinkCommand"/>.
/// </summary>
public sealed class LinkCommandTests : IDisposable
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
    /// Initializes a new instance of the <see cref="LinkCommandTests"/> class.
    /// </summary>
    public LinkCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"dottie-link-tests-{Guid.NewGuid():N}");
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
            config.AddCommand<LinkCommand>("link");
        });

        // Act
        var result = app.Run(["link", "-c", "nonexistent-config.yaml"]);

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
            config.AddCommand<LinkCommand>("link");
        });

        // Act
        var result = app.Run(["link", "-c", configPath]);

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
            config.AddCommand<LinkCommand>("link");
        });

        // Act
        var result = app.Run(["link", "-c", configPath, "--profile", "nonexistent"]);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void Execute_WhenConflictsExist_ReturnsOneWithConflictList()
    {
        // Arrange
        var configContent = @"
profiles:
  default:
    dotfiles:
      - source: test.txt
        target: ~/test-target.txt
";
        var configPath = Path.Combine(_tempDirectory, "dottie.yaml");
        File.WriteAllText(configPath, configContent);

        // Create source file in temp directory (simulating repo root)
        var sourceFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(sourceFile, "source content");

        // Create conflicting file at target
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var targetPath = Path.Combine(homeDir, "test-target.txt");

        // Only run if we can create the target file
        if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
        {
            return;
        }

        try
        {
            File.WriteAllText(targetPath, "existing content");

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.PropagateExceptions();
                config.AddCommand<LinkCommand>("link");
            });

            // Act - this should detect conflict and return 1
            var result = app.Run(["link", "-c", configPath]);

            // Assert - conflict detected, returns 1
            result.Should().Be(1);
        }
        finally
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }
    }

    [Fact]
    public void Execute_WhenConflictsExist_DoesNotModifyFiles()
    {
        // Arrange
        var configContent = @"
profiles:
  default:
    dotfiles:
      - source: test.txt
        target: ~/test-no-modify.txt
";
        var configPath = Path.Combine(_tempDirectory, "dottie.yaml");
        File.WriteAllText(configPath, configContent);

        // Create source file in temp directory
        var sourceFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(sourceFile, "source content");

        // Create conflicting file at target
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var targetPath = Path.Combine(homeDir, "test-no-modify.txt");

        // Only run if we can create the target file
        if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
        {
            return;
        }

        var originalContent = "original content - should not change";

        try
        {
            File.WriteAllText(targetPath, originalContent);

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.PropagateExceptions();
                config.AddCommand<LinkCommand>("link");
            });

            // Act - run without --force
            _ = app.Run(["link", "-c", configPath]);

            // Assert - file should be unchanged
            var fileContent = File.ReadAllText(targetPath);
            fileContent.Should().Be(originalContent);

            // Also verify it's still a regular file, not a symlink
            var attributes = File.GetAttributes(targetPath);
            attributes.Should().NotHaveFlag(FileAttributes.ReparsePoint);
        }
        finally
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }
    }

    [Fact]
    public void Execute_WithEmptyProfile_ReturnsZero()
    {
        // Arrange - profile with no dotfiles
        var configContent = @"
profiles:
  empty:
    dotfiles: []
";
        var configPath = Path.Combine(_tempDirectory, "dottie.yaml");
        File.WriteAllText(configPath, configContent);

        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<LinkCommand>("link");
        });

        // Act
        var result = app.Run(["link", "-c", configPath, "--profile", "empty"]);

        // Assert - empty profile should succeed (no work to do)
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_WithForceFlag_AcceptsFlag()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<LinkCommand>("link");
        });

        // Act - just verify the --force flag is accepted (not testing actual functionality here)
        // This will fail because the source files don't exist, but we're testing flag parsing
        var result = app.Run(["link", "-c", configPath, "--force"]);

        // Assert - returns 1 because source files don't exist, but flag was accepted
        result.Should().BeOneOf(0, 1); // Either succeeds or fails for valid reasons
    }

    [Fact]
    public void Execute_WithForceFlagShortForm_AcceptsFlag()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<LinkCommand>("link");
        });

        // Act - test short form -f
        var result = app.Run(["link", "-c", configPath, "-f"]);

        // Assert - returns 1 because source files don't exist, but flag was accepted
        result.Should().BeOneOf(0, 1);
    }

    [Fact]
    public void Execute_WithDryRunFlag_AcceptsFlag()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<LinkCommand>("link");
        });

        // Act - test --dry-run flag
        var result = app.Run(["link", "-c", configPath, "--dry-run"]);

        // Assert - should succeed (dry-run always succeeds if config is valid)
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_WithDryRunFlagShortForm_AcceptsFlag()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.AddCommand<LinkCommand>("link");
        });

        // Act - test short form -d
        var result = app.Run(["link", "-c", configPath, "-d"]);

        // Assert - should succeed (dry-run always succeeds if config is valid)
        result.Should().Be(0);
    }

    [Fact]
    public void Execute_WithDryRunAndForce_ReturnsError()
    {
        // Arrange
        var configPath = Path.Combine(FixturesPath, "valid-minimal.yaml");
        var app = new CommandApp();
        app.Configure(config =>
        {
            // Don't propagate exceptions - we want to see the validation error as exit code
            config.AddCommand<LinkCommand>("link");
        });

        // Act - test mutually exclusive flags
        var result = app.Run(["link", "-c", configPath, "--dry-run", "--force"]);

        // Assert - validation fails, returns non-zero (Spectre.Console returns -1 for validation errors)
        result.Should().NotBe(0);
    }

    [Fact]
    public void Execute_WithDryRun_DoesNotCreateSymlinks()
    {
        // Arrange
        var configContent = @"
profiles:
  default:
    dotfiles:
      - source: test-dry-run.txt
        target: ~/test-dry-run-target.txt
";
        var configPath = Path.Combine(_tempDirectory, "dottie.yaml");
        File.WriteAllText(configPath, configContent);

        // Create source file in temp directory (simulating repo root)
        var sourceFile = Path.Combine(_tempDirectory, "test-dry-run.txt");
        File.WriteAllText(sourceFile, "source content");

        // Target path
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var targetPath = Path.Combine(homeDir, "test-dry-run-target.txt");

        // Make sure target doesn't exist before test
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        try
        {
            var app = new CommandApp();
            app.Configure(config =>
            {
                config.PropagateExceptions();
                config.AddCommand<LinkCommand>("link");
            });

            // Act - run with --dry-run
            var result = app.Run(["link", "-c", configPath, "--dry-run"]);

            // Assert - should succeed and NOT create the symlink
            result.Should().Be(0);
            File.Exists(targetPath).Should().BeFalse("dry-run should not create symlinks");
        }
        finally
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }
    }
}
