// -----------------------------------------------------------------------
// <copyright file="FontInstaller.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for fonts on Linux systems.
/// Downloads fonts and installs them to ~/.local/share/fonts/.
/// </summary>
public class FontInstaller : IInstallSource
{
    private readonly HttpDownloader _downloader;
    private readonly ArchiveExtractor _extractor;
    private readonly IProcessRunner _processRunner;

    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.Font;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontInstaller"/> class.
    /// Creates a new instance of <see cref="FontInstaller"/>.
    /// </summary>
    /// <param name="downloader">HTTP downloader for fetching fonts. If null, a default instance is created.</param>
    /// <param name="processRunner">Process runner for executing system commands. If null, a default instance is created.</param>
    public FontInstaller(HttpDownloader? downloader = null, IProcessRunner? processRunner = null)
    {
        _downloader = downloader ?? new HttpDownloader();
        _extractor = new ArchiveExtractor();
        _processRunner = processRunner ?? new ProcessRunner();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallBlock installBlock, InstallContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installBlock);
        ArgumentNullException.ThrowIfNull(context);

        if (installBlock.Fonts == null || installBlock.Fonts.Count == 0 || context.DryRun)
        {
            return [];
        }

        if (!TryEnsureFontDirectory(context.FontDirectory, out var directoryError))
        {
            return installBlock.Fonts
                .Select(font => InstallResult.Failed(font.Name, SourceType, directoryError!))
                .ToList();
        }

        var results = new List<InstallResult>();
        foreach (var font in installBlock.Fonts)
        {
            var result = await InstallSingleFontAsync(font, context.FontDirectory, cancellationToken);
            results.Add(result);
        }

        await RefreshFontCacheIfNeededAsync(results, cancellationToken);
        return results;
    }

    private static bool TryEnsureFontDirectory(string fontDirectory, out string? error)
    {
        error = null;
        try
        {
            Directory.CreateDirectory(fontDirectory);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to create font directory: {ex.Message}";
            return false;
        }
    }

    private async Task<InstallResult> InstallSingleFontAsync(FontItem font, string fontDirectory, CancellationToken cancellationToken)
    {
        try
        {
            var fontData = await DownloadFontAsync(font, cancellationToken);
            if (fontData == null)
            {
                return InstallResult.Failed(font.Name, SourceType, $"Failed to download font from {font.Url}");
            }

            var fontDir = CreateFontSubdirectory(font.Name, fontDirectory);
            if (fontDir == null)
            {
                return InstallResult.Failed(font.Name, SourceType, "Failed to create font subdirectory");
            }

            return await ExtractFontArchiveAsync(font.Name, fontData, fontDir, cancellationToken);
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(font.Name, SourceType, $"Unexpected error installing font: {ex.Message}");
        }
    }

    private async Task<byte[]?> DownloadFontAsync(FontItem font, CancellationToken cancellationToken)
    {
        try
        {
            return await _downloader.DownloadAsync(font.Url, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static string? CreateFontSubdirectory(string fontName, string fontDirectory)
    {
        try
        {
            var fontDir = Path.Combine(fontDirectory, fontName);
            Directory.CreateDirectory(fontDir);
            return fontDir;
        }
        catch
        {
            return null;
        }
    }

    private async Task<InstallResult> ExtractFontArchiveAsync(string fontName, byte[] fontData, string fontDir, CancellationToken cancellationToken)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        try
        {
            await File.WriteAllBytesAsync(tempFile, fontData, cancellationToken);
            _extractor.Extract(tempFile, fontDir);
            return InstallResult.Success(fontName, SourceType);
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(fontName, SourceType, $"Failed to extract font archive: {ex.Message}");
        }
        finally
        {
            TryDeleteFile(tempFile);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private async Task RefreshFontCacheIfNeededAsync(List<InstallResult> results, CancellationToken cancellationToken)
    {
        if (results.Exists(r => r.Status == InstallStatus.Success))
        {
            try
            {
                await _processRunner.RunAsync("fc-cache", "-fv", cancellationToken: cancellationToken);
            }
            catch
            {
                // Font cache update failure is non-critical
            }
        }
    }
}
