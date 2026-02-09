// -----------------------------------------------------------------------
// <copyright file="ArchiveExtractor.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Formats.Tar;
using System.IO.Compression;

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
            using var fileStream = File.OpenRead(tarGzPath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            ExtractTarStream(gzipStream, extractPath);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException($"Invalid tar.gz file: {tarGzPath}", ex);
        }
    }

    /// <summary>
    /// Extracts files from a tar stream using <see cref="TarReader"/>.
    /// Handles all tar formats (POSIX, PAX, GNU) including extended headers,
    /// long file names, and base-256 encoding automatically.
    /// </summary>
    /// <param name="stream">The tar stream to extract from.</param>
    /// <param name="extractPath">Directory to extract to.</param>
    private static void ExtractTarStream(Stream stream, string extractPath)
    {
        var fullExtractPath = Path.GetFullPath(extractPath);
        using var reader = new TarReader(stream);

        while (reader.GetNextEntry() is { } entry)
        {
            // Skip directories (they'll be created as needed) and non-file entries
            if (entry.EntryType == TarEntryType.Directory
                || string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            // Only extract regular files
            if (entry.EntryType is not (TarEntryType.RegularFile
                or TarEntryType.V7RegularFile))
            {
                continue;
            }

            // Sanitize path to prevent zip slip attacks
            var targetPath = Path.GetFullPath(Path.Combine(fullExtractPath, entry.Name));
            if (!targetPath.StartsWith(fullExtractPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Tar entry '{entry.Name}' would extract outside of the target directory (zip slip detected)");
            }

            var targetDir = Path.GetDirectoryName(targetPath);
            if (targetDir != null && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            entry.ExtractToFile(targetPath, overwrite: true);
        }
    }
}
