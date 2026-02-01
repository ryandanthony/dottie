// Licensed under the MIT License. See LICENSE in the project root for license information.

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
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                var targetPath = Path.Combine(extractPath, entry.FullName);
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

        while (stream.Read(buffer, 0, blockSize) == blockSize)
        {
            var name = Encoding.ASCII.GetString(buffer, 0, 100).TrimEnd('\0');
            if (string.IsNullOrEmpty(name))
            {
                break;
            }

            var typeflag = (char)buffer[156];
            var size = Convert.ToInt64(Encoding.ASCII.GetString(buffer, 124, 12).TrimEnd('\0', ' '), 8);

            var targetPath = Path.Combine(extractPath, name);
            var targetDir = Path.GetDirectoryName(targetPath);

            if (targetDir != null && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            if (typeflag != '5' && size > 0)
            {
                using var file = File.Create(targetPath);
                var remaining = size;
                while (remaining > 0)
                {
                    var toRead = (int)Math.Min(blockSize, remaining);
                    if (stream.Read(buffer, 0, blockSize) != blockSize)
                    {
                        break;
                    }

                    file.Write(buffer, 0, toRead);
                    remaining -= toRead;
                }
            }
        }
    }
}
