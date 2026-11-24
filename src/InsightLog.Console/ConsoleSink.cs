using System.Globalization;
using System.Text;
using InsightLog.Configuration;
using InsightLog.Sinks;

namespace InsightLog.Console;

/// <summary>
/// A sink that writes log events to the console with optional colorization.
/// </summary>
public sealed class ConsoleSink : ILogSink
{
    private readonly LogOptions _options;
    private readonly ConsoleColorTheme _theme;
    private readonly object _lock = new();
    
    /// <summary>
    /// Initializes a new instance of the ConsoleSink class.
    /// </summary>
    /// <param name="options">The log options.</param>
    /// <param name="theme">The color theme to use.</param>
    public ConsoleSink(LogOptions? options = null, ConsoleColorTheme? theme = null)
    {
        _options = options ?? new LogOptions();
        _theme = theme ?? ConsoleColorTheme.Default;
    }
    
    /// <inheritdoc />
    public Task WriteAsync(LogEvent logEvent, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.CompletedTask;
        
        lock (_lock)
        {
            if (_options.OutputFormat == OutputFormat.Json)
            {
                WriteJson(logEvent);
            }
            else
            {
                WriteText(logEvent);
            }
        }
        
        return Task.CompletedTask;
    }
    
    private void WriteText(LogEvent logEvent)
    {
        var originalColor = System.Console.ForegroundColor;
        var originalBackground = System.Console.BackgroundColor;
        
        try
        {
            // Apply indentation for scopes
            var indent = new string(' ', logEvent.ScopeDepth * 2);
            
            // Timestamp
            SetColor(ConsoleColor.DarkGray);
            System.Console.Write($"[{logEvent.Timestamp:HH:mm:ss.fff}] ");
            
            // Level with color
            var (levelText, levelColor) = GetLevelDisplay(logEvent.Level);
            SetColor(levelColor);
            System.Console.Write($"[{levelText}] ");
            
            // Correlation ID
            SetColor(ConsoleColor.DarkCyan);
            System.Console.Write($"[corr:{logEvent.CorrelationId}] ");
            
            // Caller info
            if (_options.IncludeCallerInfo && logEvent.CallerMemberName != null)
            {
                SetColor(ConsoleColor.DarkGray);
                System.Console.Write($"[{logEvent.CallerMemberName}@{logEvent.CallerFilePath}:{logEvent.CallerLineNumber}] ");
            }
            
            // Reset color for message
            if (_options.UseColors)
            {
                System.Console.ForegroundColor = originalColor;
            }
            
            // Message with indentation
            System.Console.WriteLine(indent + logEvent.Message);
            
            // Properties
            if (logEvent.Properties.Count > 0)
            {
                SetColor(ConsoleColor.DarkGray);
                System.Console.Write(indent + "  ↳ props: { ");
                
                var first = true;
                foreach (var (key, value) in logEvent.Properties)
                {
                    if (!first) System.Console.Write(", ");
                    first = false;
                    
                    System.Console.Write($"{key}=");
                    if (value?.ToString()?.Contains("REDACTED") == true)
                    {
                        SetColor(ConsoleColor.DarkRed);
                        System.Console.Write(value);
                        SetColor(ConsoleColor.DarkGray);
                    }
                    else
                    {
                        System.Console.Write(value ?? "null");
                    }
                }
                System.Console.WriteLine(" }");
            }
            
            // Slow marker
            if (logEvent.IsSlow)
            {
                SetColor(ConsoleColor.Yellow);
                System.Console.WriteLine(indent + "  ⚠ SLOW OPERATION");
            }
            
            // Exception
            if (logEvent.Exception != null)
            {
                SetColor(ConsoleColor.Red);
                System.Console.WriteLine(indent + "  Exception: " + logEvent.Exception.GetType().Name);
                System.Console.WriteLine(indent + "  " + logEvent.Exception.Message);
                if (!string.IsNullOrEmpty(logEvent.Exception.StackTrace))
                {
                    SetColor(ConsoleColor.DarkRed);
                    var stackLines = logEvent.Exception.StackTrace.Split('\n');
                    foreach (var line in stackLines.Take(5)) // Limit stack trace
                    {
                        System.Console.WriteLine(indent + "    " + line.Trim());
                    }
                }
            }
        }
        finally
        {
            if (_options.UseColors)
            {
                System.Console.ForegroundColor = originalColor;
                System.Console.BackgroundColor = originalBackground;
            }
        }
    }

    private void SetColor(ConsoleColor color)
    {
        if (_options.UseColors)
        {
            System.Console.ForegroundColor = color;
        }
    }
    
