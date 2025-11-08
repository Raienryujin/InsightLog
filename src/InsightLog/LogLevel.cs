namespace InsightLog;

/// <summary>
/// Defines the severity levels for log messages.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Detailed diagnostic information, typically of interest only when diagnosing problems.
    /// </summary>
    Trace = 0,
    
    /// <summary>
    /// Debugging information, less detailed than Trace.
    /// </summary>
    Debug = 1,
    
    /// <summary>
    /// General informational messages that confirm things are working as expected.
    /// </summary>
    Info = 2,
    
    /// <summary>
    /// Warnings about potentially harmful situations.
    /// </summary>
    Warn = 3,
    
    /// <summary>
    /// Error messages indicating something has failed.
    /// </summary>
    Error = 4,
    
    /// <summary>
    /// Critical failures that require immediate attention.
    /// </summary>
    Fatal = 5
}
