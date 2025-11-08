namespace InsightLog;

/// <summary>
/// Immutable representation of a log event with all captured context.
/// </summary>
public sealed record LogEvent
{
    /// <summary>
    /// Gets the timestamp when the event occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Gets the severity level of the event.
    /// </summary>
    public LogLevel Level { get; init; }
    
    /// <summary>
    /// Gets the formatted message.
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the exception associated with this event, if any.
    /// </summary>
    public Exception? Exception { get; init; }
    
    /// <summary>
    /// Gets the source file path where the log was called.
    /// </summary>
    public string? CallerFilePath { get; init; }
    
    /// <summary>
    /// Gets the member name where the log was called.
    /// </summary>
    public string? CallerMemberName { get; init; }
    
    /// <summary>
    /// Gets the line number where the log was called.
    /// </summary>
    public int? CallerLineNumber { get; init; }
    
    /// <summary>
    /// Gets the correlation ID for tracing related events.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the thread ID where the event occurred.
    /// </summary>
    public int ThreadId { get; init; }
    
    /// <summary>
    /// Gets the task ID if running in an async context.
    /// </summary>
    public int? TaskId { get; init; }
    
    /// <summary>
    /// Gets the current scope depth for indentation.
    /// </summary>
    public int ScopeDepth { get; init; }
    
    /// <summary>
    /// Gets additional properties associated with the event.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; init; } = 
        new Dictionary<string, object?>();
    
    /// <summary>
    /// Gets the elapsed time in milliseconds, if measured.
    /// </summary>
    public double? ElapsedMs { get; init; }
    
    /// <summary>
    /// Gets whether this operation exceeded the slow threshold.
    /// </summary>
    public bool IsSlow { get; init; }
}
