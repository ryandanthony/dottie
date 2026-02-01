// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using Flurl;
using Flurl.Http;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for GitHub release binaries.
/// Downloads assets from GitHub releases and installs them to the bin directory.
/// </summary>
public class GithubReleaseInstaller : IInstallSource
{
    private readonly HttpDownloader _downloader;
    private readonly ArchiveExtractor _extractor;
    private readonly string? _githubToken;

    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.GithubRelease;

    /// <summary>
    /// Creates a new instance of <see cref="GithubReleaseInstaller"/>.
    /// </summary>
    /// <param name="downloader">HTTP downloader for fetching release assets. If null, a default instance is created.</param>
    public GithubReleaseInstaller(HttpDownloader? downloader = null)
    {
        _downloader = downloader ?? new HttpDownloader();
        _extractor = new ArchiveExtractor();
        _githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallContext context, CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var results = new List<InstallResult>();

        // GitHub releases require sudo to install to /usr/local/bin or user bin requires write permission
        // For dry-run, we still proceed to validate releases exist
        if (!context.DryRun && !context.HasSudo && !IsUserBinWritable(context))
        {
            return results; // Skip if no sudo and bin directory not writable
        }

        // This would be called with the InstallBlock from the config
        // For MVP, we return empty to allow the orchestrator to work
        return results;
    }

    /// <summary>
    /// Installs GitHub release items from the provided install block.
    /// </summary>
    /// <param name="installBlock">The install block containing GitHub release specifications.</param>
    /// <param name="context">The installation context with paths and configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Installation results for each item.</returns>
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

        if (installBlock.Github == null || installBlock.Github.Count == 0)
        {
            return results;
        }

        foreach (var item in installBlock.Github)
        {
            try
            {
                var result = await InstallGithubReleaseItemAsync(item, context, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                results.Add(InstallResult.Failed(item.Binary, SourceType, $"Failed to install from {item.Repo}: {ex.Message}"));
            }
        }

        return results;
    }

