using System.Diagnostics;
using System.Runtime.CompilerServices;
using InsightLog.Configuration;
using InsightLog.Internal;

namespace InsightLog;

/// <summary>
/// The main logger implementation for InsightLog.
/// </summary>
public sealed class InsightLogger : ILogger, IDisposable
{
    private readonly LogOptions _options;
    private readonly Random _sampler = Random.Shared;
    private bool _disposed;
    
    /// <summary>
    /// Initializes a new instance of the InsightLogger class.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    public InsightLogger(LogOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    /// <summary>
    /// Creates a new logger instance with the specified configuration.
    /// </summary>
    /// <param name="configure">An action to configure the logger options.</param>
    /// <returns>A new logger instance.</returns>
    public static InsightLogger Create(Action<LogOptions>? configure = null)
    {
        var options = new LogOptions();
        configure?.Invoke(options);
        return new InsightLogger(options);
    }
    
    /// <inheritdoc />
    public void Trace(string messageTemplate, params object?[] args)
        => Log(LogLevel.Trace, messageTemplate, args);
    
    /// <inheritdoc />
    public void Debug(string messageTemplate, params object?[] args)
        => Log(LogLevel.Debug, messageTemplate, args);
    
    /// <inheritdoc />
    public void Info(string messageTemplate, params object?[] args)
        => Log(LogLevel.Info, messageTemplate, args);
    
    /// <inheritdoc />
    public void Warn(string messageTemplate, params object?[] args)
        => Log(LogLevel.Warn, messageTemplate, args);
    
    /// <inheritdoc />
    public void Error(string messageTemplate, params object?[] args)
        => Log(LogLevel.Error, messageTemplate, args);
    
    /// <inheritdoc />
    public void Error(Exception exception, string messageTemplate, params object?[] args)
        => Log(LogLevel.Error, exception, messageTemplate, args);
    
    /// <inheritdoc />
    public void Fatal(string messageTemplate, params object?[] args)
        => Log(LogLevel.Fatal, messageTemplate, args);
    
    /// <inheritdoc />
    public void Fatal(Exception exception, string messageTemplate, params object?[] args)
        => Log(LogLevel.Fatal, exception, messageTemplate, args);
    
    /// <inheritdoc />
    public void Log(LogLevel level, string messageTemplate, params object?[] args)
        => LogInternal(level, null, messageTemplate, args);
    
    /// <inheritdoc />
    public void Log(LogLevel level, Exception exception, string messageTemplate, params object?[] args)
        => LogInternal(level, exception, messageTemplate, args);
    
    /// <inheritdoc />
    public IDisposable Scope(string name,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        return new LogScope(this, name, memberName, filePath, lineNumber);
    }
    
    /// <inheritdoc />
    public IDisposable Measure(string operationName,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        return new LogScope(this, $"Measure:{operationName}", memberName, filePath, lineNumber);
    }
    
    /// <inheritdoc />
    public bool IsEnabled(LogLevel level) => level >= _options.MinimumLevel;
    
    private void LogInternal(
        LogLevel level,
        Exception? exception,
        string messageTemplate,
        object?[] args,
        double? elapsedMs = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (_disposed) return;
        if (!IsEnabled(level)) return;
        
        // Apply sampling
        if (_options.SampleRate > 1 && _sampler.Next(_options.SampleRate) != 0)
            return;
        
        var (message, properties) = MessageTemplateFormatter.Format(
            messageTemplate, 
            args, 
            _options.RedactionRules,
            _options.MaxMessageLength);
        
        var correlationId = GetCorrelationId();
        var isSlow = elapsedMs.HasValue && elapsedMs.Value >= _options.SlowThresholdMs;
        
        var logEvent = new LogEvent
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            Exception = exception,
            CallerFilePath = _options.IncludeCallerInfo ? Path.GetFileName(filePath) : null,
            CallerMemberName = _options.IncludeCallerInfo ? memberName : null,
            CallerLineNumber = _options.IncludeCallerInfo ? lineNumber : null,
            CorrelationId = correlationId,
            ThreadId = Environment.CurrentManagedThreadId,
            TaskId = Task.CurrentId,
            ScopeDepth = _options.IncludeScopes ? ScopeContext.CurrentDepth : 0,
            Properties = properties,
            ElapsedMs = elapsedMs,
            IsSlow = isSlow
        };
        
        // Write to all sinks
        if (_options.Sinks.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                var tasks = _options.Sinks.Select(sink => 
                    sink.WriteAsync(logEvent, CancellationToken.None));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            });
        }
    }
    
    internal void LogScopeStart(string name, string? memberName, string? filePath, int lineNumber)
    {
        if (_options.IncludeScopes)
        {
            LogInternal(LogLevel.Debug, null, "↳ {ScopeName} started", 
                new object?[] { name }, null, memberName ?? "", filePath ?? "", lineNumber);
        }
    }
    
    internal void LogScopeEnd(string name, double elapsedMs, string? memberName, string? filePath, int lineNumber)
    {
        if (_options.IncludeScopes)
        {
            var level = elapsedMs >= _options.SlowThresholdMs ? LogLevel.Warn : LogLevel.Debug;
            LogInternal(level, null, "↳ {ScopeName} completed in {ElapsedMs:F2}ms", 
                new object?[] { name, elapsedMs }, elapsedMs, memberName ?? "", filePath ?? "", lineNumber);
        }
    }
    
    private static string GetCorrelationId()
    {
        var activityId = Activity.Current?.Id;
        if (!string.IsNullOrEmpty(activityId))
        {
            // Take last 8 chars of activity ID for brevity
            return activityId.Length > 8 
                ? activityId[^8..] 
                : activityId;
        }
        
        // Generate a short correlation ID
        return Guid.NewGuid().ToString("N")[..8];
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        foreach (var sink in _options.Sinks)
        {
            try
            {
                sink.FlushAsync().GetAwaiter().GetResult();
                sink.Dispose();
            }
            catch
            {
                // Best effort
            }
        }
    }
}