    private void WriteJson(LogEvent logEvent)
    {
        var json = new StringBuilder();
        json.Append('{');
        json.Append($"\"timestamp\":\"{logEvent.Timestamp:O}\",");
        json.Append($"\"level\":\"{logEvent.Level}\",");
        json.Append($"\"message\":\"{EscapeJson(logEvent.Message)}\",");
        json.Append($"\"correlationId\":\"{logEvent.CorrelationId}\",");
        json.Append($"\"threadId\":{logEvent.ThreadId},");
        
        if (logEvent.TaskId.HasValue)
            json.Append($"\"taskId\":{logEvent.TaskId.Value},");
        
        if (_options.IncludeScopes)
            json.Append($"\"scopeDepth\":{logEvent.ScopeDepth},");
        
        if (_options.IncludeCallerInfo && logEvent.CallerMemberName != null)
        {
            json.Append($"\"caller\":{{");
            json.Append($"\"member\":\"{EscapeJson(logEvent.CallerMemberName)}\",");
            json.Append($"\"file\":\"{EscapeJson(logEvent.CallerFilePath ?? "")}\",");
            json.Append($"\"line\":{logEvent.CallerLineNumber ?? 0}");
            json.Append("},");
        }
        
        if (logEvent.ElapsedMs.HasValue)
        {
            json.Append($"\"elapsedMs\":{logEvent.ElapsedMs.Value.ToString("F2", CultureInfo.InvariantCulture)},");
            if (logEvent.IsSlow)
                json.Append("\"isSlow\":true,");
        }
        
        if (logEvent.Properties.Count > 0)
        {
            json.Append("\"properties\":{");
            var first = true;
            foreach (var (key, value) in logEvent.Properties)
            {
                if (!first) json.Append(',');
                first = false;
                json.Append($"\"{EscapeJson(key)}\":");
                FormatJsonValue(json, value);
            }
            json.Append("},");
        }
        
        if (logEvent.Exception != null)
        {
            json.Append("\"exception\":{");
            json.Append($"\"type\":\"{EscapeJson(logEvent.Exception.GetType().FullName ?? "")}\",");
            json.Append($"\"message\":\"{EscapeJson(logEvent.Exception.Message)}\",");
            json.Append($"\"stackTrace\":\"{EscapeJson(logEvent.Exception.StackTrace ?? "")}\"");
            json.Append("},");
        }
        
        // Remove trailing comma and close
        if (json[^1] == ',')
            json.Length--;
        json.Append('}');
        
        System.Console.WriteLine(json.ToString());
    }

    private static void FormatJsonValue(StringBuilder json, object? value)
    {
        if (value == null)
        {
            json.Append("null");
        }
        else if (value is bool b)
        {
            json.Append(b ? "true" : "false");
        }
        else if (value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
        {
            json.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
        }
        else if (value is DateTime dt)
        {
            json.Append($"\"{dt:O}\"");
        }
        else if (value is DateTimeOffset dto)
        {
            json.Append($"\"{dto:O}\"");
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
    
    private (string text, ConsoleColor color) GetLevelDisplay(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => ("TRC", _theme.TraceColor),
            LogLevel.Debug => ("DBG", _theme.DebugColor),
            LogLevel.Info => ("INF", _theme.InfoColor),
            LogLevel.Warn => ("WRN", _theme.WarnColor),
            LogLevel.Error => ("ERR", _theme.ErrorColor),
            LogLevel.Fatal => ("FTL", _theme.FatalColor),
            _ => ("???", ConsoleColor.Gray)
        };
    }
    
    /// <inheritdoc />
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to dispose
    }
}

/// <summary>
/// Defines color themes for console output.
/// </summary>
public sealed class ConsoleColorTheme
{
    /// <summary>
    /// Gets the default color theme.
    /// </summary>
    public static ConsoleColorTheme Default { get; } = new();
    
    /// <summary>
    /// Gets or sets the color for trace-level messages.
    /// </summary>
    public ConsoleColor TraceColor { get; set; } = ConsoleColor.DarkGray;
    
    /// <summary>
    /// Gets or sets the color for debug-level messages.
    /// </summary>
    public ConsoleColor DebugColor { get; set; } = ConsoleColor.Gray;
    
    /// <summary>
    /// Gets or sets the color for info-level messages.
    /// </summary>
    public ConsoleColor InfoColor { get; set; } = ConsoleColor.Cyan;
    
    /// <summary>
    /// Gets or sets the color for warning-level messages.
    /// </summary>
    public ConsoleColor WarnColor { get; set; } = ConsoleColor.Yellow;
    
    /// <summary>
    /// Gets or sets the color for error-level messages.
    /// </summary>
    public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;
    
    /// <summary>
    /// Gets or sets the color for fatal-level messages.
    /// </summary>
    public ConsoleColor FatalColor { get; set; } = ConsoleColor.Magenta;
}
