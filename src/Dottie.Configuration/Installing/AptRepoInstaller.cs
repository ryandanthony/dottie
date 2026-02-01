// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;
using System.Diagnostics;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for private APT repositories on Ubuntu systems.
/// Adds third-party APT repositories with GPG key verification.
/// </summary>
public class AptRepoInstaller : IInstallSource
{
    private readonly HttpDownloader _downloader;

    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.AptRepo;

    /// <summary>
    /// Creates a new instance of <see cref="AptRepoInstaller"/>.
    /// </summary>
    /// <param name="downloader">HTTP downloader for fetching GPG keys. If null, a default instance is created.</param>
    public AptRepoInstaller(HttpDownloader? downloader = null)
    {
        _downloader = downloader ?? new HttpDownloader();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InstallResult>> InstallAsync(InstallContext context, CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // This method implements the interface. The actual work is done in InstallAsync with InstallBlock.
        // For MVP, return empty to allow the orchestrator to work.
        return new List<InstallResult>();
    }

    /// <summary>
    /// Installs private APT repositories from the provided install block.
    /// </summary>
    /// <param name="installBlock">The install block containing APT repository specifications.</param>
    /// <param name="context">The installation context with paths and configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Installation results for each repository and package.</returns>
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
                    var addKeyProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "bash",
                            Arguments = $"-c \"echo '{Convert.ToBase64String(keyData)}' | base64 -d | sudo tee {keyPath} > /dev/null\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    addKeyProcess.Start();
                    await addKeyProcess.WaitForExitAsync(cancellationToken);
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
                    var addSourceProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "bash",
                            Arguments = $"-c \"echo '{repo.Repo}' | sudo tee {sourcesPath} > /dev/null\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    addSourceProcess.Start();
                    await addSourceProcess.WaitForExitAsync(cancellationToken);
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
                            var installProcess = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = "sudo",
                                    Arguments = $"apt-get install -y {package}",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true
                                }
                            };

                            installProcess.Start();
                            await installProcess.WaitForExitAsync(cancellationToken);

                            if (installProcess.ExitCode == 0)
                            {
                                results.Add(InstallResult.Success(package, SourceType));
                            }
                            else
                            {
                                results.Add(InstallResult.Failed(package, SourceType, $"apt-get install failed with exit code {installProcess.ExitCode}"));
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
