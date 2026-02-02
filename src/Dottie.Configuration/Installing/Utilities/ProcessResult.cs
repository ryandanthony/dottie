// -----------------------------------------------------------------------
// <copyright file="ProcessResult.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Dottie.Configuration.Installing.Utilities;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public class ProcessResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessResult"/> class.
    /// Creates a new instance of <see cref="ProcessResult"/>.
    /// </summary>
    /// <param name="exitCode">The exit code of the process.</param>
    /// <param name="standardOutput">The standard output from the process.</param>
    /// <param name="standardError">The standard error from the process.</param>
    public ProcessResult(int exitCode, string standardOutput, string standardError)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput ?? string.Empty;
        StandardError = standardError ?? string.Empty;
    }

    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    /// <value>
    /// <placeholder>The exit code of the process.</placeholder>
    /// </value>
    public int ExitCode { get; }

    /// <summary>
    /// Gets the standard output from the process.
    /// </summary>
    /// <value>
    /// <placeholder>The standard output from the process.</placeholder>
    /// </value>
    public string StandardOutput { get; }

    /// <summary>
    /// Gets the standard error from the process.
    /// </summary>
    /// <value>
    /// <placeholder>The standard error from the process.</placeholder>
    /// </value>
    public string StandardError { get; }

    /// <summary>
    /// Gets a value indicating whether the process completed successfully (exit code 0).
    /// </summary>
    /// <value>
    /// <placeholder>A value indicating whether the process completed successfully (exit code 0).</placeholder>
    /// </value>
    public bool Success => ExitCode == 0;

    /// <summary>
    /// Creates a successful result with the specified output.
    /// </summary>
    /// <param name="standardOutput">The standard output.</param>
    /// <returns>A successful process result.</returns>
    public static ProcessResult Succeeded(string standardOutput = "")
        => new(0, standardOutput, string.Empty);

    /// <summary>
    /// Creates a failed result with the specified exit code and error.
    /// </summary>
    /// <param name="exitCode">The exit code (non-zero).</param>
    /// <param name="standardError">The standard error output.</param>
    /// <returns>A failed process result.</returns>
    public static ProcessResult Failed(int exitCode, string standardError = "")
        => new(exitCode, string.Empty, standardError);
}
