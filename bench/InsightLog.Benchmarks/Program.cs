using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using InsightLog;
using InsightLog.Configuration;
using InsightLog.Sinks;

namespace InsightLog.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<LoggerBenchmarks>();
    }
}

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class LoggerBenchmarks
{
    private InsightLogger _logger = null!;
    private InsightLogger _loggerWithSink = null!;
    private InsightLogger _loggerWithRedaction = null!;
    private ILogSink _noopSink = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _noopSink = new NoopSink();
        
        // Logger with minimum level filtering (no-op scenario)
        _logger = InsightLogger.Create(o =>
        {
            o.MinimumLevel = LogLevel.Error; // Most logs will be filtered
        });
        
        // Logger with actual sink
        _loggerWithSink = InsightLogger.Create(o =>
        {
            o.MinimumLevel = LogLevel.Debug;
            o.Sinks.Add(_noopSink);
        });
        
        // Logger with redaction rules
        _loggerWithRedaction = InsightLogger.Create(o =>
        {
            o.MinimumLevel = LogLevel.Debug;
            o.Redact("password", "apiKey", @"\b\d{4}\b");
            o.Sinks.Add(_noopSink);
        });
    }
    
    [Benchmark(Baseline = true)]
    public void NoOp_FilteredLog()
    {
        // This should be filtered out and do minimal work
        _logger.Debug("This message is filtered with {Value}", 42);
    }
    
    [Benchmark]
    public void SimpleLog_WithSink()
    {
        _loggerWithSink.Info("Simple message with {Value}", 42);
    }
    
    [Benchmark]
    public void ComplexLog_MultipleProperties()
    {
        _loggerWithSink.Info("User {UserId} performed {Action} on {Resource} at {Time}",
            "USER-123", "UPDATE", "Document", DateTime.UtcNow);
    }
    
    [Benchmark]
    public void Log_WithRedaction()
    {
        _loggerWithRedaction.Info("Login attempt: {Username} with {Password}",
            "john.doe", "secret123");
    }
    
    [Benchmark]
    public void Scope_Creation()
    {
        using (var scope = _loggerWithSink.Scope("TestScope"))
        {
            // Just measure scope overhead
        }
    }
    
    [Benchmark]
    public void Measure_Operation()
    {
        using (var measure = _loggerWithSink.Measure("TestOperation"))
        {
            // Simulate some work
            Thread.SpinWait(100);
        }
    }
    
    [Benchmark]
    public void TextFormat_vs_JsonFormat()
    {
        var textLogger = InsightLogger.Create(o =>
        {
            o.OutputFormat = OutputFormat.Text;
            o.Sinks.Add(_noopSink);
        });
        
        var jsonLogger = InsightLogger.Create(o =>
        {
            o.OutputFormat = OutputFormat.Json;
            o.Sinks.Add(_noopSink);
        });
        
        textLogger.Info("Message with {Data}", "value");
        jsonLogger.Info("Message with {Data}", "value");
    }
    
    private class NoopSink : ILogSink
    {
        public Task WriteAsync(LogEvent logEvent, CancellationToken cancellationToken = default)
        {
            // Do nothing - just for benchmarking
            return Task.CompletedTask;
        }
        
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
        
        public void Dispose()
        {
        }
    }
}
