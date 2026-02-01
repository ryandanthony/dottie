// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Models.InstallBlocks;
using System.Diagnostics;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for shell scripts on Unix-like systems.
/// Executes custom installation scripts from the repository.
/// </summary>
public class ScriptRunner : IInstallSource
{
    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.Script;

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
    /// Executes scripts from the provided install block.
    /// </summary>
    /// <param name="installBlock">The install block containing script paths.</param>
    /// <param name="context">The installation context with paths and configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Installation results for each script executed.</returns>
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

        // Check if there are any scripts to run
        if (installBlock.Scripts == null || installBlock.Scripts.Count == 0)
        {
            return results;
        }

        // Skip installation if dry-run is enabled
        if (context.DryRun)
        {
            // In dry-run mode, just validate scripts exist
            foreach (var scriptPath in installBlock.Scripts)
            {
                var fullPath = Path.Combine(context.RepoRoot, scriptPath);
                if (File.Exists(fullPath))
                {
                    results.Add(InstallResult.Success(scriptPath, SourceType, null, $"Script would be executed: {scriptPath}"));
                }
                else
                {
                    results.Add(InstallResult.Failed(scriptPath, SourceType, $"Script not found: {scriptPath}"));
                }
            }
            return results;
        }

        // Execute each script
        foreach (var scriptPath in installBlock.Scripts)
        {
            try
            {
                // Validate path doesn't escape repo root
                var fullPath = Path.GetFullPath(Path.Combine(context.RepoRoot, scriptPath));
                if (!fullPath.StartsWith(context.RepoRoot, StringComparison.Ordinal))
                {
                    results.Add(InstallResult.Failed(scriptPath, SourceType, "Script path escapes repository root"));
                    continue;
                }

                // Validate script exists
                if (!File.Exists(fullPath))
                {
                    results.Add(InstallResult.Failed(scriptPath, SourceType, "Script file not found"));
                    continue;
                }

                // Execute script
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = fullPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = context.RepoRoot
                    }
                };

                process.Start();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0)
                {
                    results.Add(InstallResult.Success(scriptPath, SourceType));
                }
                else
                {
                    results.Add(InstallResult.Failed(scriptPath, SourceType, $"Script exited with code {process.ExitCode}"));
                }
            }
            catch (Exception ex)
            {
                results.Add(InstallResult.Failed(scriptPath, SourceType, $"Exception during script execution: {ex.Message}"));
            }
        }

        return results;
    }
}
