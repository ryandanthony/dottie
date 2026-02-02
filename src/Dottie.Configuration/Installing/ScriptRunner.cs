// -----------------------------------------------------------------------
// <copyright file="ScriptRunner.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
    /// Initializes a new instance of the <see cref="ScriptRunner"/> class.
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
        ArgumentNullException.ThrowIfNull(installBlock);
        ArgumentNullException.ThrowIfNull(context);

        if (installBlock.Scripts == null || installBlock.Scripts.Count == 0)
        {
            return [];
        }

        return context.DryRun
            ? ValidateScriptsExist(installBlock.Scripts.AsReadOnly(), context.RepoRoot)
            : await ExecuteScriptsAsync(installBlock.Scripts.AsReadOnly(), context, cancellationToken);
    }

    private List<InstallResult> ValidateScriptsExist(IReadOnlyList<string> scripts, string repoRoot)
    {
        var results = new List<InstallResult>();
        foreach (var scriptPath in scripts)
        {
            var fullPath = Path.Combine(repoRoot, scriptPath);
            var result = File.Exists(fullPath)
                ? InstallResult.Success(scriptPath, SourceType, message: $"Script would be executed: {scriptPath}")
                : InstallResult.Failed(scriptPath, SourceType, $"Script not found: {scriptPath}");
            results.Add(result);
        }

        return results;
    }

    private async Task<List<InstallResult>> ExecuteScriptsAsync(IReadOnlyList<string> scripts, InstallContext context, CancellationToken cancellationToken)
    {
        var results = new List<InstallResult>();
        foreach (var scriptPath in scripts)
        {
            var result = await ExecuteSingleScriptAsync(scriptPath, context, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private async Task<InstallResult> ExecuteSingleScriptAsync(string scriptPath, InstallContext context, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.GetFullPath(Path.Combine(context.RepoRoot, scriptPath));
            if (!fullPath.StartsWith(context.RepoRoot, StringComparison.Ordinal))
            {
                return InstallResult.Failed(scriptPath, SourceType, "Script path escapes repository root");
            }

            if (!File.Exists(fullPath))
            {
                return InstallResult.Failed(scriptPath, SourceType, "Script file not found");
            }

            var processResult = await _processRunner.RunAsync(
                "bash",
                fullPath,
                workingDirectory: context.RepoRoot,
                cancellationToken: cancellationToken);

            return processResult.Success
                ? InstallResult.Success(scriptPath, SourceType)
                : InstallResult.Failed(scriptPath, SourceType, $"Script exited with code {processResult.ExitCode}");
        }
        catch (Exception ex)
        {
            return InstallResult.Failed(scriptPath, SourceType, $"Exception during script execution: {ex.Message}");
        }
    }
}
