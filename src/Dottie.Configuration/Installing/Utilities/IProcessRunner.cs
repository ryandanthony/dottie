// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Dottie.Configuration.Installing.Utilities;

/// <summary>
/// Abstraction for running external processes.
/// Enables unit testing by allowing process execution to be mocked.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process asynchronously and returns the result.
    /// </summary>
    /// <param name="fileName">The name of the executable to run.</param>
    /// <param name="arguments">The command-line arguments for the process.</param>
    /// <param name="workingDirectory">Optional working directory for the process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The result of the process execution.</returns>
    Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a process synchronously and returns the result.
    /// </summary>
    /// <param name="fileName">The name of the executable to run.</param>
    /// <param name="arguments">The command-line arguments for the process.</param>
    /// <param name="workingDirectory">Optional working directory for the process.</param>
    /// <param name="timeoutMilliseconds">Optional timeout in milliseconds.</param>
    /// <returns>The result of the process execution.</returns>
    ProcessResult Run(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        int? timeoutMilliseconds = null);
}
