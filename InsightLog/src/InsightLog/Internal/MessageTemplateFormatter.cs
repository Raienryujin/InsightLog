using System.Text;
using InsightLog.Configuration;

namespace InsightLog.Internal;

/// <summary>
/// Provides lightweight message template formatting without external dependencies.
/// </summary>
public static class MessageTemplateFormatter
{
    private const string RedactedValue = "***REDACTED***";
    
    /// <summary>
    /// Formats a message template with the provided arguments.
    /// </summary>
    public static (string message, Dictionary<string, object?> properties) Format(
        string messageTemplate,
        object?[] args,
        List<RedactionRule> redactionRules,
        int maxMessageLength)
    {
        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var result = new StringBuilder();
        var templateSpan = messageTemplate.AsSpan();
        var argIndex = 0;
        var position = 0;
        
        while (position < templateSpan.Length)
        {
            var openBrace = templateSpan[position..].IndexOf('{');
            if (openBrace == -1)
            {
                result.Append(templateSpan[position..]);
                break;
            }
            
            result.Append(templateSpan[position..(position + openBrace)]);
            position += openBrace;
            
            if (position + 1 < templateSpan.Length && templateSpan[position + 1] == '{')
            {
                // Escaped brace
                result.Append('{');
                position += 2;
                continue;
            }
            
            var closeBrace = templateSpan[position..].IndexOf('}');
            if (closeBrace == -1)
            {
                result.Append(templateSpan[position..]);
                break;
            }
            
            var propertyName = templateSpan[(position + 1)..(position + closeBrace)].ToString();
            position += closeBrace + 1;
            
            if (argIndex < args.Length)
            {
                var value = args[argIndex++];
                var shouldRedact = ShouldRedact(propertyName, redactionRules);
                
                if (shouldRedact)
                {
                    result.Append(RedactedValue);
                    properties[propertyName] = RedactedValue;
                }
                else
                {
                    var formattedValue = FormatValue(value);
                    result.Append(formattedValue);
                    properties[propertyName] = value;
                }
            }
            else
            {
                result.Append('{').Append(propertyName).Append('}');
            }
        }
        
        var message = result.ToString();
        if (message.Length > maxMessageLength)
        {
            message = string.Concat(
                message.AsSpan(0, maxMessageLength - 3),
                "...");
        }
        
        return (message, properties);
    }
    
    private static bool ShouldRedact(string propertyName, List<RedactionRule> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.Matches(propertyName))
                return true;
        }
        return false;
    }
    
    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => s,
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "null"
        };
    }
}
