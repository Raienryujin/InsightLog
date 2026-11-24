using InsightLog;
using InsightLog.Configuration;
using ConsoleSink = InsightLog.Console.ConsoleSink;
using InsightLog.File;
using InsightLog.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InsightLog.Demo;

public class Program
{
    public static async Task Main(string[] args)
    {
        System.Console.WriteLine("=== InsightLog Demo Application ===\n");
        
        // Example 1: Basic usage with manual configuration
        await RunBasicDemo();
        
        // Example 2: Dependency injection with host builder
        await RunDependencyInjectionDemo();
        
        // Example 3: Advanced features
        await RunAdvancedFeaturesDemo();
        
        System.Console.WriteLine("\n=== Demo Complete ===");
        System.Console.WriteLine("Check the 'logs' directory for file outputs.");
    }
    
    private static async Task RunBasicDemo()
    {
        System.Console.WriteLine("1. Basic Usage Demo\n");
        
        var logger = InsightLogger.Create(options =>
        {
            options.MinimumLevel = LogLevel.Trace;
            options.UseColors = true;
            options.OutputFormat = OutputFormat.Text;
            options.Sinks.Add(new ConsoleSink(options));
            options.Sinks.Add(new FileSink("logs/basic-.log", options));
        });
        
        logger.Trace("This is a trace message with {Details}", "some details");
        logger.Debug("Debug message with {Count} items", 42);
        logger.Info("Application started successfully");
        logger.Warn("Warning: {Resource} usage at {Percentage}%", "Memory", 85);
        
        try
        {
            throw new InvalidOperationException("Something went wrong!");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "An error occurred while processing {Operation}", "DataSync");
        }
        
        logger.Fatal("Critical system failure in {Component}", "Database");
        
        await Task.Delay(100); // Let async writes complete
        logger.Dispose();
    }
    
    private static async Task RunDependencyInjectionDemo()
    {
        System.Console.WriteLine("\n2. Dependency Injection Demo\n");
        
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddInsightLog(options =>
                {
                    options.MinimumLevel = LogLevel.Info;
                    options.SlowThresholdMs = 500;
                    options.Redact("password", "apiKey", "ssn", @"\b\d{3}-\d{2}-\d{4}\b");
                    options.Sinks.Add(new ConsoleSink(options));
                    options.Sinks.Add(new JsonSink("logs/di-.json"));
                });
                
                services.AddHostedService<SampleService>();
            })
            .Build();
        
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await host.RunAsync(cts.Token);
    }
    
    private static async Task RunAdvancedFeaturesDemo()
    {
        System.Console.WriteLine("\n3. Advanced Features Demo\n");
        
        var logger = InsightLogger.Create(options =>
        {
            options.MinimumLevel = LogLevel.Debug;
            options.SlowThresholdMs = 200;
            options.MaxMessageLength = 100;
            options.SampleRate = 1; // Log everything
            options.Redact("password", "creditCard", "socialSecurity");
            options.IncludeScopes = true;
            options.IncludeCallerInfo = true;
            options.UseColors = true;
            
            // Multiple sinks with different formats
            options.Sinks.Add(new ConsoleSink(options));
            options.Sinks.Add(new FileSink("logs/text-.log", options));
            options.Sinks.Add(new JsonSink("logs/json-.json"));
        });
        
        // Scoped logging
        using (logger.Scope("UserAuthentication"))
        {
            logger.Info("Checking user credentials");
            logger.Debug("User {Username} with password {Password}", "john.doe", "secret123");
            
            using (logger.Scope("DatabaseQuery"))
            {
                logger.Debug("Executing query: SELECT * FROM users WHERE username = {Username}", "john.doe");
                await SimulateSlowOperation(300);
                logger.Info("Query completed, found {Count} results", 1);
            }
            
            logger.Info("Authentication successful");
        }
        
        // Measuring operations
        using (logger.Measure("DataProcessing"))
        {
            logger.Info("Processing batch {BatchId}", "BATCH-001");
            await SimulateSlowOperation(250);
            logger.Info("Batch processing completed");
        }
        
        // Correlation tracking
        using (var activity = new System.Diagnostics.Activity("OrderProcessing").Start())
        {
            logger.Info("Processing order {OrderId}", "ORD-12345");
            logger.Debug("Order contains {ItemCount} items", 3);
            
            // Simulate fast operations (should not trigger slow warning)
            using (logger.Measure("FastOperation"))
            {
                await Task.Delay(50);
            }
            
            // Long message truncation
            var longMessage = string.Concat(Enumerable.Repeat("This is a very long message. ", 20));
            logger.Info("Long message test: {Message}", longMessage);
        }
        
        // Structured properties with redaction
        logger.Info("User profile updated: {UserId}, {Email}, {CreditCard}, {SocialSecurity}",
            "USER-789",
            "user@example.com",
            "4111-1111-1111-1111",
            "123-45-6789");
        
        // Sampling demonstration (if sample rate was > 1)
        logger.Debug("Sample rate is 1, so this message will always appear");
        
        await Task.Delay(100);
        logger.Dispose();
    }
    
    private static async Task SimulateSlowOperation(int delayMs)
    {
        await Task.Delay(delayMs);
    }
}

// Sample hosted service for DI demo
public class SampleService : BackgroundService
{
    private readonly ILogger _logger;
    
    public SampleService(ILogger logger)
    {
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Info("SampleService started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            using (_logger.Scope("ServiceIteration"))
            {
                _logger.Info("Processing service iteration at {Time}", DateTime.Now);
                
                using (_logger.Measure("ServiceWork"))
                {
                    await Task.Delay(500, stoppingToken);
                    _logger.Debug("Service work completed");
                }
            }
            
            await Task.Delay(1000, stoppingToken);
        }
        
        _logger.Info("SampleService stopped");
    }
}
