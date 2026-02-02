// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Dottie.Configuration.Installing.Utilities;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public class ProcessResult
{
    /// <summary>
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
    public int ExitCode { get; }

    /// <summary>
    /// Gets the standard output from the process.
    /// </summary>
    public string StandardOutput { get; }

    /// <summary>
    /// Gets the standard error from the process.
    /// </summary>
    public string StandardError { get; }

    /// <summary>
    /// Gets a value indicating whether the process completed successfully (exit code 0).
    /// </summary>
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
