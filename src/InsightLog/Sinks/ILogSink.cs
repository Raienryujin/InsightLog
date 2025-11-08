namespace InsightLog.Sinks;

/// <summary>
/// Defines the contract for log sinks that handle log events.
/// </summary>
public interface ILogSink : IDisposable
{
    /// <summary>
    /// Writes a log event to the sink.
    /// </summary>
    /// <param name="logEvent">The log event to write.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteAsync(LogEvent logEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Flushes any buffered log events.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
