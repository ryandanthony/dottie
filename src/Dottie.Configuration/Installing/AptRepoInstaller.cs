// Licensed under the MIT License. See LICENSE in the project root for license information.

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

        // Check if there are any APT repositories to configure
        if (installBlock.AptRepos == null || installBlock.AptRepos.Count == 0)
        {
            return results;
        }

        // Skip installation if dry-run is enabled
        if (context.DryRun)
        {
            return results;
        }

        // If sudo is not available, return warning results
        if (!context.HasSudo)
        {
            foreach (var repo in installBlock.AptRepos)
            {
                results.Add(InstallResult.Warning(repo.Name, SourceType, "Sudo required to add APT repositories"));
                foreach (var package in repo.Packages ?? new List<string>())
                {
                    results.Add(InstallResult.Warning(package, SourceType, "Sudo required to install packages from APT repositories"));
                }
            }
            return results;
        }

        // Configure each repository
        foreach (var repo in installBlock.AptRepos)
        {
            try
            {
                // Download GPG key
                byte[] keyData;
                try
                {
                    keyData = await _downloader.DownloadAsync(repo.KeyUrl, cancellationToken);
                }
                catch (Exception ex)
                {
                    results.Add(InstallResult.Failed(repo.Name, SourceType, $"Failed to download GPG key from {repo.KeyUrl}: {ex.Message}"));
                    continue;
                }

                // Add GPG key to trusted keys
                var keyPath = $"/etc/apt/trusted.gpg.d/{repo.Name}.gpg";
                try
                {
                    var addKeyResult = await _processRunner.RunAsync(
                        "bash",
                        $"-c \"echo '{Convert.ToBase64String(keyData)}' | base64 -d | sudo tee {keyPath} > /dev/null\"",
                        cancellationToken: cancellationToken);

                    if (!addKeyResult.Success)
                    {
                        results.Add(InstallResult.Failed(repo.Name, SourceType, $"Failed to add GPG key: exit code {addKeyResult.ExitCode}"));
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    results.Add(InstallResult.Failed(repo.Name, SourceType, $"Failed to add GPG key: {ex.Message}"));
                    continue;
                }

                // Create sources list file
                var sourcesPath = $"/etc/apt/sources.list.d/{repo.Name}.list";
                try
                {
                    var addSourceResult = await _processRunner.RunAsync(
                        "bash",
                        $"-c \"echo '{repo.Repo}' | sudo tee {sourcesPath} > /dev/null\"",
                        cancellationToken: cancellationToken);

                    if (!addSourceResult.Success)
                    {
                        results.Add(InstallResult.Failed(repo.Name, SourceType, $"Failed to add repository source: exit code {addSourceResult.ExitCode}"));
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    results.Add(InstallResult.Failed(repo.Name, SourceType, $"Failed to add repository source: {ex.Message}"));
                    continue;
                }

                results.Add(InstallResult.Success(repo.Name, SourceType));

                // Now install packages from this repository
                if (repo.Packages != null && repo.Packages.Count > 0)
                {
                    foreach (var package in repo.Packages)
                    {
                        try
                        {
                            var installResult = await _processRunner.RunAsync(
                                "sudo",
                                $"apt-get install -y {package}",
                                cancellationToken: cancellationToken);

                            if (installResult.Success)
                            {
                                results.Add(InstallResult.Success(package, SourceType));
                            }
                            else
                            {
                                results.Add(InstallResult.Failed(package, SourceType, $"apt-get install failed with exit code {installResult.ExitCode}"));
                            }
                        }
                        catch (Exception ex)
                        {
                            results.Add(InstallResult.Failed(package, SourceType, $"Exception during package installation: {ex.Message}"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(InstallResult.Failed(repo.Name, SourceType, $"Unexpected error configuring repository: {ex.Message}"));
            }
        }

        return results;
    }
}
