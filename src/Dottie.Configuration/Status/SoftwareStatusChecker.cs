// -----------------------------------------------------------------------
// <copyright file="SoftwareStatusChecker.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Dottie.Configuration.Installing;
using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Status;

/// <summary>
/// Service that checks the installation status of software items.
/// </summary>
public sealed partial class SoftwareStatusChecker
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftwareStatusChecker"/> class.
    /// </summary>
    /// <param name="processRunner">Process runner for executing system commands. If null, a default instance is created.</param>
    public SoftwareStatusChecker(IProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new ProcessRunner();
    }

    /// <summary>
    /// Checks the installation status of all software items in the install block.
    /// </summary>
    /// <param name="installBlock">The install block containing software items to check.</param>
    /// <param name="context">The installation context with paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of status entries for each software item.</returns>
    public async Task<IReadOnlyList<SoftwareStatusEntry>> CheckStatusAsync(
        InstallBlock? installBlock,
        InstallContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (installBlock is null)
        {
            return [];
        }

        var results = new List<SoftwareStatusEntry>();

        await CheckGitHubReleasesAsync(installBlock, context, results, cancellationToken).ConfigureAwait(false);
        await CheckAptPackagesAsync(installBlock, results, cancellationToken).ConfigureAwait(false);
        await CheckAptReposAsync(installBlock, results, cancellationToken).ConfigureAwait(false);
        CheckScripts(installBlock, context, results);
        await CheckSnapPackagesAsync(installBlock, results, cancellationToken).ConfigureAwait(false);
        CheckFonts(installBlock, context, results);

        return results;
    }

    [GeneratedRegex(@"[vV]?(?<version>\d+\.\d+(?:\.\d+)?(?:-[a-zA-Z0-9]+)?)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex VersionRegex();

    private async Task CheckGitHubReleasesAsync(
        InstallBlock installBlock,
        InstallContext context,
        List<SoftwareStatusEntry> results,
        CancellationToken cancellationToken)
    {
        if (installBlock.Github.Count == 0)
        {
            return;
        }

        foreach (var item in installBlock.Github)
        {
            var entry = await CheckGitHubReleaseAsync(item, context, cancellationToken).ConfigureAwait(false);
            results.Add(entry);
        }
    }

    private async Task CheckAptPackagesAsync(
        InstallBlock installBlock,
        List<SoftwareStatusEntry> results,
        CancellationToken cancellationToken)
    {
        if (installBlock.Apt.Count == 0)
        {
            return;
        }

        foreach (var package in installBlock.Apt)
        {
            var entry = await CheckAptPackageAsync(package, cancellationToken).ConfigureAwait(false);
            results.Add(entry);
        }
    }

    private async Task CheckSnapPackagesAsync(
        InstallBlock installBlock,
        List<SoftwareStatusEntry> results,
        CancellationToken cancellationToken)
    {
        if (installBlock.Snaps.Count == 0)
        {
            return;
        }

        foreach (var package in installBlock.Snaps)
        {
            var entry = await CheckSnapPackageAsync(package, cancellationToken).ConfigureAwait(false);
            results.Add(entry);
        }
    }

    private async Task CheckAptReposAsync(
        InstallBlock installBlock,
        List<SoftwareStatusEntry> results,
        CancellationToken cancellationToken)
    {
        if (installBlock.AptRepos.Count == 0)
        {
            return;
        }

        foreach (var repo in installBlock.AptRepos)
        {
            var entry = await CheckAptRepoAsync(repo, cancellationToken).ConfigureAwait(false);
            results.Add(entry);
        }
    }

    private static void CheckScripts(
        InstallBlock installBlock,
        InstallContext context,
        List<SoftwareStatusEntry> results)
    {
        if (installBlock.Scripts.Count == 0)
        {
            return;
        }

        foreach (var script in installBlock.Scripts)
        {
            var entry = CheckScript(script, context);
            results.Add(entry);
        }
    }

    private static void CheckFonts(
        InstallBlock installBlock,
        InstallContext context,
        List<SoftwareStatusEntry> results)
    {
        if (installBlock.Fonts.Count == 0)
        {
            return;
        }

        foreach (var font in installBlock.Fonts)
        {
            var entry = CheckFont(font, context);
            results.Add(entry);
        }
    }

    private async Task<SoftwareStatusEntry> CheckGitHubReleaseAsync(
        GithubReleaseItem item,
        InstallContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await CheckGitHubReleaseInternalAsync(item, context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return CreateErrorEntry(item.Binary, InstallSourceType.GithubRelease, item.Version, ex.Message);
        }
    }

    private async Task<SoftwareStatusEntry> CheckGitHubReleaseInternalAsync(
        GithubReleaseItem item,
        InstallContext context,
        CancellationToken cancellationToken)
    {
        var binaryName = item.Binary;
        var binPath = Path.Combine(context.BinDirectory, binaryName);

        var existingBinaryResult = await CheckExistingBinaryAsync(binaryName, binPath, item.Version, cancellationToken).ConfigureAwait(false);
        if (existingBinaryResult is not null)
        {
            return existingBinaryResult;
        }

        var windowsResult = CheckWindowsBinary(binaryName, binPath, item.Version);
        if (windowsResult is not null)
        {
            return windowsResult;
        }

        var pathResult = await CheckPathBinaryAsync(binaryName, item.Version, cancellationToken).ConfigureAwait(false);
        if (pathResult is not null)
        {
            return pathResult;
        }

        return CreateMissingEntry(binaryName, InstallSourceType.GithubRelease, item.Version);
    }

    private async Task<SoftwareStatusEntry?> CheckExistingBinaryAsync(
        string binaryName,
        string binPath,
        string? targetVersion,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(binPath))
        {
            return null;
        }

        var installedVersion = await GetBinaryVersionAsync(binPath, cancellationToken).ConfigureAwait(false);
        var isOutdated = !string.IsNullOrEmpty(targetVersion) &&
                         !string.IsNullOrEmpty(installedVersion) &&
                         !VersionMatches(installedVersion, targetVersion);

        var state = isOutdated ? SoftwareInstallState.Outdated : SoftwareInstallState.Installed;

        return new SoftwareStatusEntry(
            binaryName,
            InstallSourceType.GithubRelease,
            state,
            installedVersion,
            targetVersion,
            binPath,
            null);
    }

    private static SoftwareStatusEntry? CheckWindowsBinary(string binaryName, string binPath, string? targetVersion)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var exePath = binPath + ".exe";
        if (!File.Exists(exePath))
        {
            return null;
        }

        return new SoftwareStatusEntry(
            binaryName,
            InstallSourceType.GithubRelease,
            SoftwareInstallState.Installed,
            null,
            targetVersion,
            exePath,
            null);
    }

    private async Task<SoftwareStatusEntry?> CheckPathBinaryAsync(
        string binaryName,
        string? targetVersion,
        CancellationToken cancellationToken)
    {
        var pathLocation = await FindInPathAsync(binaryName, cancellationToken).ConfigureAwait(false);
        if (pathLocation is null)
        {
            return null;
        }

        return new SoftwareStatusEntry(
            binaryName,
            InstallSourceType.GithubRelease,
            SoftwareInstallState.Installed,
            null,
            targetVersion,
            pathLocation,
            null);
    }

    private async Task<SoftwareStatusEntry> CheckAptPackageAsync(string packageName, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _processRunner.RunAsync("dpkg", $"-s {packageName}", cancellationToken: cancellationToken).ConfigureAwait(false);
            var state = result.ExitCode == 0 ? SoftwareInstallState.Installed : SoftwareInstallState.Missing;

            return new SoftwareStatusEntry(packageName, InstallSourceType.AptPackage, state, null, null, null, null);
        }
        catch (Exception ex)
        {
            return CreateErrorEntry(packageName, InstallSourceType.AptPackage, null, ex.Message);
        }
    }

    private async Task<SoftwareStatusEntry> CheckSnapPackageAsync(SnapItem package, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _processRunner.RunAsync("snap", $"list {package.Name}", cancellationToken: cancellationToken).ConfigureAwait(false);
            var state = result.ExitCode == 0 ? SoftwareInstallState.Installed : SoftwareInstallState.Missing;

            return new SoftwareStatusEntry(package.Name, InstallSourceType.SnapPackage, state, null, null, null, null);
        }
        catch (Exception ex)
        {
            return CreateErrorEntry(package.Name, InstallSourceType.SnapPackage, null, ex.Message);
        }
    }

    private async Task<SoftwareStatusEntry> CheckAptRepoAsync(AptRepoItem repo, CancellationToken cancellationToken)
    {
        try
        {
            // Check if all packages from this repo are installed
            var allInstalled = true;
            foreach (var package in repo.Packages)
            {
                var result = await _processRunner.RunAsync("dpkg", $"-s {package}", cancellationToken: cancellationToken).ConfigureAwait(false);
                if (result.ExitCode != 0)
                {
                    allInstalled = false;
                    break;
                }
            }

            var state = allInstalled ? SoftwareInstallState.Installed : SoftwareInstallState.Missing;
            var message = repo.Packages.Count > 0 ? $"Packages: {string.Join(", ", repo.Packages)}" : null;
            return new SoftwareStatusEntry(repo.Name, InstallSourceType.AptRepo, state, null, null, null, message);
        }
        catch (Exception ex)
        {
            return CreateErrorEntry(repo.Name, InstallSourceType.AptRepo, null, ex.Message);
        }
    }

    private static SoftwareStatusEntry CheckScript(string scriptPath, InstallContext context)
    {
        try
        {
            var fullPath = Path.Combine(context.RepoRoot, scriptPath);
            var exists = File.Exists(fullPath);

            // Scripts don't have a traditional "installed" state - they're either present (ready to run) or missing
            var state = exists ? SoftwareInstallState.Installed : SoftwareInstallState.Missing;
            var message = exists ? "Script ready to execute" : "Script file not found";

            return new SoftwareStatusEntry(scriptPath, InstallSourceType.Script, state, null, null, exists ? fullPath : null, message);
        }
        catch (Exception ex)
        {
            return CreateErrorEntry(scriptPath, InstallSourceType.Script, null, ex.Message);
        }
    }

    private static SoftwareStatusEntry CheckFont(FontItem font, InstallContext context)
    {
        try
        {
            return CheckFontInternal(font, context);
        }
        catch (Exception ex)
        {
            return CreateErrorEntry(font.Name, InstallSourceType.Font, null, ex.Message);
        }
    }

    private static SoftwareStatusEntry CheckFontInternal(FontItem font, InstallContext context)
    {
        var fontDir = context.FontDirectory;
        if (!Directory.Exists(fontDir))
        {
            return CreateMissingEntry(font.Name, InstallSourceType.Font, null);
        }

        if (FontExistsInDirectory(font.Name, fontDir))
        {
            return new SoftwareStatusEntry(font.Name, InstallSourceType.Font, SoftwareInstallState.Installed, null, null, fontDir, null);
        }

        return CreateMissingEntry(font.Name, InstallSourceType.Font, null);
    }

    private static bool FontExistsInDirectory(string fontName, string fontDir)
    {
        var searchPatterns = new[] { "*.ttf", "*.otf", "*.woff", "*.woff2" };
        var normalizedFontName = fontName.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);

        foreach (var pattern in searchPatterns)
        {
            var files = Directory.GetFiles(fontDir, pattern, SearchOption.AllDirectories);
            if (Array.Exists(files, f => Path.GetFileNameWithoutExtension(f).Contains(normalizedFontName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string?> GetBinaryVersionAsync(string binaryPath, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _processRunner.RunAsync(binaryPath, "--version", cancellationToken: cancellationToken).ConfigureAwait(false);
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput)
                ? ExtractVersion(result.StandardOutput)
                : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> FindInPathAsync(string binaryName, CancellationToken cancellationToken)
    {
        try
        {
            var command = OperatingSystem.IsWindows() ? "where" : "which";
            var result = await _processRunner.RunAsync(command, binaryName, cancellationToken: cancellationToken).ConfigureAwait(false);

            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput)
                ? result.StandardOutput.Trim().Split('\n')[0].Trim()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractVersion(string versionOutput)
    {
        var lines = versionOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return null;
        }

        var match = VersionRegex().Match(lines[0].Trim());
        return match.Success ? match.Groups["version"].Value : null;
    }

    private static bool VersionMatches(string installedVersion, string targetVersion)
    {
        var installed = installedVersion.TrimStart('v', 'V');
        var target = targetVersion.TrimStart('v', 'V');
        return string.Equals(installed, target, StringComparison.OrdinalIgnoreCase);
    }

    private static SoftwareStatusEntry CreateMissingEntry(string itemName, InstallSourceType sourceType, string? targetVersion) =>
        new(itemName, sourceType, SoftwareInstallState.Missing, null, targetVersion, null, null);

    private static SoftwareStatusEntry CreateErrorEntry(string itemName, InstallSourceType sourceType, string? targetVersion, string errorMessage) =>
        new(itemName, sourceType, SoftwareInstallState.Unknown, null, targetVersion, null, $"Detection error: {errorMessage}");
}
