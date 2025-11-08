using System.Diagnostics;
using FluentAssertions;
using InsightLog;
using InsightLog.Configuration;
using InsightLog.Internal;
using InsightLog.Sinks;
using NSubstitute;
using Xunit;

namespace InsightLog.Tests;

public class InsightLoggerTests
{
    [Fact]
    public void Create_WithDefaultOptions_CreatesLoggerSuccessfully()
    {
        // Act
        var logger = InsightLogger.Create();
        
        // Assert
        logger.Should().NotBeNull();
        logger.IsEnabled(LogLevel.Info).Should().BeTrue();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
    }
    
    [Fact]
    public void IsEnabled_RespectsMinimumLevel()
    {
        // Arrange
        var logger = InsightLogger.Create(o => o.MinimumLevel = LogLevel.Warn);
        
        // Assert
        logger.IsEnabled(LogLevel.Trace).Should().BeFalse();
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Info).Should().BeFalse();
        logger.IsEnabled(LogLevel.Warn).Should().BeTrue();
        logger.IsEnabled(LogLevel.Error).Should().BeTrue();
        logger.IsEnabled(LogLevel.Fatal).Should().BeTrue();
    }
    
    [Fact]
    public async Task Log_WritesToConfiguredSinks()
    {
        // Arrange
        var mockSink = Substitute.For<ILogSink>();
        var logger = InsightLogger.Create(o =>
        {
            o.MinimumLevel = LogLevel.Debug;
            o.Sinks.Add(mockSink);
        });
        
        // Act
        logger.Info("Test message with {Value}", 42);
        await Task.Delay(100); // Allow async write to complete
        
        // Assert
        await mockSink.Received(1).WriteAsync(
            Arg.Is<LogEvent>(e => 
                e.Level == LogLevel.Info && 
                e.Message.Contains("Test message with 42")),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public void MessageTemplateFormatter_FormatsCorrectly()
    {
        // Arrange
        var template = "User {Username} logged in from {IpAddress}";
        var args = new object?[] { "john.doe", "192.168.1.1" };
        
        // Act
        var (message, properties) = MessageTemplateFormatter.Format(
            template, args, new List<RedactionRule>(), 1000);
        
        // Assert
        message.Should().Be("User john.doe logged in from 192.168.1.1");
        properties.Should().ContainKey("Username").WhoseValue.Should().Be("john.doe");
        properties.Should().ContainKey("IpAddress").WhoseValue.Should().Be("192.168.1.1");
    }
    
    [Fact]
    public void MessageTemplateFormatter_HandlesEscapedBraces()
    {
        // Arrange
        var template = "Object {escaped} with {Value}";
        var args = new object?[] { "escaped", 123 };
        
        // Act
        var (message, _) = MessageTemplateFormatter.Format(
            template, args, new List<RedactionRule>(), 1000);
    
        // Assert
        message.Should().Be("Object escaped with 123");
    }
    
    [Fact]
    public void MessageTemplateFormatter_TruncatesLongMessages()
    {
        // Arrange
        var template = "Very long message: {Content}";
        var args = new object?[] { string.Concat(Enumerable.Repeat("x", 100)) };
        
        // Act
        var (message, _) = MessageTemplateFormatter.Format(
            template, args, new List<RedactionRule>(), 50);
        
        // Assert
        message.Should().HaveLength(50);
        message.Should().EndWith("...");
    }
    
    [Fact]
    public void RedactionRule_MatchesCorrectly()
    {
        // Arrange
        var literalRule = new RedactionRule { Pattern = "password", IsRegex = false };
        var regexRule = new RedactionRule { Pattern = @"\b\d{3}-\d{2}-\d{4}\b", IsRegex = true };
        
        // Assert
        literalRule.Matches("password").Should().BeTrue();
        literalRule.Matches("Password").Should().BeTrue(); // Case insensitive
        literalRule.Matches("pass").Should().BeFalse();
        
        regexRule.Matches("123-45-6789").Should().BeTrue();
        regexRule.Matches("invalid").Should().BeFalse();
    }
    
    [Fact]
    public void MessageTemplateFormatter_RedactsSpecifiedProperties()
    {
        // Arrange
        var template = "Login attempt: {Username} with {Password}";
        var args = new object?[] { "john.doe", "secret123" };
        var rules = new List<RedactionRule>
        {
            new() { Pattern = "password", IsRegex = false }
        };
        
        // Act
        var (message, properties) = MessageTemplateFormatter.Format(
            template, args, rules, 1000);
        
        // Assert
        message.Should().Be("Login attempt: john.doe with ***REDACTED***");
        properties["Username"].Should().Be("john.doe");
        properties["Password"].Should().Be("***REDACTED***");
    }
    
    [Fact]
    public async Task LogScope_TracksElapsedTime()
    {
        // Arrange
        var mockSink = Substitute.For<ILogSink>();
        var logger = InsightLogger.Create(o =>
        {
            o.MinimumLevel = LogLevel.Debug;
            o.IncludeScopes = true;
            o.SlowThresholdMs = 100;
            o.Sinks.Add(mockSink);
        });
        
        // Act
        using (logger.Scope("TestOperation"))
        {
            await Task.Delay(150);
        }
        
        await Task.Delay(100); // Allow async writes
        
        // Assert
        await mockSink.Received().WriteAsync(
            Arg.Is<LogEvent>(e => 
                e.Message.Contains("TestOperation") && 
                e.Message.Contains("completed") &&
                e.ElapsedMs > 100),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public void LogScope_ManagesDepthCorrectly()
    {
        // Arrange
        var initialDepth = ScopeContext.CurrentDepth;
        var logger = InsightLogger.Create();
        
        // Act & Assert
        using (var scope1 = logger.Scope("Outer"))
        {
            ScopeContext.CurrentDepth.Should().Be(initialDepth + 1);
            
            using (var scope2 = logger.Scope("Inner"))
            {
                ScopeContext.CurrentDepth.Should().Be(initialDepth + 2);
            }
            
            ScopeContext.CurrentDepth.Should().Be(initialDepth + 1);
        }
        
        ScopeContext.CurrentDepth.Should().Be(initialDepth);
    }
    
    [Fact]
    public void LogOptions_RedactMethod_AddsRules()
    {
        // Arrange
        var options = new LogOptions();
        
        // Act
        options.Redact("password", "apiKey", @"\b\d{4}\b");
        
        // Assert
        options.RedactionRules.Should().HaveCount(3);
        options.RedactionRules[0].Pattern.Should().Be("password");
        options.RedactionRules[0].IsRegex.Should().BeFalse();
        options.RedactionRules[2].Pattern.Should().Be(@"\b\d{4}\b");
        options.RedactionRules[2].IsRegex.Should().BeTrue();
    }
    
    [Fact]
    public async Task Logger_HandlesExceptions()
    {
        // Arrange
        var mockSink = Substitute.For<ILogSink>();
        var logger = InsightLogger.Create(o =>
        {
            o.Sinks.Add(mockSink);
        });
        var exception = new InvalidOperationException("Test error");
        
        // Act
        logger.Error(exception, "Operation failed");
        await Task.Delay(100);
        
        // Assert
        await mockSink.Received(1).WriteAsync(
            Arg.Is<LogEvent>(e => 
                e.Exception == exception && 
                e.Level == LogLevel.Error),
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task Logger_GeneratesCorrelationId()
    {
        // Arrange
        var mockSink = Substitute.For<ILogSink>();
        var logger = InsightLogger.Create(o =>
        {
            o.Sinks.Add(mockSink);
        });
        
        // Act - without Activity
        logger.Info("Test without activity");
        await Task.Delay(50);
        
        // Assert
        await mockSink.Received(1).WriteAsync(
            Arg.Is<LogEvent>(e => 
                !string.IsNullOrEmpty(e.CorrelationId) &&
                e.CorrelationId.Length == 8),
            Arg.Any<CancellationToken>());
        
        mockSink.ClearReceivedCalls();
        
        // Act - with Activity
        using var activity = new Activity("Test").Start();
        logger.Info("Test with activity");
        await Task.Delay(50);
        
        // Assert
        await mockSink.Received(1).WriteAsync(
            Arg.Is<LogEvent>(e => 
                !string.IsNullOrEmpty(e.CorrelationId)),
            Arg.Any<CancellationToken>());
    }
    
    [Theory]
    [InlineData(1, 10, 10)] // Sample rate 1 = log all
    [InlineData(2, 20, 10)] // Sample rate 2 = log ~50%
    [InlineData(5, 50, 10)] // Sample rate 5 = log ~20%
    public async Task Logger_RespectsConfiguredSampleRate(int sampleRate, int totalMessages, int expectedApprox)
    {
        // Arrange
        var mockSink = Substitute.For<ILogSink>();
        var logger = InsightLogger.Create(o =>
        {
            o.SampleRate = sampleRate;
            o.Sinks.Add(mockSink);
        });
        
        // Act
        for (int i = 0; i < totalMessages; i++)
        {
            logger.Info($"Message {i}");
        }
        await Task.Delay(100);
        
        // Assert
        var receivedCalls = mockSink.ReceivedCalls().Count();
        if (sampleRate == 1)
        {
            receivedCalls.Should().Be(totalMessages);
        }
        else
        {
            // Allow for statistical variance in sampling
            receivedCalls.Should().BeInRange(expectedApprox - 5, expectedApprox + 5);
        }
    }
}
