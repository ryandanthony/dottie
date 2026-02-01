// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using System.Diagnostics;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for fonts on Linux systems.
/// Downloads fonts and installs them to ~/.local/share/fonts/.
/// </summary>
public class FontInstaller : IInstallSource
{
    private readonly HttpDownloader _downloader;
    private readonly ArchiveExtractor _extractor;

    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.Font;

    /// <summary>
    /// Creates a new instance of <see cref="FontInstaller"/>.
    /// </summary>
    /// <param name="downloader">HTTP downloader for fetching fonts. If null, a default instance is created.</param>
    public FontInstaller(HttpDownloader? downloader = null)
    {
        _downloader = downloader ?? new HttpDownloader();
        _extractor = new ArchiveExtractor();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallContext context, CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // This method implements the interface. The actual work is done in InstallAsync with InstallBlock.
        return new List<InstallResult>();
    }

    /// <summary>
    /// Installs fonts from the provided install block.
    /// </summary>
    /// <param name="installBlock">The install block containing font specifications.</param>
    /// <param name="context">The installation context with paths and configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Installation results for each font.</returns>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallBlock installBlock, InstallContext context, CancellationToken cancellationToken = default)
    {
        if (installBlock == null)
        {
            throw new ArgumentNullException(nameof(installBlock));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var results = new List<InstallResult>();

        // Check if there are any fonts to install
        if (installBlock.Fonts == null || installBlock.Fonts.Count == 0)
        {
            return results;
        }

        // Skip installation if dry-run is enabled
        if (context.DryRun)
        {
            return results;
        }

        // Ensure font directory exists
        try
        {
            Directory.CreateDirectory(context.FontDirectory);
        }
        catch (Exception ex)
        {
            foreach (var font in installBlock.Fonts)
            {
                results.Add(InstallResult.Failed(font.Name, SourceType, $"Failed to create font directory: {ex.Message}"));
            }
            return results;
        }

        // Download and install each font
        foreach (var font in installBlock.Fonts)
        {
            try
            {
                // Download font archive
                byte[] fontData;
                try
                {
                    fontData = await _downloader.DownloadAsync(font.Url, cancellationToken);
                }
                catch (Exception ex)
                {
                    results.Add(InstallResult.Failed(font.Name, SourceType, $"Failed to download font from {font.Url}: {ex.Message}"));
                    continue;
                }

                // Create font directory
                var fontDir = Path.Combine(context.FontDirectory, font.Name);
                try
                {
                    Directory.CreateDirectory(fontDir);
                }
                catch (Exception ex)
                {
                    results.Add(InstallResult.Failed(font.Name, SourceType, $"Failed to create font subdirectory: {ex.Message}"));
                    continue;
                }

                // Extract font archive
                try
                {
                    // Save the downloaded data to a temporary file
                    var tempFile = Path.GetTempFileName();
                    try
                    {
                        await File.WriteAllBytesAsync(tempFile, fontData, cancellationToken);
                        _extractor.Extract(tempFile, fontDir);
                        results.Add(InstallResult.Success(font.Name, SourceType));
                    }
                    finally
                    {
                        // Clean up temp file
                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                }
                catch (Exception ex)
                {
                    results.Add(InstallResult.Failed(font.Name, SourceType, $"Failed to extract font archive: {ex.Message}"));
                }
            }
            catch (Exception ex)
            {
                results.Add(InstallResult.Failed(font.Name, SourceType, $"Unexpected error installing font: {ex.Message}"));
            }
        }

        // Refresh font cache after all fonts are installed
        if (results.Any(r => r.Status == InstallStatus.Success))
        {
            try
            {
                var fcCacheProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "fc-cache",
                        Arguments = "-fv",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                fcCacheProcess.Start();
                await fcCacheProcess.WaitForExitAsync(cancellationToken);
            }
            catch
            {
                // Font cache update failure is non-critical
            }
        }

        return results;
    }
}
