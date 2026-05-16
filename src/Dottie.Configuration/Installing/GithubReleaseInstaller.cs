// -----------------------------------------------------------------------
// <copyright file="GithubReleaseInstaller.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using Dottie.Configuration.Utilities;
using Flurl.Http;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for GitHub release binaries.
/// Downloads assets from GitHub releases and installs them to the bin directory.
/// </summary>
[SuppressMessage("SonarAnalyzer.CSharp", "S1200:Classes should not be coupled to too many other classes", Justification = "GitHub release installation inherently requires multiple dependencies for HTTP, JSON, file I/O, archive extraction, and process execution.")]
public class GithubReleaseInstaller : IInstallSource
{
    private const string LatestVersion = "latest";
    private const int HttpSuccessMin = 200;
    private const int HttpSuccessMax = 300;
    private const int RequestTimeoutSeconds = 10;
    private const int MajorVersionPartIndex = 0;
    private const int MinorVersionPartIndex = 1;
    private const int PatchVersionPartIndex = 2;
    private const int MinimumSemanticVersionParts = 2;
    private const int MaximumSemanticVersionParts = 3;
    private static readonly Regex VersionRegex = new(@"[vV]?(?<version>\d+\.\d+(?:\.\d+)?(?:-[a-zA-Z0-9.-]+)?)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    private readonly HttpDownloader _downloader;
    private readonly ArchiveExtractor _extractor;
    private readonly IProcessRunner _processRunner;
    private readonly string? _githubToken;

    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.GithubRelease;

    /// <summary>
    /// Initializes a new instance of the <see cref="GithubReleaseInstaller"/> class.
    /// Creates a new instance of <see cref="GithubReleaseInstaller"/>.
    /// </summary>
    /// <param name="downloader">HTTP downloader for fetching release assets. If null, a default instance is created.</param>
    /// <param name="processRunner">Process runner for executing system commands. If null, a default instance is created.</param>
    public GithubReleaseInstaller(HttpDownloader? downloader = null, IProcessRunner? processRunner = null)
    {
        _downloader = downloader ?? new HttpDownloader();
        _extractor = new ArchiveExtractor();
        _processRunner = processRunner ?? new ProcessRunner();
        _githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallBlock installBlock, InstallContext context, Action? onItemComplete, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installBlock);

        ArgumentNullException.ThrowIfNull(context);

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
                results.Add(InstallResult.Failed(item.Binary ?? item.Repo, SourceType, $"Failed to install from {item.Repo}: {ex.Message}"));
            }

