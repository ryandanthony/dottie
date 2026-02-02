// -----------------------------------------------------------------------
// <copyright file="ArchiveExtractor.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.IO.Compression;
using System.Text;

namespace Dottie.Configuration.Installing.Utilities;

/// <summary>
/// Extracts files from various archive formats (.zip, .tar.gz, .tgz).
/// </summary>
public class ArchiveExtractor
{
    /// <summary>
    /// Extracts an archive file to the specified directory.
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <param name="extractPath">Directory to extract files to.</param>
    /// <exception cref="FileNotFoundException">Thrown when the archive file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the archive format is not supported.</exception>
    /// <exception cref="IOException">Thrown when extraction fails.</exception>
    public void Extract(string archivePath, string extractPath)
    {
        ArgumentNullException.ThrowIfNull(archivePath);
        ArgumentNullException.ThrowIfNull(extractPath);

        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException($"Archive file not found: {archivePath}");
        }

        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(extractPath);
        }

        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ExtractZip(archivePath, extractPath);
        }
        else if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
                 || archivePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
        {
            ExtractTarGz(archivePath, extractPath);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported archive format: {Path.GetExtension(archivePath)}");
        }
    }

    /// <summary>
    /// Extracts files from a file in a Zip archive.
    /// </summary>
    /// <param name="zipPath">Path to the zip file.</param>
    /// <param name="extractPath">Directory to extract to.</param>
    private static void ExtractZip(string zipPath, string extractPath)
    {
        try
        {
            var fullExtractPath = Path.GetFullPath(extractPath);
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                // Sanitize entry name to prevent zip slip attacks
                var targetPath = Path.GetFullPath(Path.Combine(fullExtractPath, entry.FullName));
                if (!targetPath.StartsWith(fullExtractPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Zip entry '{entry.FullName}' would extract outside of the target directory (zip slip detected)");
                }

                var targetDir = Path.GetDirectoryName(targetPath);

                if (targetDir != null && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                if (!string.IsNullOrEmpty(entry.Name))
                {
                    entry.ExtractToFile(targetPath, overwrite: true);
                }
            }
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException($"Invalid zip file: {zipPath}", ex);
        }
    }

    /// <summary>
    /// Extracts files from a tar.gz or .tgz archive.
    /// </summary>
    /// <param name="tarGzPath">Path to the tar.gz or .tgz file.</param>
    /// <param name="extractPath">Directory to extract to.</param>
    private static void ExtractTarGz(string tarGzPath, string extractPath)
    {
        try
        {
            using var gzipStream = new GZipStream(File.OpenRead(tarGzPath), CompressionMode.Decompress);
            ExtractTarStream(gzipStream, extractPath);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException($"Invalid tar.gz file: {tarGzPath}", ex);
        }
    }

    /// <summary>
    /// Extracts files from a tar stream (used for .tar.gz and .tgz files).
    /// </summary>
    /// <param name="stream">The tar stream to extract from.</param>
    /// <param name="extractPath">Directory to extract to.</param>
    private static void ExtractTarStream(Stream stream, string extractPath)
    {
        const int blockSize = 512;
        var buffer = new byte[blockSize];
        var fullExtractPath = Path.GetFullPath(extractPath);

        while (TryReadTarEntry(stream, buffer, blockSize, out var entry))
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                return;
            }

            ExtractTarEntry(stream, fullExtractPath, entry, buffer, blockSize);
        }
    }

    private static bool TryReadTarEntry(Stream stream, byte[] buffer, int blockSize, out TarEntry entry)
    {
        // TAR header field positions and sizes (POSIX ustar format)
        const int NameOffset = 0;
        const int NameLength = 100;
        const int TypeFlagOffset = 156;
        const int SizeOffset = 124;
        const int SizeLength = 12;
        const int OctalBase = 8;

        entry = default;
        if (stream.Read(buffer, 0, blockSize) != blockSize)
        {
            return false;
        }

        entry = new TarEntry(
            Encoding.ASCII.GetString(buffer, NameOffset, NameLength).TrimEnd('\0'),
            (char)buffer[TypeFlagOffset],
            Convert.ToInt64(Encoding.ASCII.GetString(buffer, SizeOffset, SizeLength).TrimEnd('\0', ' '), OctalBase));

        return true;
    }

    private static void ExtractTarEntry(Stream stream, string fullExtractPath, TarEntry entry, byte[] buffer, int blockSize)
    {
        // Sanitize path to prevent zip slip attacks
        var targetPath = Path.GetFullPath(Path.Combine(fullExtractPath, entry.Name));
        if (!targetPath.StartsWith(fullExtractPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Tar entry '{entry.Name}' would extract outside of the target directory (zip slip detected)");
        }

        var targetDir = Path.GetDirectoryName(targetPath);
        if (targetDir != null && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        if (entry.TypeFlag != '5' && entry.Size > 0)
        {
            WriteTarEntryToFile(stream, targetPath, entry.Size, buffer, blockSize);
        }
    }

    private static void WriteTarEntryToFile(Stream stream, string targetPath, long size, byte[] buffer, int blockSize)
    {
        using var file = File.Create(targetPath);
        var remaining = size;
        while (remaining > 0 && stream.Read(buffer, 0, blockSize) == blockSize)
        {
            var toWrite = (int)Math.Min(blockSize, remaining);
            file.Write(buffer, 0, toWrite);
            remaining -= toWrite;
        }
    }

    private readonly record struct TarEntry(string Name, char TypeFlag, long Size);
}
