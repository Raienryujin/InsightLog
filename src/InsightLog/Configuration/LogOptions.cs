using System.Text.RegularExpressions;
using InsightLog.Sinks;

namespace InsightLog.Configuration;

/// <summary>
/// Configuration options for the InsightLog logger.
/// </summary>
public sealed class LogOptions
{
    /// <summary>
    /// Gets or sets the minimum log level to output.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
    
    /// <summary>
    /// Gets or sets whether to use colored output (for console sinks).
    /// </summary>
    public bool UseColors { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the output format for messages.
    /// </summary>
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Text;
    
    /// <summary>
    /// Gets or sets the threshold in milliseconds for marking operations as slow.
    /// </summary>
    public int SlowThresholdMs { get; set; } = 1000;
    
    /// <summary>
    /// Gets or sets the sampling rate (1 = log all, 2 = log 50%, etc.).
    /// </summary>
    public int SampleRate { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the maximum message length before truncation.
    /// </summary>
    public int MaxMessageLength { get; set; } = 4000;
    
    /// <summary>
    /// Gets the list of redaction rules (property names or regex patterns).
    /// </summary>
    public List<RedactionRule> RedactionRules { get; } = new();
    
    /// <summary>
    /// Gets or sets whether to include scope information in logs.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to include caller information (file, member, line).
    /// </summary>
    public bool IncludeCallerInfo { get; set; } = true;
    
    /// <summary>
    /// Gets the list of configured sinks.
    /// </summary>
    public List<ILogSink> Sinks { get; } = new();
    
    /// <summary>
    /// Adds a redaction rule for sensitive property names.
    /// </summary>
    /// <param name="patterns">Property name patterns to redact.</param>
    public void Redact(params string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (pattern.Contains('\\') || pattern.Contains('[') || pattern.Contains('^'))
            {
                // Treat as regex
                RedactionRules.Add(new RedactionRule { Pattern = pattern, IsRegex = true });
            }
            else
            {
                // Treat as literal string
                RedactionRules.Add(new RedactionRule { Pattern = pattern, IsRegex = false });
            }
        }
    }
}

/// <summary>
/// Represents a redaction rule for sensitive data.
/// </summary>
public sealed class RedactionRule
{
    /// <summary>
    /// Gets or sets the pattern to match.
    /// </summary>
    public string Pattern { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether the pattern is a regex.
    /// </summary>
    public bool IsRegex { get; init; }
    
    private Regex? _compiledRegex;
    
    /// <summary>
    /// Checks if the given property name matches this rule.
    /// </summary>
    /// <param name="propertyName">The property name to check.</param>
    /// <returns>True if the property should be redacted.</returns>
    public bool Matches(string propertyName)
    {
        if (IsRegex)
        {
            _compiledRegex ??= new Regex(Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return _compiledRegex.IsMatch(propertyName);
        }
        
        return string.Equals(propertyName, Pattern, StringComparison.OrdinalIgnoreCase);
    }
}
