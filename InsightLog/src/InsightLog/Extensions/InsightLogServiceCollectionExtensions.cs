using InsightLog;
using InsightLog.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring InsightLog in dependency injection.
/// </summary>
public static class InsightLogServiceCollectionExtensions
{
    /// <summary>
    /// Adds InsightLog to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure the logger options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInsightLog(
        this IServiceCollection services,
        Action<LogOptions>? configure = null)
    {
        services.TryAddSingleton<LogOptions>(sp =>
        {
            var options = new LogOptions();
            configure?.Invoke(options);
            return options;
        });
        
        services.TryAddSingleton<ILogger>(sp =>
        {
            var options = sp.GetRequiredService<LogOptions>();
            return new InsightLogger(options);
        });
        
        services.TryAddSingleton<InsightLogger>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger>();
            return (InsightLogger)logger;
        });
        
        return services;
    }
}
