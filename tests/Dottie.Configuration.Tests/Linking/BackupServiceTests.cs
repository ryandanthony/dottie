// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Linking;
using FluentAssertions;

namespace Dottie.Configuration.Tests.Linking;

/// <summary>
/// Tests for <see cref="BackupService"/>.
/// </summary>
public sealed class BackupServiceTests : IDisposable
{
    private readonly string _testDir;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupServiceTests"/> class.
    /// </summary>
    public BackupServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "dottie-backup-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }

        _disposed = true;
    }

    [Fact]
    public void Backup_WhenFileExists_CreatesBackupWithTimestamp()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, "original content");
        var service = new BackupService();

        // Act
        var result = service.Backup(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.OriginalPath.Should().Be(filePath);
        result.BackupPath.Should().NotBeNull();
        result.BackupPath.Should().MatchRegex(@"test\.txt\.dottie-backup-\d{8}-\d{6}$");
        File.Exists(result.BackupPath).Should().BeTrue();
        File.Exists(filePath).Should().BeFalse(); // Original moved
    }

    [Fact]
    public void Backup_WhenDirectoryExists_CreatesBackupWithTimestamp()
    {
        // Arrange
        var dirPath = Path.Combine(_testDir, "testdir");
        Directory.CreateDirectory(dirPath);
        File.WriteAllText(Path.Combine(dirPath, "file.txt"), "content");
        var service = new BackupService();

        // Act
        var result = service.Backup(dirPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.OriginalPath.Should().Be(dirPath);
        result.BackupPath.Should().NotBeNull();
        result.BackupPath.Should().MatchRegex(@"testdir\.dottie-backup-\d{8}-\d{6}$");
        Directory.Exists(result.BackupPath).Should().BeTrue();
        Directory.Exists(dirPath).Should().BeFalse(); // Original moved
    }

    [Fact]
    public void Backup_WhenBackupNameExists_AppendsNumericSuffix()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, "original content");
        var service = new BackupService();

        // Create first backup
        var firstResult = service.Backup(filePath);
        firstResult.IsSuccess.Should().BeTrue();

        // Recreate file for second backup
        File.WriteAllText(filePath, "new content");

        // Act - second backup should get numeric suffix if timestamp collision
        var secondResult = service.Backup(filePath);

        // Assert
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.BackupPath.Should().NotBeNull();
        secondResult.BackupPath.Should().NotBe(firstResult.BackupPath);
    }

    [Fact]
    public void Backup_PreservesOriginalContent()
    {
        // Arrange
        var originalContent = "This is the original content to preserve";
        var filePath = Path.Combine(_testDir, "preserve.txt");
        File.WriteAllText(filePath, originalContent);
        var service = new BackupService();

        // Act
        var result = service.Backup(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var backupContent = File.ReadAllText(result.BackupPath!);
        backupContent.Should().Be(originalContent);
    }

    [Fact]
    public void Backup_WhenPathDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDir, "nonexistent.txt");
        var service = new BackupService();

        // Act
        var result = service.Backup(nonExistentPath);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Backup_WhenFileExists_CreatesBackupWithDottieBackupNamingConvention()
    {
        // Arrange
        var filePath = Path.Combine(_testDir, "naming-test.txt");
        File.WriteAllText(filePath, "test content");
        var service = new BackupService();

        // Act
        var result = service.Backup(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.BackupPath.Should().NotBeNull();

        // Should use .dottie-backup-YYYYMMDD-HHMMSS format per spec FR-022
        result.BackupPath.Should().MatchRegex(@"naming-test\.txt\.dottie-backup-\d{8}-\d{6}$");
    }

    [Fact]
    public void Backup_WhenDirectoryExists_CreatesBackupWithDottieBackupNamingConvention()
    {
        // Arrange
        var dirPath = Path.Combine(_testDir, "naming-test-dir");
        Directory.CreateDirectory(dirPath);
        File.WriteAllText(Path.Combine(dirPath, "file.txt"), "content");
        var service = new BackupService();

        // Act
        var result = service.Backup(dirPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.BackupPath.Should().NotBeNull();

        // Should use .dottie-backup-YYYYMMDD-HHMMSS format per spec FR-022
        result.BackupPath.Should().MatchRegex(@"naming-test-dir\.dottie-backup-\d{8}-\d{6}$");
    }
}
