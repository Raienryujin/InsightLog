using System.Runtime.CompilerServices;

namespace InsightLog;

/// <summary>
/// Defines the contract for a logger instance.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs a trace-level message.
    /// </summary>
    void Trace(string messageTemplate, params object?[] args);
    
    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    void Debug(string messageTemplate, params object?[] args);
    
    /// <summary>
    /// Logs an info-level message.
    /// </summary>
    void Info(string messageTemplate, params object?[] args);
    
    /// <summary>
    /// Logs a warning-level message.
    /// </summary>
    void Warn(string messageTemplate, params object?[] args);
    
    /// <summary>
    /// Logs an error-level message.
    /// </summary>
    void Error(string messageTemplate, params object?[] args);
    
    /// <summary>
    /// Logs an error-level message with an exception.
    /// </summary>
    void Error(Exception exception, string messageTemplate, params object?[] args);
    
    /// <summary>
    /// Logs a fatal-level message.
    /// </summary>
    void Fatal(string messageTemplate, params object?[] args);
    
    /// <summary>
    /// Logs a fatal-level message with an exception.
    /// </summary>
    void Fatal(Exception exception, string messageTemplate, params object?[] args);
    
    /// <summary>
    /// Logs a message at the specified level.
    /// </summary>
    void Log(LogLevel level, string messageTemplate, params object?[] args);
    
    /// <summary>
    /// Logs a message at the specified level with an exception.
    /// </summary>
    void Log(LogLevel level, Exception exception, string messageTemplate, params object?[] args);
    
    /// <summary>
    /// Creates a new scope for grouping related log messages.
    /// </summary>
    /// <param name="name">The name of the scope.</param>
    /// <param name="memberName">Auto-captured member name.</param>
    /// <param name="filePath">Auto-captured file path.</param>
    /// <param name="lineNumber">Auto-captured line number.</param>
    /// <returns>A disposable scope.</returns>
    IDisposable Scope(string name,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0);
    
    /// <summary>
    /// Measures the execution time of a code block.
    /// </summary>
    /// <param name="operationName">The name of the operation being measured.</param>
    /// <param name="memberName">Auto-captured member name.</param>
    /// <param name="filePath">Auto-captured file path.</param>
    /// <param name="lineNumber">Auto-captured line number.</param>
    /// <returns>A disposable scope that measures execution time.</returns>
    IDisposable Measure(string operationName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0);
    
    /// <summary>
    /// Checks if the specified log level is enabled.
    /// </summary>
    /// <param name="level">The log level to check.</param>
    /// <returns>True if the level is enabled; otherwise, false.</returns>
    bool IsEnabled(LogLevel level);
}