    private async Task<InstallResult> InstallGithubReleaseItemAsync(GithubReleaseItem item, InstallContext context, CancellationToken cancellationToken)
    {
        if (context.DryRun)
        {
            // In dry-run mode, just validate the release exists
            var releaseUrl = string.IsNullOrEmpty(item.Version)
                ? $"https://api.github.com/repos/{item.Repo}/releases/latest"
                : $"https://api.github.com/repos/{item.Repo}/releases/tags/{item.Version}";

            try
            {
                var request = releaseUrl
                    .WithHeader("User-Agent", "dottie-dotfiles/1.0")
                    .WithTimeout(TimeSpan.FromSeconds(10));

                if (!string.IsNullOrEmpty(_githubToken))
                {
                    request = request.WithOAuthBearerToken(_githubToken);
                }

                var response = await request.HeadAsync();
                if (response.StatusCode >= 200 && response.StatusCode < 300)
                {
                    return InstallResult.Success(item.Binary, SourceType, null, $"GitHub release {item.Repo}@{item.Version ?? "latest"} would be installed");
                }
                else
                {
                    return InstallResult.Failed(item.Binary, SourceType, $"GitHub release not found: {item.Repo}@{item.Version ?? "latest"} (HTTP {response.StatusCode})");
                }
            }
            catch (Exception ex)
            {
                return InstallResult.Failed(item.Binary, SourceType, $"Failed to verify GitHub release {item.Repo}: {ex.Message}");
            }
        }

        // Get the release from GitHub API
        var release = await GetGithubReleaseAsync(item, cancellationToken);
        if (release == null)
        {
            return InstallResult.Failed(item.Binary, SourceType, $"GitHub release not found (API returned null): {item.Repo}@{item.Version ?? "latest"}");
        }

        // Find the matching asset
        var matchingAsset = FindMatchingAsset(release, item.Asset);
        if (matchingAsset == null)
        {
            return InstallResult.Failed(item.Binary, SourceType, $"No asset matching pattern '{item.Asset}' in release {item.Repo}@{item.Version ?? "latest"}");
        }

        // Download the asset
        byte[] assetData;
        try
        {
            assetData = await _downloader.DownloadAsync(matchingAsset.BrowserDownloadUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(item.Binary, SourceType, $"Failed to download {matchingAsset.Name}: {ex.Message}");
        }

        // Extract if needed and copy to bin directory
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"dottie-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Determine if archive needs extraction
                var assetPath = Path.Combine(tempDir, matchingAsset.Name);
                await File.WriteAllBytesAsync(assetPath, assetData, cancellationToken);

                string? binaryPath;
                if (matchingAsset.Name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
                    matchingAsset.Name.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase) ||
                    matchingAsset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract archive
                    var extractDir = Path.Combine(tempDir, "extracted");
                    _extractor.Extract(assetPath, extractDir);
                    binaryPath = FindBinaryInDirectory(extractDir, item.Binary);
                }
                else
                {
                    // Binary is not archived
                    binaryPath = assetPath;
                }

                if (binaryPath == null || !File.Exists(binaryPath))
                {
                    return InstallResult.Failed(item.Binary, SourceType, $"Binary '{item.Binary}' not found in release asset");
                }

                // Ensure bin directory exists
                if (!Directory.Exists(context.BinDirectory))
                {
                    Directory.CreateDirectory(context.BinDirectory);
                }

                // Copy to bin directory
                var destPath = Path.Combine(context.BinDirectory, item.Binary);
                File.Copy(binaryPath, destPath, overwrite: true);

                // Make executable on Unix-like systems
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"+x \"{destPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true
                        }
                    };
                    process.Start();
                    await process.WaitForExitAsync(cancellationToken);
                }

                return InstallResult.Success(item.Binary, SourceType, destPath, $"from {item.Repo}");
            }
            finally
            {
                // Clean up temp directory
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(item.Binary, SourceType, $"Failed to install: {ex.Message}");
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Using JsonDocument for safe parsing")]
    private async Task<GithubRelease?> GetGithubReleaseAsync(GithubReleaseItem item, CancellationToken cancellationToken)
    {
        var url = string.IsNullOrEmpty(item.Version)
            ? $"https://api.github.com/repos/{item.Repo}/releases/latest"
            : $"https://api.github.com/repos/{item.Repo}/releases/tags/{item.Version}";

        try
        {
            var request = url
                .WithHeader("User-Agent", "dottie-dotfiles/1.0")
                .WithTimeout(TimeSpan.FromSeconds(10));

            if (!string.IsNullOrEmpty(_githubToken))
            {
                request = request.WithOAuthBearerToken(_githubToken);
            }

            // Fetch the release data from GitHub API
            var responseBody = await request.GetStringAsync();
            
            // Deserialize using JsonDocument to handle JSON safely (for AOT compatibility)
            using (var doc = System.Text.Json.JsonDocument.Parse(responseBody))
            {
                var root = doc.RootElement;
                var assetsArray = root.GetProperty("assets");
                
                var assets = new List<GithubAsset>();
                foreach (var asset in assetsArray.EnumerateArray())
                {
                    assets.Add(new GithubAsset
                    {
                        Name = asset.GetProperty("name").GetString() ?? string.Empty,
                        BrowserDownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? string.Empty
                    });
                }
                
                return new GithubRelease { Assets = assets };
            }
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"GitHub API request failed: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"GitHub API request timeout: {ex.Message}");
            return null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON deserialization error: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error in GitHub API: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private GithubAsset? FindMatchingAsset(GithubRelease release, string pattern)
    {
        if (release.Assets == null || release.Assets.Count == 0)
        {
            return null;
        }

        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

        return release.Assets.FirstOrDefault(a => regex.IsMatch(a.Name));
    }

    private string? FindBinaryInDirectory(string directory, string binaryName)
    {
        var files = Directory.GetFiles(directory, binaryName, SearchOption.AllDirectories);
        if (files.Length > 0)
        {
            return files[0];
        }

        // On Windows, also try with .exe extension
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            files = Directory.GetFiles(directory, binaryName + ".exe", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return files[0];
            }
        }

        return null;
    }

    private static bool IsUserBinWritable(InstallContext context)
    {
        try
        {
            if (!Directory.Exists(context.BinDirectory))
            {
                Directory.CreateDirectory(context.BinDirectory);
            }

            // Test write permission by attempting to create a test file
            var testFile = Path.Combine(context.BinDirectory, ".write-test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // GitHub API response models
    private class GithubRelease
    {
        [JsonPropertyName("assets")]
        public List<GithubAsset>? Assets { get; set; }
    }

    private class GithubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}

