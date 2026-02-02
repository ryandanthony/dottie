// Licensed under the MIT License. See LICENSE in the project root for license information.

using Dottie.Configuration.Installing.Utilities;

namespace Dottie.Configuration.Tests.Fakes;

/// <summary>
/// A fake implementation of <see cref="IProcessRunner"/> for unit testing.
/// Allows configuring predetermined results for process executions.
/// </summary>
public class FakeProcessRunner : IProcessRunner
{
    private readonly Queue<object> _results = new(); // Can hold ProcessResult or Exception
    private readonly List<(string FileName, string Arguments, string? WorkingDirectory)> _calls = new();

    /// <summary>
    /// Gets the list of recorded calls made to this process runner.
    /// </summary>
    public IReadOnlyList<(string FileName, string Arguments, string? WorkingDirectory)> Calls => _calls;

    /// <summary>
    /// Gets the number of times RunAsync or Run was called.
    /// </summary>
    public int CallCount => _calls.Count;

    /// <summary>
    /// Creates a new instance of <see cref="FakeProcessRunner"/> with a default successful result.
    /// </summary>
    public FakeProcessRunner()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="FakeProcessRunner"/> with a single result.
    /// </summary>
    /// <param name="result">The result to return for all calls.</param>
    public FakeProcessRunner(ProcessResult result)
    {
        _results.Enqueue(result);
    }

    /// <summary>
    /// Queues a result to be returned by the next call to RunAsync or Run.
    /// </summary>
    /// <param name="result">The result to return.</param>
    /// <returns>This instance for fluent configuration.</returns>
    public FakeProcessRunner WithResult(ProcessResult result)
    {
        _results.Enqueue(result);
        return this;
    }

    /// <summary>
    /// Queues a successful result with the specified output.
    /// </summary>
    /// <param name="output">The standard output to return.</param>
    /// <returns>This instance for fluent configuration.</returns>
    public FakeProcessRunner WithSuccessResult(string output = "")
    {
        _results.Enqueue(ProcessResult.Succeeded(output));
        return this;
    }

    /// <summary>
    /// Queues a failed result with the specified exit code and error.
    /// </summary>
    /// <param name="exitCode">The exit code to return.</param>
    /// <param name="error">The standard error to return.</param>
    /// <returns>This instance for fluent configuration.</returns>
    public FakeProcessRunner WithFailureResult(int exitCode = 1, string error = "")
    {
        _results.Enqueue(ProcessResult.Failed(exitCode, error));
        return this;
    }

    /// <summary>
    /// Queues an exception to be thrown on the next call.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>This instance for fluent configuration.</returns>
    public FakeProcessRunner WithException(Exception exception)
    {
        _results.Enqueue(exception);
        return this;
    }

    /// <inheritdoc/>
    public Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Run(fileName, arguments, workingDirectory));
    }

    /// <inheritdoc/>
    public ProcessResult Run(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        int? timeoutMilliseconds = null)
    {
        _calls.Add((fileName, arguments, workingDirectory));

        if (_results.Count == 0)
        {
            // Return a default success result if no results are queued
            return ProcessResult.Succeeded();
        }

        // If only one result is queued, reuse it for all calls
        object result;
        if (_results.Count == 1)
        {
            result = _results.Peek();
        }
        else
        {
            result = _results.Dequeue();
        }

        // Handle exceptions
        if (result is Exception ex)
        {
            throw ex;
        }

        return (ProcessResult)result;
    }

    /// <summary>
    /// Resets the call history.
    /// </summary>
    public void Reset()
    {
        _calls.Clear();
    }
}