            onItemComplete?.Invoke();
        }

        return results;
    }

    private async Task<InstallResult> InstallGithubReleaseItemAsync(GithubReleaseItem item, InstallContext context, CancellationToken cancellationToken)
    {
        if (item.Type == GithubReleaseAssetType.Deb)
        {
            return await InstallDebReleaseItemAsync(item, context, cancellationToken);
        }

        return await InstallBinaryReleaseItemAsync(item, context, cancellationToken);
    }

    private async Task<InstallResult> InstallBinaryReleaseItemAsync(GithubReleaseItem item, InstallContext context, CancellationToken cancellationToken)
    {
        var binaryName = item.Binary ?? item.Repo;

        if (!context.UpdateExisting)
        {
            // Check if binary is already installed (idempotency)
            var installedCheck = await CheckBinaryInstalledAsync(binaryName, context, cancellationToken);
            if (installedCheck != null)
            {
                return installedCheck;
            }

            if (context.DryRun)
            {
                return await ValidateReleaseExistsAsync(item, cancellationToken);
            }

            return await DownloadAndInstallReleaseAsync(item, context, cancellationToken);
        }

        return await InstallOrUpdateBinaryReleaseAsync(item, context, binaryName, cancellationToken);
    }

    private async Task<InstallResult> InstallOrUpdateBinaryReleaseAsync(
        GithubReleaseItem item,
        InstallContext context,
        string binaryName,
        CancellationToken cancellationToken)
    {
        var release = await GetGithubReleaseAsync(item, cancellationToken);
        if (release == null)
        {
            return InstallResult.Failed(binaryName, SourceType, $"GitHub release not found (API returned null): {item.Repo}@{item.Version ?? LatestVersion}");
        }

        var resolvedItem = ResolveReleaseVersion(item, release);
        var targetVersion = GetTargetVersion(item, release);
        var installedBinary = await FindInstalledBinaryAsync(binaryName, context, cancellationToken);
        if (installedBinary is { } existingBinary)
        {
            var installedVersion = await GetBinaryVersionAsync(existingBinary.Path, cancellationToken);
            if (ShouldSkipExistingInstall(installedVersion, targetVersion, item.Version is not null))
            {
                return InstallResult.Skipped(binaryName, SourceType, BuildUpToDateMessage(existingBinary.Path, installedVersion, targetVersion));
            }

            if (context.DryRun)
            {
                return CreateDryRunUpdateResult(binaryName, installedVersion, targetVersion);
            }
            else
            {
                // Continue with the normal download/install flow when an update is required.
            }
        }
        else if (context.DryRun)
        {
            return InstallResult.Success(binaryName, SourceType, message: $"GitHub release {item.Repo}@{targetVersion ?? LatestVersion} would be installed");
        }
        else
        {
            // No existing binary was found, so continue with a fresh install.
        }

        var matchingAsset = FindMatchingAsset(release, resolvedItem.Asset);
        if (matchingAsset == null)
        {
            return InstallResult.Failed(resolvedItem.Binary ?? resolvedItem.Repo, SourceType, $"No asset matching pattern '{resolvedItem.Asset}' in release {item.Repo}@{item.Version ?? LatestVersion}");
        }

        return await DownloadAndExtractAssetAsync(resolvedItem, matchingAsset, context, cancellationToken);
    }

    private async Task<InstallResult> InstallDebReleaseItemAsync(GithubReleaseItem item, InstallContext context, CancellationToken cancellationToken)
    {
        var itemName = item.Repo;

        // Check sudo first — required for dpkg -i
        if (!context.HasSudo)
        {
            return InstallResult.Warning(itemName, SourceType, "Sudo required to install .deb packages");
        }

        // Check dpkg availability
        var dpkgCheck = await CheckDpkgAvailableAsync(cancellationToken);
        if (!dpkgCheck)
        {
            return InstallResult.Failed(itemName, SourceType, "dpkg is not available on this system");
        }

        if (context.DryRun)
        {
            return await ValidateDebReleaseExistsAsync(item, cancellationToken);
        }

        return await DownloadAndInstallDebAsync(item, context, cancellationToken);
    }

    /// <summary>
    /// Checks if the binary is already installed in ~/bin/ or PATH.
    /// </summary>
    /// <param name="binaryName">Name of the binary to check.</param>
    /// <param name="context">Install context with bin directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Skipped result if installed, null if not installed.</returns>
    private async Task<InstallResult?> CheckBinaryInstalledAsync(string binaryName, InstallContext context, CancellationToken cancellationToken)
    {
        // Check ~/bin/ first (dottie's install location)
        var binPath = Path.Combine(context.BinDirectory, binaryName);
        if (File.Exists(binPath))
        {
            return InstallResult.Skipped(binaryName, SourceType, $"Already installed in {context.BinDirectory}");
        }

        // On Windows, also check with .exe extension
        if (OperatingSystem.IsWindows())
        {
            var exePath = binPath + ".exe";
            if (File.Exists(exePath))
            {
                return InstallResult.Skipped(binaryName, SourceType, $"Already installed in {context.BinDirectory}");
            }
        }

        // Fall back to PATH check using 'which' (Linux/macOS) or 'where' (Windows)
        var pathCheck = await CheckPathForBinaryAsync(binaryName, cancellationToken);
        if (pathCheck != null)
        {
            return InstallResult.Skipped(binaryName, SourceType, $"Already installed at {pathCheck}");
        }

        return null;
    }

    /// <summary>
    /// Checks if the binary exists in PATH using the system's 'which' or 'where' command.
    /// </summary>
    /// <param name="binaryName">Name of the binary to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to binary if found, null otherwise.</returns>
    private async Task<string?> CheckPathForBinaryAsync(string binaryName, CancellationToken cancellationToken)
    {
        try
        {
            var command = OperatingSystem.IsWindows() ? "where" : "which";
            var result = await _processRunner.RunAsync(command, binaryName, cancellationToken: cancellationToken);

            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                return result.StandardOutput.Trim().Split('\n')[0].Trim();
            }
        }
        catch
        {
            // Ignore errors from which/where command - just means not found
        }

        return null;
    }

    private async Task<InstallResult> ValidateReleaseExistsAsync(GithubReleaseItem item, CancellationToken cancellationToken)
    {
        var itemName = item.Binary ?? item.Repo;
        var releaseUrl = BuildReleaseUrl(item.Repo, item.Version);
        var versionDisplay = item.Version ?? LatestVersion;

        try
        {
            var request = BuildGithubRequest(releaseUrl);
            var response = await request.HeadAsync(cancellationToken: cancellationToken);

            return response.StatusCode >= HttpSuccessMin && response.StatusCode < HttpSuccessMax
                ? InstallResult.Success(itemName, SourceType, message: $"GitHub release {item.Repo}@{versionDisplay} would be installed")
                : InstallResult.Failed(itemName, SourceType, $"GitHub release not found: {item.Repo}@{versionDisplay} (HTTP {response.StatusCode})");
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(itemName, SourceType, $"Failed to verify GitHub release {item.Repo}: {ex.Message}");
        }
    }

    private async Task<InstallResult> DownloadAndInstallReleaseAsync(GithubReleaseItem item, InstallContext context, CancellationToken cancellationToken)
    {
        var release = await GetGithubReleaseAsync(item, cancellationToken);
        if (release == null)
        {
            return InstallResult.Failed(item.Binary ?? item.Repo, SourceType, $"GitHub release not found (API returned null): {item.Repo}@{item.Version ?? LatestVersion}");
        }

        // Resolve ${RELEASE_VERSION} in asset and binary patterns
        var resolvedItem = ResolveReleaseVersion(item, release);

        var matchingAsset = FindMatchingAsset(release, resolvedItem.Asset);
        if (matchingAsset == null)
        {
            return InstallResult.Failed(resolvedItem.Binary ?? resolvedItem.Repo, SourceType, $"No asset matching pattern '{resolvedItem.Asset}' in release {item.Repo}@{item.Version ?? LatestVersion}");
        }

        return await DownloadAndExtractAssetAsync(resolvedItem, matchingAsset, context, cancellationToken);
    }

    private async Task<InstallResult> DownloadAndExtractAssetAsync(GithubReleaseItem item, GithubAsset asset, InstallContext context, CancellationToken cancellationToken)
    {
        byte[] assetData;
        try
        {
            assetData = await _downloader.DownloadAsync(asset.BrowserDownloadUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(item.Binary ?? item.Repo, SourceType, $"Failed to download {asset.Name}: {ex.Message}");
        }

        return await ExtractAndInstallBinaryAsync(item, asset, assetData, context, cancellationToken);
    }

    private async Task<InstallResult> ExtractAndInstallBinaryAsync(GithubReleaseItem item, GithubAsset asset, byte[] assetData, InstallContext context, CancellationToken cancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"dottie-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var assetPath = Path.Combine(tempDir, asset.Name);
            await File.WriteAllBytesAsync(assetPath, assetData, cancellationToken);

            var binaryPath = ResolveBinaryPath(assetPath, tempDir, asset.Name, item.Binary ?? item.Repo);
            if (binaryPath == null)
            {
                return InstallResult.Failed(item.Binary ?? item.Repo, SourceType, $"Binary '{item.Binary ?? item.Repo}' not found in release asset");
            }

            return await CopyBinaryToBinDirectoryAsync(item, binaryPath, context, cancellationToken);
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(item.Binary ?? item.Repo, SourceType, $"Failed to install: {ex.Message}");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    private string? ResolveBinaryPath(string assetPath, string tempDir, string assetName, string binaryName)
    {
        if (IsArchiveFile(assetName))
        {
            var extractDir = Path.Combine(tempDir, "extracted");
            _extractor.Extract(assetPath, extractDir);
            return FindBinaryInDirectory(extractDir, binaryName);
        }

        return File.Exists(assetPath) ? assetPath : null;
    }

    private static GithubReleaseItem ResolveReleaseVersion(GithubReleaseItem item, GithubRelease release)
    {
        // Determine the version: explicit version tag or tag from API response
        var version = !string.IsNullOrEmpty(item.Version)
            ? item.Version
            : release.TagName;

        if (string.IsNullOrEmpty(version))
        {
            return item;
        }

        // Strip leading 'v' prefix if present for the version value
        var versionValue = version.StartsWith('v') ? version[1..] : version;

        var releaseVars = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RELEASE_VERSION"] = versionValue,
        };

        var assetResult = VariableResolver.ResolveString(item.Asset, releaseVars);
        var binaryResult = item.Binary is not null
            ? VariableResolver.ResolveString(item.Binary, releaseVars)
            : null;

        return item with
        {
            Asset = assetResult.ResolvedValue,
            Binary = binaryResult?.ResolvedValue ?? item.Binary,
        };
    }

    private static bool IsArchiveFile(string fileName)
    {
        return fileName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<InstallResult> CopyBinaryToBinDirectoryAsync(GithubReleaseItem item, string binaryPath, InstallContext context, CancellationToken cancellationToken)
    {
        var binaryName = item.Binary ?? item.Repo;

        if (!Directory.Exists(context.BinDirectory))
        {
            Directory.CreateDirectory(context.BinDirectory);
        }

        var destPath = Path.Combine(context.BinDirectory, binaryName);
        File.Copy(binaryPath, destPath, overwrite: true);

        if (!OperatingSystem.IsWindows())
        {
            await _processRunner.RunAsync("chmod", $"+x \"{destPath}\"", cancellationToken: cancellationToken);
        }

        return InstallResult.Success(binaryName, SourceType, destPath, $"from {item.Repo}");
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private async Task<bool> CheckDpkgAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _processRunner.RunAsync("which", "dpkg", cancellationToken: cancellationToken);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<InstallResult> ValidateDebReleaseExistsAsync(GithubReleaseItem item, CancellationToken cancellationToken)
    {
        var itemName = item.Repo;
        var releaseUrl = BuildReleaseUrl(item.Repo, item.Version);
        var versionDisplay = item.Version ?? LatestVersion;

        try
        {
            var request = BuildGithubRequest(releaseUrl);
            var response = await request.HeadAsync(cancellationToken: cancellationToken);

            return response.StatusCode >= HttpSuccessMin && response.StatusCode < HttpSuccessMax
                ? InstallResult.Skipped(itemName, SourceType, $"{item.Repo}: would be installed via dpkg")
                : InstallResult.Failed(itemName, SourceType, $"GitHub release not found: {item.Repo}@{versionDisplay} (HTTP {response.StatusCode})");
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(itemName, SourceType, $"Failed to verify GitHub release {item.Repo}: {ex.Message}");
        }
    }

    private async Task<InstallResult> DownloadAndInstallDebAsync(GithubReleaseItem item, InstallContext context, CancellationToken cancellationToken)
    {
        var itemName = item.Repo;

        var release = await GetGithubReleaseAsync(item, cancellationToken);
        if (release == null)
        {
            return InstallResult.Failed(itemName, SourceType, $"GitHub release not found (API returned null): {item.Repo}@{item.Version ?? LatestVersion}");
        }

        // Resolve ${RELEASE_VERSION} in asset pattern
        var resolvedItem = ResolveReleaseVersion(item, release);

        var matchingAsset = FindMatchingAsset(release, resolvedItem.Asset);
        if (matchingAsset == null)
        {
            return InstallResult.Failed(itemName, SourceType, $"No asset matching pattern '{resolvedItem.Asset}' in release {item.Repo}@{item.Version ?? LatestVersion}");
        }

        // Validate the asset looks like a .deb file
        if (!matchingAsset.Name.EndsWith(".deb", StringComparison.OrdinalIgnoreCase))
        {
            return InstallResult.Failed(itemName, SourceType, "Asset does not appear to be a .deb package");
        }

        return await InstallDebPackageAsync(item, matchingAsset, context, GetTargetVersion(item, release), cancellationToken);
    }

    private async Task<InstallResult> InstallDebPackageAsync(
        GithubReleaseItem item,
        GithubAsset asset,
        InstallContext context,
        string? targetVersion,
        CancellationToken cancellationToken)
    {
        var itemName = item.Repo;
        var tempDir = Path.Combine(Path.GetTempPath(), $"dottie-deb-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(tempDir);
            var debPath = Path.Combine(tempDir, asset.Name);

            // Download the .deb file
            byte[] assetData;
            try
            {
                assetData = await _downloader.DownloadAsync(asset.BrowserDownloadUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                return InstallResult.Failed(itemName, SourceType, $"Failed to download {asset.Name}: {ex.Message}");
            }

            await File.WriteAllBytesAsync(debPath, assetData, cancellationToken);

            // Extract package name from .deb file
            var dpkgDeb = await _processRunner.RunAsync("dpkg-deb", $"--showformat='${{Package}}' -W \"{debPath}\"", cancellationToken: cancellationToken);
            var packageName = dpkgDeb.StandardOutput.Trim().Trim('\'');

            // Check if already installed (idempotency / upgrades)
            var installedVersionResult = await _processRunner.RunAsync("dpkg-query", $"-W -f='${{Version}}' {packageName}", cancellationToken: cancellationToken);
            if (installedVersionResult.ExitCode == 0)
            {
                var installedVersion = installedVersionResult.StandardOutput.Trim().Trim('\'');
                if (!context.UpdateExisting || ShouldSkipExistingInstall(installedVersion, targetVersion, item.Version is not null))
                {
                    return InstallResult.Skipped(itemName, SourceType, $"{item.Repo}: package '{packageName}' is already up to date");
                }
            }

            // Install the .deb package
            var dpkgInstall = await _processRunner.RunAsync("sudo", $"dpkg -i \"{debPath}\"", cancellationToken: cancellationToken);
            if (dpkgInstall.ExitCode != 0)
            {
                return InstallResult.Failed(itemName, SourceType, $"dpkg installation failed: {dpkgInstall.StandardError}");
            }

            // Resolve dependencies
            var aptFix = await _processRunner.RunAsync("sudo", "apt-get install -f -y", cancellationToken: cancellationToken);
            if (aptFix.ExitCode != 0)
            {
                return InstallResult.Failed(itemName, SourceType, $"Dependency resolution failed: {aptFix.StandardError}");
            }

            return InstallResult.Success(itemName, SourceType, message: $"{item.Repo}: installed via dpkg");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    private async Task<InstalledBinaryInfo?> FindInstalledBinaryAsync(string binaryName, InstallContext context, CancellationToken cancellationToken)
    {
        var binPath = Path.Combine(context.BinDirectory, binaryName);
        if (File.Exists(binPath))
        {
            return new InstalledBinaryInfo(binPath);
        }

        if (OperatingSystem.IsWindows())
        {
            var exePath = binPath + ".exe";
            if (File.Exists(exePath))
            {
                return new InstalledBinaryInfo(exePath);
            }
        }

        var pathCheck = await CheckPathForBinaryAsync(binaryName, cancellationToken);
        return pathCheck is null ? null : new InstalledBinaryInfo(pathCheck);
    }

    private async Task<string?> GetBinaryVersionAsync(string binaryPath, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _processRunner.RunAsync(binaryPath, "--version", cancellationToken: cancellationToken);
            if (!result.Success || string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                return null;
            }

            var firstLine = result.StandardOutput
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(firstLine))
            {
                return null;
            }

            var match = VersionRegex.Match(firstLine.Trim());
            return match.Success ? match.Groups["version"].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetTargetVersion(GithubReleaseItem item, GithubRelease release)
    {
        if (!string.IsNullOrWhiteSpace(release.TagName))
        {
            return release.TagName;
        }

        return string.IsNullOrWhiteSpace(item.Version) ? null : item.Version;
    }

    private static bool ShouldSkipExistingInstall(string? installedVersion, string? targetVersion, bool isPinnedVersion)
    {
        if (string.IsNullOrWhiteSpace(installedVersion) || string.IsNullOrWhiteSpace(targetVersion))
        {
            return false;
        }

        if (VersionTextEquals(installedVersion, targetVersion))
        {
            return true;
        }

        if (!TryCompareSemanticVersions(installedVersion, targetVersion, out var comparison))
        {
            return false;
        }

        return isPinnedVersion ? comparison == 0 : comparison >= 0;
    }

    private static string BuildUpToDateMessage(string installedPath, string? installedVersion, string? targetVersion)
    {
        if (!string.IsNullOrWhiteSpace(installedVersion))
        {
            return $"Already up to date at {installedPath} ({installedVersion})";
        }

        if (!string.IsNullOrWhiteSpace(targetVersion))
        {
            return $"Already up to date at {installedPath} ({NormalizeVersionText(targetVersion)})";
        }

        return $"Already up to date at {installedPath}";
    }

    private static InstallResult CreateDryRunUpdateResult(string binaryName, string? installedVersion, string? targetVersion)
    {
        if (!string.IsNullOrWhiteSpace(installedVersion) && !string.IsNullOrWhiteSpace(targetVersion))
        {
            return InstallResult.Success(binaryName, InstallSourceType.GithubRelease, message: $"Would update from {installedVersion} to {NormalizeVersionText(targetVersion)}");
        }

        return InstallResult.Success(binaryName, InstallSourceType.GithubRelease, message: "Would update installed binary");
    }

    private static bool VersionTextEquals(string left, string right)
        => string.Equals(NormalizeVersionText(left), NormalizeVersionText(right), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeVersionText(string version)
        => version.Trim().TrimStart('v', 'V');

    private static bool TryCompareSemanticVersions(string left, string right, out int comparison)
    {
        comparison = 0;

        if (!TryParseSemanticVersion(left, out var leftVersion) || !TryParseSemanticVersion(right, out var rightVersion))
        {
            return false;
        }

        comparison = leftVersion.CompareSemanticVersion(rightVersion);
        return true;
    }

    private static bool TryParseSemanticVersion(string value, out SemanticVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = NormalizeVersionText(value);
        var plusIndex = normalized.IndexOf('+', StringComparison.Ordinal);
        if (plusIndex >= 0)
        {
            normalized = normalized[..plusIndex];
        }

        var dashIndex = normalized.IndexOf('-', StringComparison.Ordinal);
        var prerelease = dashIndex >= 0 ? normalized[(dashIndex + 1)..] : null;
        var core = dashIndex >= 0 ? normalized[..dashIndex] : normalized;
        var parts = core.Split('.');
        if (parts.Length is < MinimumSemanticVersionParts or > MaximumSemanticVersionParts)
        {
            return false;
        }

        if (!int.TryParse(parts[MajorVersionPartIndex], NumberStyles.None, CultureInfo.InvariantCulture, out var major) ||
            !int.TryParse(parts[MinorVersionPartIndex], NumberStyles.None, CultureInfo.InvariantCulture, out var minor))
        {
            return false;
        }

        var patch = 0;
        if (parts.Length == MaximumSemanticVersionParts &&
            !int.TryParse(parts[PatchVersionPartIndex], NumberStyles.None, CultureInfo.InvariantCulture, out patch))
        {
            return false;
        }

        version = new SemanticVersion(major, minor, patch, prerelease);
        return true;
    }

    private static string BuildReleaseUrl(string repo, string? version)
    {
        return string.IsNullOrEmpty(version)
            ? $"https://api.github.com/repos/{repo}/releases/latest"
            : $"https://api.github.com/repos/{repo}/releases/tags/{version}";
    }

    private IFlurlRequest BuildGithubRequest(string url)
    {
        var request = url
            .WithHeader("User-Agent", "dottie-dotfiles/1.0")
            .WithTimeout(TimeSpan.FromSeconds(RequestTimeoutSeconds));

        if (!string.IsNullOrEmpty(_githubToken))
        {
            request = request.WithOAuthBearerToken(_githubToken);
        }

        return request;
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
                .WithTimeout(TimeSpan.FromSeconds(RequestTimeoutSeconds));

            if (!string.IsNullOrEmpty(_githubToken))
            {
                request = request.WithOAuthBearerToken(_githubToken);
            }

            var responseBody = await request.GetStringAsync();
            return ParseGithubReleaseResponse(responseBody);
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

    private static GithubRelease ParseGithubReleaseResponse(string responseBody)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
        var root = doc.RootElement;
        var tagName = root.TryGetProperty("tag_name", out var tagElement)
            ? tagElement.GetString() ?? string.Empty
            : string.Empty;
        var assetsArray = root.GetProperty("assets");

        var assets = new List<GithubAsset>();
        foreach (var asset in assetsArray.EnumerateArray())
        {
            assets.Add(new GithubAsset
            {
                Name = asset.GetProperty("name").GetString() ?? string.Empty,
                BrowserDownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? string.Empty,
            });
        }

        return new GithubRelease { TagName = tagName, Assets = assets };
    }

    private GithubAsset? FindMatchingAsset(GithubRelease release, string pattern)
    {
        if (release.Assets == null || release.Assets.Count == 0)
        {
            return null;
        }

        // Convert glob pattern to regex (properly escape first, then convert glob wildcards)
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*", StringComparison.Ordinal)
            .Replace("\\?", ".", StringComparison.Ordinal) + "$";

        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

        return release.Assets.Find(a => regex.IsMatch(a.Name));
    }

    private static string? FindBinaryInDirectory(string directory, string binaryName)
    {
        var files = Directory.GetFiles(directory, binaryName, SearchOption.AllDirectories);
        if (files.Length > 0)
        {
            return files[0];
        }

        // On Windows, also try with .exe extension
        if (OperatingSystem.IsWindows())
        {
            files = Directory.GetFiles(directory, binaryName + ".exe", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                return files[0];
            }
        }

        return null;
    }

    private readonly record struct InstalledBinaryInfo(string Path);

    private readonly record struct SemanticVersion(int Major, int Minor, int Patch, string? Prerelease)
    {
        public int CompareSemanticVersion(SemanticVersion other)
        {
            var major = Major.CompareTo(other.Major);
            if (major != 0)
            {
                return major;
            }

            var minor = Minor.CompareTo(other.Minor);
            if (minor != 0)
            {
                return minor;
            }

            var patch = Patch.CompareTo(other.Patch);
            if (patch != 0)
            {
                return patch;
            }

            return ComparePrerelease(Prerelease, other.Prerelease);
        }

        private static int ComparePrerelease(string? left, string? right)
        {
            if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right))
            {
                return 0;
            }

            if (string.IsNullOrEmpty(left))
            {
                return 1;
            }

            if (string.IsNullOrEmpty(right))
            {
                return -1;
            }

            var leftParts = left.Split('.');
            var rightParts = right.Split('.');
            var length = Math.Max(leftParts.Length, rightParts.Length);

            for (var i = 0; i < length; i++)
            {
                if (i >= leftParts.Length)
                {
                    return -1;
                }

                if (i >= rightParts.Length)
                {
                    return 1;
                }

                var partComparison = ComparePrereleasePart(leftParts[i], rightParts[i]);
                if (partComparison != 0)
                {
                    return partComparison;
                }
            }

            return 0;
        }

        private static int ComparePrereleasePart(string left, string right)
        {
            var leftNumeric = int.TryParse(left, NumberStyles.None, CultureInfo.InvariantCulture, out var leftValue);
            var rightNumeric = int.TryParse(right, NumberStyles.None, CultureInfo.InvariantCulture, out var rightValue);

            if (leftNumeric && rightNumeric)
            {
                return leftValue.CompareTo(rightValue);
            }

            if (leftNumeric)
            {
                return -1;
            }

            if (rightNumeric)
            {
                return 1;
            }

            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    // GitHub API response models
    private sealed class GithubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GithubAsset>? Assets { get; set; }
    }

    private sealed class GithubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
