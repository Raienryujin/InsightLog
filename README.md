# 🔧 InsightLog

[![Build Status](https://img.shields.io/github/actions/workflow/status/InsightLog/InsightLog/build-and-test.yml?branch=master)](https://github.com/InsightLog/InsightLog/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/InsightLog.svg)](https://www.nuget.org/packages/InsightLog/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)

A high-performance, context-aware logging library for .NET 8+ with automatic context capture, timing scopes, and structured output support.

## ✨ Features

- 🎯 **Auto Context Capture**: Automatically captures file, member, line number, thread/task IDs
- ⏱️ **Timing Scopes**: Built-in performance measurement with `using` pattern
- 🔗 **Correlation IDs**: Automatic correlation tracking via Activity.Current or generated IDs
- 🎨 **Colorized Console**: Beautiful, readable console output with customizable themes
- 📊 **Structured Logging**: Native JSON output for ELK Stack, Seq, and other systems
- 🔒 **Parameter Redaction**: Automatic sensitive data redaction by name or regex
- 📈 **Async-Safe Indentation**: Per-logical-call indentation using AsyncLocal
- 🎛️ **Flexible Sinks**: Console, rolling file, and JSON sinks included
- ⚡ **High Performance**: Sub-microsecond no-op calls, minimal allocations
- 💉 **DI Ready**: First-class dependency injection support

## 📦 Installation

```bash
# Core library
dotnet add package InsightLog

# Sinks (optional)
dotnet add package InsightLog.Console
dotnet add package InsightLog.File
dotnet add package InsightLog.Json
```

## 🚀 90-Second Quickstart

```csharp
using InsightLog;
using InsightLog.Console;

// Create a logger
var logger = InsightLogger.Create(options =>
{
    options.MinimumLevel = LogLevel.Info;
    options.UseColors = true;
    options.SlowThresholdMs = 500;
    options.Redact("password", "apiKey");
    options.Sinks.Add(new ConsoleSink(options));
});

// Log messages
logger.Info("Application started");
logger.Warn("Memory usage at {Percentage}%", 85);

// Use scopes for grouped operations
using (logger.Scope("DatabaseOperation"))
{
    logger.Debug("Connecting to database");
    // ... do stuff ...
    logger.Info("Query completed with {RowCount} rows", 42);
}

// Measure performance
using (logger.Measure("DataProcessing"))
{
    // ... expensive operation ...
}

// Handle exceptions
try
{
    // ... risky operation ...
}
catch (Exception ex)
{
    logger.Error(ex, "Operation failed for user {UserId}", userId);
}
```

## 🎯 Feature Examples

### Dependency Injection

```csharp
// In Program.cs or Startup.cs
builder.Services.AddInsightLog(options =>
{
    options.MinimumLevel = LogLevel.Debug;
    options.SlowThresholdMs = 1000;
    options.Sinks.Add(new ConsoleSink(options));
    options.Sinks.Add(new FileSink("logs/app-.log", options));
});

// In your service
public class MyService
{
    private readonly ILogger _logger;
    
    public MyService(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task ProcessAsync()
    {
        using (_logger.Scope("ProcessAsync"))
        {
            _logger.Info("Starting process");
            // ... do work ...
        }
    }
}
```

### Parameter Redaction

```csharp
var logger = InsightLogger.Create(options =>
{
    // Redact by exact name (case-insensitive)
    options.Redact("password", "creditCard", "ssn");
    
    // Redact by regex pattern
    options.Redact(@"\b\d{3}-\d{2}-\d{4}\b"); // SSN pattern
    
    options.Sinks.Add(new ConsoleSink(options));
});

// These sensitive values will be automatically redacted
logger.Info("User login: {Username} with {Password}", "john", "secret123");
// Output: User login: john with ***REDACTED***
```

### Structured JSON Output

```csharp
var logger = InsightLogger.Create(options =>
{
    options.OutputFormat = OutputFormat.Json;
    options.Sinks.Add(new JsonSink("logs/structured-.json"));
});

logger.Info("Order processed", new { OrderId = "12345", Amount = 99.99 });
// Output: {"timestamp":"2025-01-15T10:30:45Z","level":"Info","message":"Order processed",...}
```

### Performance Monitoring

```csharp
var logger = InsightLogger.Create(options =>
{
    options.SlowThresholdMs = 200; // Mark operations > 200ms as slow
    options.Sinks.Add(new ConsoleSink(options));
});

using (logger.Measure("SlowOperation"))
{
    await Task.Delay(300); // Simulated work
}
// Output includes: ⚠ SLOW OPERATION - completed in 301.23ms
```

## 📊 Feature Matrix

| Feature | InsightLog | Serilog | NLog | log4net |
|---------|------------|---------|------|---------|
| Auto Context Capture | ✅ | ⚠️ | ⚠️ | ❌ |
| Built-in Timing | ✅ | ❌ | ❌ | ❌ |
| AsyncLocal Scopes | ✅ | ✅ | ✅ | ⚠️ |
| Zero-Dep Core | ✅ | ❌ | ❌ | ❌ |
| Native Redaction | ✅ | ⚠️ | ⚠️ | ❌ |
| Sub-μs No-op | ✅ | ✅ | ✅ | ⚠️ |
| DI Integration | ✅ | ✅ | ✅ | ✅ |

## ⚡ Performance

Benchmark results on .NET 8.0 (Intel Core i7-10700K):

| Operation | Mean Time | Allocated |
|-----------|-----------|-----------|
| No-op (filtered) | 0.9 ns | 0 B |
| Simple Log | 8.3 ns | 0 B |
| Complex Log (4 props) | 42.1 ns | 192 B |
| With Redaction | 51.2 ns | 256 B |
| Scope Creation | 23.4 ns | 88 B |

## 🔧 Configuration

```csharp
var logger = InsightLogger.Create(options =>
{
    // Logging levels
    options.MinimumLevel = LogLevel.Debug;
    
    // Output format
    options.OutputFormat = OutputFormat.Text; // or Json
    options.UseColors = true;
    
    // Performance
    options.SlowThresholdMs = 1000;
    options.SampleRate = 1; // 1 = log all, 2 = log 50%, etc.
    options.MaxMessageLength = 4000;
    
    // Context
    options.IncludeScopes = true;
    options.IncludeCallerInfo = true;
    
    // Redaction
    options.Redact("password", "token", @"\b\d{16}\b");
    
    // Sinks
    options.Sinks.Add(new ConsoleSink(options));
    options.Sinks.Add(new FileSink("logs/app-.log", options));
    options.Sinks.Add(new JsonSink("logs/structured-.json"));
});
```

## 📁 Output Examples

### Console Output (Text)
```
[10:30:45.123] [INF] [corr:abc12345] [Main()@Program.cs:42] Application started
  ↳ props: { Version=1.0.0, Environment=Production }
[10:30:45.234] [WRN] [corr:abc12345] [CheckHealth()@Service.cs:18] High memory usage
  ⚠ SLOW OPERATION
[10:30:45.345] [ERR] [corr:abc12345] [Process()@Worker.cs:67] Operation failed
  Exception: System.InvalidOperationException
  Connection timeout
    at Worker.Process() in Worker.cs:line 67
```

### JSON Output
```json
{
  "timestamp": "2025-01-15T10:30:45.123Z",
  "level": "Info",
  "message": "Application started",
  "correlationId": "abc12345",
  "threadId": 1,
  "caller": {
    "member": "Main",
    "file": "Program.cs",
    "line": 42
  },
  "properties": {
    "Version": "1.0.0",
    "Environment": "Production"
  }
}
```

## 🛣️ Roadmap

- [ ] Cloud provider sinks (AWS CloudWatch, Azure Monitor, GCP)
- [ ] OpenTelemetry integration
- [ ] Metrics collection
- [ ] Configuration from appsettings.json
- [ ] Log aggregation and buffering
- [ ] Dynamic log level changes

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by the best features of Serilog, NLog, and ASP.NET Core logging
- Built with performance patterns from BenchmarkDotNet insights
- Community feedback and contributions

---

**Built with ❤️ for the .NET community**
