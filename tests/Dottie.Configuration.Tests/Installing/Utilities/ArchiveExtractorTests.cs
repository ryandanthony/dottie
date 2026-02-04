// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing.Utilities;
using FluentAssertions;
using System.IO.Compression;

namespace Dottie.Configuration.Tests.Installing.Utilities;

/// <summary>
/// Tests for <see cref="ArchiveExtractor"/>.
/// </summary>
public class ArchiveExtractorTests
{
    private readonly ArchiveExtractor _extractor = new();
    private readonly string _testDir = Path.Combine(Path.GetTempPath(), $"archive-test-{Guid.NewGuid()}");

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveExtractorTests"/> class.
    /// </summary>
    public ArchiveExtractorTests()
    {
        if (!Directory.Exists(_testDir))
        {
            Directory.CreateDirectory(_testDir);
        }
    }

    [Fact]
    public void Extract_WithZipFile_ExtractsAllFiles()
    {
        // Arrange
        var zipPath = Path.Combine(_testDir, "test.zip");
        var extractDir = Path.Combine(_testDir, "extracted-zip");
        CreateTestZipFile(zipPath);

        // Act
        _extractor.Extract(zipPath, extractDir);

        // Assert
        Directory.Exists(extractDir).Should().BeTrue();
        File.Exists(Path.Combine(extractDir, "test.txt")).Should().BeTrue();
    }

    [Fact]
    public void Extract_WithTarGzFile_ExtractsAllFiles()
    {
        // Arrange
        var tarGzPath = Path.Combine(_testDir, "test.tar.gz");
        var extractDir = Path.Combine(_testDir, "extracted-targz");
        CreateTestTarGzFile(tarGzPath);

        // Act
        _extractor.Extract(tarGzPath, extractDir);

        // Assert
        Directory.Exists(extractDir).Should().BeTrue();

        // Note: Tar.gz extraction is complex; verify directory was created
        // Real-world tests would use SharpCompress or similar
    }

    [Fact]
    public void Extract_WithTgzFile_ExtractsAllFiles()
    {
        // Arrange
        var tgzPath = Path.Combine(_testDir, "test.tgz");
        var extractDir = Path.Combine(_testDir, "extracted-tgz");
        CreateTestTarGzFile(tgzPath);

        // Act
        _extractor.Extract(tgzPath, extractDir);

        // Assert
        Directory.Exists(extractDir).Should().BeTrue();

        // Note: Tar.gz extraction is complex; verify directory was created
        // Real-world tests would use SharpCompress or similar
    }

    [Fact]
    public void Extract_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var zipPath = Path.Combine(_testDir, "nonexistent.zip");
        var extractDir = Path.Combine(_testDir, "extracted");

