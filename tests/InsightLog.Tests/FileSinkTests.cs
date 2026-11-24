using System.Text.Json;
using FluentAssertions;
using InsightLog.Configuration;
using InsightLog.File;
using InsightLog.Json;
using Xunit;

namespace InsightLog.Tests;

public class FileSinkTests : IDisposable
{
    private readonly string _testDirectory;

    public FileSinkTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "InsightLogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task FileSink_WritesTextLogEventToFile()
    {
        // Arrange
        var logPath = Path.Combine(_testDirectory, "test.log");
        var options = new LogOptions { OutputFormat = OutputFormat.Text };
        using var sink = new FileSink(logPath, options);

        var logEvent = new LogEvent
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Info,
            Message = "Test message",
            CorrelationId = "12345678",
            Properties = new Dictionary<string, object?> { ["User"] = "Alice" }
        };

        // Act
        await sink.WriteAsync(logEvent);
        await sink.FlushAsync();
        sink.Dispose();

        // Assert
        var files = Directory.GetFiles(_testDirectory);
        files.Should().NotBeEmpty();

        var content = await System.IO.File.ReadAllTextAsync(files[0]);
        content.Should().Contain("[INFO]");
        content.Should().Contain("Test message");
        content.Should().Contain("corr:12345678");
        content.Should().Contain("User=Alice");
    }

    [Fact]
    public async Task FileSink_WritesValidJsonWhenConfigured()
    {
        // Arrange
        var logPath = Path.Combine(_testDirectory, "test.json");
        var options = new LogOptions { OutputFormat = OutputFormat.Json };
        using var sink = new FileSink(logPath, options);

        var logEvent = new LogEvent
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Warn,
            Message = "Warning occurred",
            CorrelationId = "abcdef12",
            ThreadId = 1,
            Properties = new Dictionary<string, object?>
            {
                ["Count"] = 42,
                ["Timestamp"] = DateTime.UtcNow,
                ["Active"] = true,
                ["NullVal"] = null,
                ["GuidVal"] = Guid.NewGuid()
            }
        };

        // Act
        await sink.WriteAsync(logEvent);
        await sink.FlushAsync();
        sink.Dispose();

        // Assert
        var files = Directory.GetFiles(_testDirectory);
        files.Should().NotBeEmpty();

        var content = await System.IO.File.ReadAllTextAsync(files[0]);
        var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1);

        // Verify valid JSON parse
        var doc = JsonDocument.Parse(lines[0]);
        doc.RootElement.GetProperty("level").GetString().Should().Be("Warn");
        doc.RootElement.GetProperty("message").GetString().Should().Be("Warning occurred");
        doc.RootElement.GetProperty("properties").GetProperty("Count").GetInt32().Should().Be(42);
        doc.RootElement.GetProperty("properties").GetProperty("Active").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task JsonSink_InheritsAndOutputsValidJsonLines()
    {
        // Arrange
        var logPath = Path.Combine(_testDirectory, "app.json");
        using var sink = new JsonSink(logPath);

        var logEvent = new LogEvent
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Error,
            Message = "Database error",
            CorrelationId = "corr-1234",
            ThreadId = 5,
            ElapsedMs = 125.5,
            IsSlow = true,
            Properties = new Dictionary<string, object?> { ["Retries"] = 3 }
        };

        // Act
        await sink.WriteAsync(logEvent);
        await sink.FlushAsync();
        sink.Dispose();

        // Assert
        var files = Directory.GetFiles(_testDirectory);
        files.Should().NotBeEmpty();

        var content = await System.IO.File.ReadAllTextAsync(files[0]);
        var doc = JsonDocument.Parse(content.Trim());
        doc.RootElement.GetProperty("level").GetString().Should().Be("Error");
        doc.RootElement.GetProperty("isSlow").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("elapsedMs").GetDouble().Should().Be(125.5);
    }
}
