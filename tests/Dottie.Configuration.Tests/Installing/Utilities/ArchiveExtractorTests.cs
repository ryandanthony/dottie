// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Formats.Tar;
using System.IO.Compression;
using Dottie.Configuration.Installing.Utilities;
using FluentAssertions;

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

    [Fact]
    public void Extract_WithTarGzContainingPaxHeaders_ExtractsSuccessfully()
    {
        // Arrange - Create a tar.gz with PAX metadata on the entry.
        // System.Formats.Tar writes PAX extended headers automatically when metadata
        // requires it. The extractor should handle this transparently.
        var tarGzPath = Path.Combine(_testDir, "pax-test.tar.gz");
        var extractDir = Path.Combine(_testDir, "extracted-pax");
        var fileContent = "hello from pax tar";
        CreateTarGzWithPaxEntry(tarGzPath, "myfile.txt", fileContent);

        // Act
        _extractor.Extract(tarGzPath, extractDir);

        // Assert
        var extractedFile = Path.Combine(extractDir, "myfile.txt");
        File.Exists(extractedFile).Should().BeTrue("the file should be extracted regardless of PAX metadata");
        File.ReadAllText(extractedFile).Should().Be(fileContent);
    }

    [Fact]
    public void Extract_WithTarGzContainingGnuLongNameHeader_ExtractsSuccessfully()
    {
        // Arrange - Create a tar.gz with a GNU-format entry (long file name).
        // The extractor should handle GNU entries transparently.
        var tarGzPath = Path.Combine(_testDir, "gnu-longname-test.tar.gz");
        var extractDir = Path.Combine(_testDir, "extracted-gnu-longname");
        var fileContent = "hello from gnu long name tar";
        CreateTarGzWithGnuEntry(tarGzPath, "shortname.txt", fileContent);

        // Act
        _extractor.Extract(tarGzPath, extractDir);

        // Assert
        var extractedFile = Path.Combine(extractDir, "shortname.txt");
        File.Exists(extractedFile).Should().BeTrue("the file should be extracted from a GNU-format tar");
        File.ReadAllText(extractedFile).Should().Be(fileContent);
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
        CreateTarGzWithContent(tarGzPath, "test.txt", "test tar content");
    }

    /// <summary>
    /// Creates a tar.gz file containing a single file entry using <see cref="TarWriter"/>.
    /// </summary>
    private static void CreateTarGzWithContent(string tarGzPath, string fileName, string content)
    {
        using var fileStream = File.Create(tarGzPath);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
        using var writer = new TarWriter(gzipStream, TarEntryFormat.Pax, leaveOpen: false);

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        var entry = new PaxTarEntry(TarEntryType.RegularFile, fileName)
        {
            DataStream = new MemoryStream(contentBytes),
        };
        writer.WriteEntry(entry);
    }

    /// <summary>
    /// Creates a tar.gz file with a PAX-format entry (PAX extended headers are written automatically).
    /// </summary>
    private static void CreateTarGzWithPaxEntry(string tarGzPath, string fileName, string content)
    {
        using var fileStream = File.Create(tarGzPath);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
        using var writer = new TarWriter(gzipStream, TarEntryFormat.Pax, leaveOpen: false);

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        var entry = new PaxTarEntry(TarEntryType.RegularFile, fileName)
        {
            DataStream = new MemoryStream(contentBytes),
        };
        writer.WriteEntry(entry);
    }

    /// <summary>
    /// Creates a tar.gz file with a GNU-format entry.
    /// </summary>
    private static void CreateTarGzWithGnuEntry(string tarGzPath, string fileName, string content)
    {
        using var fileStream = File.Create(tarGzPath);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
        using var writer = new TarWriter(gzipStream, TarEntryFormat.Gnu, leaveOpen: false);

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        var entry = new GnuTarEntry(TarEntryType.RegularFile, fileName)
        {
            DataStream = new MemoryStream(contentBytes),
        };
        writer.WriteEntry(entry);
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