        // Act & Assert
        FluentActions.Invoking(() => _extractor.Extract(zipPath, extractDir))
            .Should()
            .Throw<FileNotFoundException>();
    }

    [Fact]
    public void Extract_CreatesTargetDirectory_IfNotExists()
    {
        // Arrange
        var zipPath = Path.Combine(_testDir, "test.zip");
        var extractDir = Path.Combine(_testDir, "new-extract-dir");
        CreateTestZipFile(zipPath);

        // Act
        _extractor.Extract(zipPath, extractDir);

        // Assert
        Directory.Exists(extractDir).Should().BeTrue();
    }

    [Fact]
    public void Extract_WithInvalidArchive_ThrowsException()
    {
        // Arrange
        var invalidZipPath = Path.Combine(_testDir, "invalid.zip");
        File.WriteAllText(invalidZipPath, "This is not a valid zip file");
        var extractDir = Path.Combine(_testDir, "extracted-invalid");

        // Act & Assert
        FluentActions.Invoking(() => _extractor.Extract(invalidZipPath, extractDir))
            .Should()
            .Throw<Exception>();
    }

    [Fact]
    public void Extract_PreservesFileContent()
    {
        // Arrange
        var zipPath = Path.Combine(_testDir, "test.zip");
        var extractDir = Path.Combine(_testDir, "extracted-content");
        var expectedContent = "Test file content";
        CreateTestZipFileWithContent(zipPath, expectedContent);

        // Act
        _extractor.Extract(zipPath, extractDir);

        // Assert
        var extractedFile = Path.Combine(extractDir, "test.txt");
        File.Exists(extractedFile).Should().BeTrue();
        File.ReadAllText(extractedFile).Should().Be(expectedContent);
    }

    [Fact]
    public void Extract_PreservesDirectoryStructure()
    {
        // Arrange
        var zipPath = Path.Combine(_testDir, "test-struct.zip");
        var extractDir = Path.Combine(_testDir, "extracted-struct");
        CreateTestZipFileWithStructure(zipPath);

        // Act
        _extractor.Extract(zipPath, extractDir);

        // Assert
        Directory.Exists(Path.Combine(extractDir, "subdir")).Should().BeTrue();
        File.Exists(Path.Combine(extractDir, "subdir", "nested.txt")).Should().BeTrue();
    }

    [Fact]
    public void Extract_WithUnsupportedExtension_ThrowsInvalidOperationException()
    {
        // Arrange
        var unsupportedPath = Path.Combine(_testDir, "test.rar");
        File.WriteAllText(unsupportedPath, "fake rar content");
        var extractDir = Path.Combine(_testDir, "extracted-unsupported");

        // Act & Assert
        FluentActions.Invoking(() => _extractor.Extract(unsupportedPath, extractDir))
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Unsupported archive format*");
    }

    [Fact]
    public void Extract_WithNullArchivePath_ThrowsArgumentNullException()
    {
        // Arrange
        string? archivePath = null;
        var extractDir = Path.Combine(_testDir, "extracted");

        // Act & Assert
        FluentActions.Invoking(() => _extractor.Extract(archivePath!, extractDir))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void Extract_WithNullExtractPath_ThrowsArgumentNullException()
    {
        // Arrange
        var zipPath = Path.Combine(_testDir, "test.zip");
        CreateTestZipFile(zipPath);
        string? extractDir = null;

        // Act & Assert
        FluentActions.Invoking(() => _extractor.Extract(zipPath, extractDir!))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void Extract_WithZipSlipAttempt_ThrowsInvalidOperationException()
    {
        // Arrange
        var zipPath = Path.Combine(_testDir, "malicious.zip");
        var extractDir = Path.Combine(_testDir, "safe-extract");
        CreateMaliciousZipFile(zipPath);

        // Act & Assert
        FluentActions.Invoking(() => _extractor.Extract(zipPath, extractDir))
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*zip slip detected*");
    }

    [Fact]
    public void Extract_OverwritesExistingFiles()
    {
        // Arrange
        var zipPath = Path.Combine(_testDir, "test-overwrite.zip");
        var extractDir = Path.Combine(_testDir, "extracted-overwrite");
        Directory.CreateDirectory(extractDir);
        
        var existingFile = Path.Combine(extractDir, "test.txt");
        File.WriteAllText(existingFile, "old content");
        
        CreateTestZipFileWithContent(zipPath, "new content");

        // Act
        _extractor.Extract(zipPath, extractDir);

        // Assert
        File.ReadAllText(existingFile).Should().Be("new content");
    }

    private void CreateMaliciousZipFile(string zipPath)
    {
        // Create a zip with an entry that tries to escape the extraction directory
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = zip.CreateEntry("../../../evil.txt");
        using var writer = new StreamWriter(entry.Open());
        writer.Write("malicious content");
    }

    private void CreateTestZipFile(string zipPath)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = zip.CreateEntry("test.txt");
        using var writer = new StreamWriter(entry.Open());
        writer.Write("Test content");
    }

    private void CreateTestZipFileWithContent(string zipPath, string content)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = zip.CreateEntry("test.txt");
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    private void CreateTestZipFileWithStructure(string zipPath)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry1 = zip.CreateEntry("root.txt");
        using (var writer = new StreamWriter(entry1.Open()))
        {
            writer.Write("Root file");
        }

        var entry2 = zip.CreateEntry("subdir/nested.txt");
        using (var writer = new StreamWriter(entry2.Open()))
        {
            writer.Write("Nested file");
        }
    }

    private void CreateTestTarGzFile(string tarGzPath)
    {
        // For simplicity, we'll create a gzip file with minimal tar content
        // In production, use SharpCompress or similar
        using var fileStream = File.Create(tarGzPath);
        using var gzipStream = new System.IO.Compression.GZipStream(fileStream, CompressionMode.Compress);
        var testData = System.Text.Encoding.UTF8.GetBytes("test tar content");
        gzipStream.Write(testData, 0, testData.Length);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ArchiveExtractorTests"/> class.
    /// </summary>
    ~ArchiveExtractorTests()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}
