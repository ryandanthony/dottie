// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing.Utilities;
using Dottie.Configuration.Models.InstallBlocks;

namespace Dottie.Configuration.Installing;

/// <summary>
/// Installer for shell scripts on Unix-like systems.
/// Executes custom installation scripts from the repository.
/// </summary>
public class ScriptRunner : IInstallSource
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Creates a new instance of <see cref="ScriptRunner"/>.
    /// </summary>
    /// <param name="processRunner">Process runner for executing system commands. If null, a default instance is created.</param>
    public ScriptRunner(IProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new ProcessRunner();
    }

    /// <inheritdoc/>
    public InstallSourceType SourceType => InstallSourceType.Script;

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
                var processResult = await _processRunner.RunAsync(
                    "bash",
                    fullPath,
                    workingDirectory: context.RepoRoot,
                    cancellationToken: cancellationToken);

                if (processResult.Success)
                {
                    results.Add(InstallResult.Success(scriptPath, SourceType));
                }
                else
                {
                    results.Add(InstallResult.Failed(scriptPath, SourceType, $"Script exited with code {processResult.ExitCode}"));
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
