using InsightLog.Configuration;
using InsightLog.File;

namespace InsightLog.Json;

/// <summary>
/// A specialized file sink that writes newline-delimited JSON for structured logging systems.
/// </summary>
public sealed class JsonSink : FileSink
{
    /// <summary>
    /// Initializes a new instance of the JsonSink class.
    /// </summary>
    /// <param name="pathTemplate">The file path template (e.g., "logs/app-.json").</param>
    /// <param name="maxFileSizeMB">Maximum file size in megabytes before rolling.</param>
    public JsonSink(string pathTemplate, int maxFileSizeMB = 100)
        : base(pathTemplate, new LogOptions { OutputFormat = OutputFormat.Json }, maxFileSizeMB)
    {
    }
}
