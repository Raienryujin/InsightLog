using System.Diagnostics;

namespace InsightLog;

/// <summary>
/// Represents a logging scope that tracks execution time and manages indentation.
/// </summary>
public sealed class LogScope : IDisposable, IAsyncDisposable
{
    private readonly InsightLogger _logger;
    private readonly string _name;
    private readonly Stopwatch _stopwatch;
    private readonly string? _memberName;
    private readonly string? _filePath;
    private readonly int _lineNumber;
    private readonly int _previousDepth;
    private bool _disposed;
    
    internal LogScope(
        InsightLogger logger,
        string name,
        string memberName,
        string filePath,
        int lineNumber)
    {
        _logger = logger;
        _name = name;
        _memberName = memberName;
        _filePath = filePath;
        _lineNumber = lineNumber;
        _stopwatch = Stopwatch.StartNew();
        
        _previousDepth = ScopeContext.CurrentDepth;
        ScopeContext.CurrentDepth++;
        
        _logger.LogScopeStart(_name, _memberName, _filePath, _lineNumber);
    }
    
    /// <summary>
    /// Disposes the scope and logs the elapsed time.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _stopwatch.Stop();
        ScopeContext.CurrentDepth = _previousDepth;
        
        _logger.LogScopeEnd(_name, _stopwatch.Elapsed.TotalMilliseconds, 
            _memberName, _filePath, _lineNumber);
    }
    
    /// <summary>
    /// Asynchronously disposes the scope and logs the elapsed time.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Provides AsyncLocal storage for scope depth.
/// </summary>
public static class ScopeContext
{
    private static readonly AsyncLocal<int> _scopeDepth = new();
    
    /// <summary>
    /// Gets or sets the current scope depth for the async context.
    /// </summary>
    public static int CurrentDepth
    {
        get => _scopeDepth.Value;
        set => _scopeDepth.Value = value;
    }
}
