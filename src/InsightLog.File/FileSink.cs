using System.Text;
using InsightLog.Configuration;
using InsightLog.Sinks;

namespace InsightLog.File;

/// <summary>
/// A sink that writes log events to rolling files with date-based rotation.
/// </summary>
public class FileSink : ILogSink
{
    private readonly string _pathTemplate;
    private readonly LogOptions _options;
    private readonly long _maxFileSize;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private StreamWriter? _currentWriter;
    private string? _currentPath;
    private DateTime _currentDate;
    private long _currentSize;
    
    /// <summary>
    /// Initializes a new instance of the FileSink class.
    /// </summary>
    /// <param name="pathTemplate">The file path template (e.g., "logs/app-.log" becomes "logs/app-2025-01-15.log").</param>
    /// <param name="options">The log options.</param>
    /// <param name="maxFileSizeMB">Maximum file size in megabytes before rolling.</param>
    public FileSink(string pathTemplate, LogOptions? options = null, int maxFileSizeMB = 100)
    {
        _pathTemplate = pathTemplate ?? throw new ArgumentNullException(nameof(pathTemplate));
        _options = options ?? new LogOptions();
        _maxFileSize = maxFileSizeMB * 1024L * 1024L;
    }
    
    /// <inheritdoc />
    public async Task WriteAsync(LogEvent logEvent, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureWriterAsync(logEvent.Timestamp).ConfigureAwait(false);
            
            var line = _options.OutputFormat == OutputFormat.Json 
                ? FormatJson(logEvent) 
                : FormatText(logEvent);
            
            if (_currentWriter != null)
            {
                await _currentWriter.WriteLineAsync(line).ConfigureAwait(false);
                await _currentWriter.FlushAsync().ConfigureAwait(false);
                _currentSize += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
                
                // Check if we need to roll due to size
                if (_currentSize >= _maxFileSize)
                {
                    await RollFileAsync().ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private async Task EnsureWriterAsync(DateTime timestamp)
    {
        var date = timestamp.Date;
        
        // Roll if date changed
        if (_currentWriter != null && date != _currentDate)
        {
            await RollFileAsync().ConfigureAwait(false);
        }
        
        if (_currentWriter == null)
        {
            var path = GetFilePath(date);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            _currentPath = path;
            _currentDate = date;
            
            // Check existing file size
            if (System.IO.File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                _currentSize = fileInfo.Length;
                
                // If existing file is too large, create a new one with sequence number
                if (_currentSize >= _maxFileSize)
                {
                    path = GetSequencedPath(path);
                    _currentPath = path;
                    _currentSize = 0;
                }
            }
            else
            {
                _currentSize = 0;
            }
            
            _currentWriter = new StreamWriter(path, append: true, Encoding.UTF8)
            {
                AutoFlush = false
            };
        }
    }
    
    private string GetFilePath(DateTime date)
    {
        var insertPos = _pathTemplate.LastIndexOf('.');
        if (insertPos == -1)
        {
            return $"{_pathTemplate}-{date:yyyy-MM-dd}";
        }
        
        return _pathTemplate.Insert(insertPos, $"-{date:yyyy-MM-dd}");
    }
    
    private string GetSequencedPath(string basePath)
    {
        var sequence = 1;
        string path;
        
        do
        {
            var insertPos = basePath.LastIndexOf('.');
            if (insertPos == -1)
            {
                path = $"{basePath}.{sequence:D3}";
            }
            else
            {
                path = basePath.Insert(insertPos, $".{sequence:D3}");
            }
            sequence++;
        }
        while (System.IO.File.Exists(path) && sequence < 1000);
        
        return path;
    }
    
    private async Task RollFileAsync()
    {
        if (_currentWriter != null)
        {
            await _currentWriter.FlushAsync().ConfigureAwait(false);
            await _currentWriter.DisposeAsync().ConfigureAwait(false);
            _currentWriter = null;
            _currentPath = null;
            _currentSize = 0;
        }
    }
    
    /// <summary>
    /// Formats a log event as a human-readable text line for output to the log file.
    /// </summary>
    /// <param name="logEvent">The log event to format.</param>
    /// <returns>A formatted string representing the log event.</returns>
    protected virtual string FormatText(LogEvent logEvent)
    {
        var sb = new StringBuilder();
        
        // Basic format: [timestamp] [level] [correlation] message
        sb.Append($"[{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
        sb.Append($"[{logEvent.Level.ToString().ToUpperInvariant()}] ");
        sb.Append($"[corr:{logEvent.CorrelationId}] ");
        
        if (_options.IncludeCallerInfo && logEvent.CallerMemberName != null)
        {
            sb.Append($"[{logEvent.CallerMemberName}@{logEvent.CallerFilePath}:{logEvent.CallerLineNumber}] ");
        }
        
        // Indentation for scopes
        if (_options.IncludeScopes && logEvent.ScopeDepth > 0)
        {
            sb.Append(new string(' ', logEvent.ScopeDepth * 2));
        }
        
        sb.Append(logEvent.Message);
        
        // Properties
        if (logEvent.Properties.Count > 0)
        {
            sb.Append(" | Props: ");
            foreach (var (key, value) in logEvent.Properties)
            {
                sb.Append($"{key}={value ?? "null"} ");
            }
        }
        
        // Elapsed time
        if (logEvent.ElapsedMs.HasValue)
        {
            sb.Append($" | Elapsed: {logEvent.ElapsedMs.Value:F2}ms");
            if (logEvent.IsSlow)
            {
                sb.Append(" [SLOW]");
            }
        }
        
        // Exception
        if (logEvent.Exception != null)
        {
            sb.AppendLine();
            sb.AppendLine($"  Exception: {logEvent.Exception.GetType().FullName}: {logEvent.Exception.Message}");
            if (!string.IsNullOrEmpty(logEvent.Exception.StackTrace))
            {
                sb.AppendLine("  Stack Trace:");
                foreach (var line in logEvent.Exception.StackTrace.Split('\n'))
                {
                    sb.AppendLine($"    {line.Trim()}");
                }
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Formats a log event as a JSON string for output to the log file.
    /// </summary>
    /// <param name="logEvent">The log event to format.</param>
    /// <returns>A JSON-formatted string representing the log event.</returns>
    protected virtual string FormatJson(LogEvent logEvent)
    {
        var json = new StringBuilder();
        json.Append('{');
        json.Append($"\"timestamp\":\"{logEvent.Timestamp:O}\",");
        json.Append($"\"level\":\"{logEvent.Level}\",");
        json.Append($"\"message\":\"{EscapeJson(logEvent.Message)}\",");
        json.Append($"\"correlationId\":\"{logEvent.CorrelationId}\",");
        json.Append($"\"threadId\":{logEvent.ThreadId}");
        
        if (logEvent.TaskId.HasValue)
        {
            json.Append($",\"taskId\":{logEvent.TaskId.Value}");
        }

        if (_options.IncludeScopes)
        {
            json.Append($",\"scopeDepth\":{logEvent.ScopeDepth}");
        }

        if (logEvent.ElapsedMs.HasValue)
        {
            json.Append($",\"elapsedMs\":{logEvent.ElapsedMs.Value:F2}");
            if (logEvent.IsSlow)
            {
                json.Append(",\"isSlow\":true");
            }
        }
        
        if (_options.IncludeCallerInfo && logEvent.CallerMemberName != null)
        {
            json.Append($",\"caller\":{{\"member\":\"{EscapeJson(logEvent.CallerMemberName)}\"");
            json.Append($",\"file\":\"{EscapeJson(logEvent.CallerFilePath ?? "")}\"");
            json.Append($",\"line\":{logEvent.CallerLineNumber ?? 0}}}");
        }
        
        if (logEvent.Properties.Count > 0)
        {
            json.Append(",\"properties\":{");
            var first = true;
            foreach (var (key, value) in logEvent.Properties)
            {
                if (!first)
                {
                    json.Append(',');
                }

                first = false;
                json.Append($"\"{EscapeJson(key)}\":");
                FormatJsonValue(json, value);
            }
            json.Append('}');
        }
        
        if (logEvent.Exception != null)
        {
            json.Append(",\"exception\":{");
            json.Append($"\"type\":\"{EscapeJson(logEvent.Exception.GetType().FullName ?? "")}\"");
            json.Append($",\"message\":\"{EscapeJson(logEvent.Exception.Message)}\"");
            if (!string.IsNullOrEmpty(logEvent.Exception.StackTrace))
            {
                json.Append($",\"stackTrace\":\"{EscapeJson(logEvent.Exception.StackTrace)}\"");
            }
            json.Append('}');
        }
        
        json.Append('}');
        return json.ToString();
    }
    
    private static void FormatJsonValue(StringBuilder json, object? value)
    {
        if (value == null)
        {
            json.Append("null");
        }
        else if (value is string s)
        {
            json.Append($"\"{EscapeJson(s)}\"");
        }
        else if (value is bool b)
        {
            json.Append(b ? "true" : "false");
        }
        else if (value is IFormattable f)
        {
            json.Append(f.ToString(null, System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            json.Append($"\"{EscapeJson(value.ToString() ?? "")}\"");
        }
    }
    
    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
    
    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_currentWriter != null)
            {
                await _currentWriter.FlushAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        _currentWriter?.Dispose();
        _semaphore.Dispose();
    }
}
