using System.Text.Json;
using FluentAssertions;
using InsightLog.Configuration;
using InsightLog.Console;
using Xunit;

namespace InsightLog.Tests;

public class ConsoleSinkTests
{
    [Fact]
    public async Task ConsoleSink_WriteAsync_DoesNotThrowInTextMode()
    {
        // Arrange
        var options = new LogOptions { UseColors = false, OutputFormat = OutputFormat.Text };
        using var sink = new ConsoleSink(options);
        var logEvent = new LogEvent
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Info,
            Message = "Console test message",
            CorrelationId = "testcorr",
            Properties = new Dictionary<string, object?> { ["Key"] = "Val" }
        };

        // Act & Assert
        var act = async () => await sink.WriteAsync(logEvent);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConsoleSink_WriteAsync_DoesNotThrowInJsonMode()
    {
        // Arrange
        var options = new LogOptions { OutputFormat = OutputFormat.Json };
        using var sink = new ConsoleSink(options);
        var logEvent = new LogEvent
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Error,
            Message = "Error in console json",
            CorrelationId = "testjson",
            Exception = new InvalidOperationException("Something failed"),
            Properties = new Dictionary<string, object?>
            {
                ["Count"] = 10,
                ["Timestamp"] = DateTime.UtcNow,
                ["Guid"] = Guid.NewGuid()
            }
        };

        // Act & Assert
        var act = async () => await sink.WriteAsync(logEvent);
        await act.Should().NotThrowAsync();
    }
}
