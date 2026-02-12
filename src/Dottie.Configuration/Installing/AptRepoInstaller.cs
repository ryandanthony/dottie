// -----------------------------------------------------------------------
// <copyright file="AptRepoInstaller.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for private APT repositories on Ubuntu systems.
/// Adds third-party APT repositories with GPG key verification.
/// </summary>
public class AptRepoInstaller : IInstallSource
{
    private readonly HttpDownloader _downloader;
    private readonly IProcessRunner _processRunner;

    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.AptRepo;

    /// <summary>
    /// Initializes a new instance of the <see cref="AptRepoInstaller"/> class.
    /// Creates a new instance of <see cref="AptRepoInstaller"/>.
    /// </summary>
    /// <param name="downloader">HTTP downloader for fetching GPG keys. If null, a default instance is created.</param>
    /// <param name="processRunner">Process runner for executing system commands. If null, a default instance is created.</param>
    public AptRepoInstaller(HttpDownloader? downloader = null, IProcessRunner? processRunner = null)
    {
        _downloader = downloader ?? new HttpDownloader();
        _processRunner = processRunner ?? new ProcessRunner();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallBlock installBlock, InstallContext context, Action? onItemComplete, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(installBlock);
        ArgumentNullException.ThrowIfNull(context);

        if (installBlock.AptRepos == null || installBlock.AptRepos.Count == 0 || context.DryRun)
        {
            return [];
        }

        if (!context.HasSudo)
        {
            return CreateSudoRequiredWarnings(installBlock.AptRepos.AsReadOnly(), onItemComplete);
        }

        var results = new List<InstallResult>();
        foreach (var repo in installBlock.AptRepos)
        {
            var repoResults = await ConfigureRepositoryAsync(repo, cancellationToken);
            results.AddRange(repoResults);
            onItemComplete?.Invoke();
        }

        return results;
    }

    private List<InstallResult> CreateSudoRequiredWarnings(IReadOnlyList<AptRepoItem> repos, Action? onItemComplete)
    {
        var results = new List<InstallResult>();
        foreach (var repo in repos)
        {
            results.Add(InstallResult.Warning(repo.Name, SourceType, "Sudo required to add APT repositories"));
            foreach (var package in repo.Packages ?? [])
            {
                results.Add(InstallResult.Warning(package, SourceType, "Sudo required to install packages from APT repositories"));
            }

            onItemComplete?.Invoke();
        }

        return results;
    }

    private async Task<List<InstallResult>> ConfigureRepositoryAsync(AptRepoItem repo, CancellationToken cancellationToken)
    {
        var results = new List<InstallResult>();

        try
        {
            var keyResult = await AddGpgKeyAsync(repo, cancellationToken);
            if (keyResult != null)
            {
                results.Add(keyResult);
                return results;
            }

            var sourceResult = await AddSourcesListAsync(repo, cancellationToken);
            if (sourceResult != null)
            {
                results.Add(sourceResult);
                return results;
            }

            // Update apt cache after adding the repository
            var updateResult = await UpdateAptCacheAsync(cancellationToken);
            if (updateResult != null)
            {
                results.Add(updateResult);
                return results;
            }

            results.Add(InstallResult.Success(repo.Name, SourceType));
            results.AddRange(await InstallPackagesAsync(repo, cancellationToken));
        }
        catch (Exception ex)
        {
            results.Add(InstallResult.Failed(repo.Name, SourceType, $"Unexpected error configuring repository: {ex.Message}"));
        }

        return results;
    }

    private async Task<InstallResult?> AddGpgKeyAsync(AptRepoItem repo, CancellationToken cancellationToken)
    {
        byte[] keyData;
        try
        {
            keyData = await _downloader.DownloadAsync(repo.KeyUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(repo.Name, SourceType, $"Failed to download GPG key from {repo.KeyUrl}: {ex.Message}");
        }

        var keyPath = $"/etc/apt/trusted.gpg.d/{repo.Name}.gpg";
        try
        {
            // Check if the key is ASCII-armored (starts with "-----BEGIN PGP")
            // If so, pipe through gpg --dearmor to convert to binary format
            var keyBase64 = Convert.ToBase64String(keyData);
            var isAsciiArmored = System.Text.Encoding.UTF8.GetString(keyData).TrimStart().StartsWith("-----BEGIN PGP", StringComparison.Ordinal);

            // Dearmor ASCII-armored keys to binary GPG format, or write directly if already binary
            var command = isAsciiArmored
                ? $"-c \"echo '{keyBase64}' | base64 -d | gpg --dearmor | sudo tee {keyPath} > /dev/null\""
                : $"-c \"echo '{keyBase64}' | base64 -d | sudo tee {keyPath} > /dev/null\"";

            var addKeyResult = await _processRunner.RunAsync(
                "bash",
                command,
                cancellationToken: cancellationToken);

            return addKeyResult.Success
                ? null
                : InstallResult.Failed(repo.Name, SourceType, $"Failed to add GPG key: exit code {addKeyResult.ExitCode}");
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(repo.Name, SourceType, $"Failed to add GPG key: {ex.Message}");
        }
    }

    private async Task<InstallResult?> AddSourcesListAsync(AptRepoItem repo, CancellationToken cancellationToken)
    {
        var sourcesPath = $"/etc/apt/sources.list.d/{repo.Name}.list";
        try
        {
            // Remove any conflicting .sources file (DEB822 format) to avoid
            // "Target ... is configured multiple times" warnings from APT.
            var conflictingPath = $"/etc/apt/sources.list.d/{repo.Name}.sources";
            await _processRunner.RunAsync(
                "bash",
                $"-c \"sudo rm -f {conflictingPath}\"",
                cancellationToken: cancellationToken);

            var addSourceResult = await _processRunner.RunAsync(
                "bash",
                $"-c \"echo '{repo.Repo}' | sudo tee {sourcesPath} > /dev/null\"",
                cancellationToken: cancellationToken);

            return addSourceResult.Success
                ? null
                : InstallResult.Failed(repo.Name, SourceType, $"Failed to add repository source: exit code {addSourceResult.ExitCode}");
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(repo.Name, SourceType, $"Failed to add repository source: {ex.Message}");
        }
    }

    private async Task<InstallResult?> UpdateAptCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            var updateResult = await _processRunner.RunAsync(
                "sudo",
                "apt-get update",
                cancellationToken: cancellationToken);

            return updateResult.Success
                ? null
                : InstallResult.Warning("apt-update", SourceType, $"apt-get update returned exit code {updateResult.ExitCode}");
        }
        catch (Exception ex)
        {
            return InstallResult.Warning("apt-update", SourceType, $"apt-get update failed: {ex.Message}");
        }
    }

    private async Task<List<InstallResult>> InstallPackagesAsync(AptRepoItem repo, CancellationToken cancellationToken)
    {
        var results = new List<InstallResult>();
        if (repo.Packages == null || repo.Packages.Count == 0)
        {
            return results;
        }

        foreach (var package in repo.Packages)
        {
            var result = await InstallSinglePackageAsync(package, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private async Task<InstallResult> InstallSinglePackageAsync(string package, CancellationToken cancellationToken)
    {
        try
        {
            var installResult = await _processRunner.RunAsync(
                "sudo",
                $"apt-get install -y {package}",
                cancellationToken: cancellationToken);

            return installResult.Success
                ? InstallResult.Success(package, SourceType)
                : InstallResult.Failed(package, SourceType, $"apt-get install failed with exit code {installResult.ExitCode}");
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(package, SourceType, $"Exception during package installation: {ex.Message}");
        }
    }
}
