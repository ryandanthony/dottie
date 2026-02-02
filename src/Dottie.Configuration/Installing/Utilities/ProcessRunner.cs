// -----------------------------------------------------------------------
// <copyright file="ProcessRunner.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Dottie.Configuration.Installing.Utilities;

/// <summary>
/// Default implementation of <see cref="IProcessRunner"/> that executes real system processes.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Wrapper for system process execution, tested via integration tests")]
public class ProcessRunner : IProcessRunner
{
    /// <inheritdoc/>
    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        process.Start();

        // Read output streams to prevent deadlocks
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    /// <inheritdoc/>
    public ProcessResult Run(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        int? timeoutMilliseconds = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        process.Start();

        // Read output streams to prevent deadlocks
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();

        if (timeoutMilliseconds.HasValue)
        {
            process.WaitForExit(timeoutMilliseconds.Value);
        }
        else
        {
            process.WaitForExit();
        }

        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }
}
