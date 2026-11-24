using FluentAssertions;
using InsightLog.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InsightLog.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInsightLog_RegistersLoggerAndOptionsAsSingletons()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInsightLog(options =>
        {
            options.MinimumLevel = LogLevel.Warn;
            options.SlowThresholdMs = 250;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<LogOptions>();
        options.Should().NotBeNull();
        options!.MinimumLevel.Should().Be(LogLevel.Warn);
        options.SlowThresholdMs.Should().Be(250);

        var logger = provider.GetService<ILogger>();
        logger.Should().NotBeNull();
        logger!.IsEnabled(LogLevel.Info).Should().BeFalse();
        logger.IsEnabled(LogLevel.Warn).Should().BeTrue();

        var concreteLogger = provider.GetService<InsightLogger>();
        concreteLogger.Should().NotBeNull();
        concreteLogger.Should().BeSameAs(logger);
    }
}
