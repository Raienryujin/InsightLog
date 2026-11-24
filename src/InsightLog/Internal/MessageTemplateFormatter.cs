using System.Globalization;
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
        object?[]? args,
        List<RedactionRule>? redactionRules,
        int maxMessageLength)
    {
        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(messageTemplate))
        {
            return (string.Empty, properties);
        }

        args ??= Array.Empty<object?>();
        redactionRules ??= new List<RedactionRule>();

        var result = new StringBuilder(messageTemplate.Length);
        var templateSpan = messageTemplate.AsSpan();
        var argIndex = 0;
        var position = 0;
        
        while (position < templateSpan.Length)
        {
            var nextOpen = templateSpan[position..].IndexOf('{');
            var nextClose = templateSpan[position..].IndexOf('}');

            if (nextOpen == -1 && nextClose == -1)
            {
                result.Append(templateSpan[position..]);
                break;
            }

            // If '}' comes before '{'
            if (nextClose != -1 && (nextOpen == -1 || nextClose < nextOpen))
            {
                result.Append(templateSpan[position..(position + nextClose)]);
                position += nextClose;

                if (position + 1 < templateSpan.Length && templateSpan[position + 1] == '}')
                {
                    // Escaped '}}'
                    result.Append('}');
                    position += 2;
                }
                else
                {
                    // Unescaped literal '}'
                    result.Append('}');
                    position++;
                }
                continue;
            }

            // 'nextOpen' comes first
            result.Append(templateSpan[position..(position + nextOpen)]);
            position += nextOpen;

            if (position + 1 < templateSpan.Length && templateSpan[position + 1] == '{')
            {
                // Escaped '{{'
                result.Append('{');
                position += 2;
                continue;
            }

            // Start of a placeholder
            var placeholderClose = templateSpan[position..].IndexOf('}');
            if (placeholderClose == -1)
            {
                // Unclosed brace
                result.Append(templateSpan[position..]);
                break;
            }

            var placeholderContent = templateSpan[(position + 1)..(position + placeholderClose)];
            position += placeholderClose + 1;

            // Check for optional format specifier, e.g. {ElapsedMs:F2}
            string propertyName;
            string? format = null;
            var colonIndex = placeholderContent.IndexOf(':');
            if (colonIndex != -1)
            {
                propertyName = placeholderContent[..colonIndex].ToString();
                format = placeholderContent[(colonIndex + 1)..].ToString();
            }
            else
            {
                propertyName = placeholderContent.ToString();
            }

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
                    var formattedValue = FormatValue(value, format);
                    result.Append(formattedValue);
                    properties[propertyName] = value;
                }
            }
            else
            {
                result.Append('{').Append(placeholderContent).Append('}');
            }
        }
        
        var message = result.ToString();
        if (maxMessageLength > 0 && message.Length > maxMessageLength)
        {
            message = maxMessageLength >= 3
                ? string.Concat(message.AsSpan(0, maxMessageLength - 3), "...")
                : message[..maxMessageLength];
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
    
    private static string FormatValue(object? value, string? format = null)
    {
        return value switch
        {
            null => "null",
            string s => s,
            IFormattable f => f.ToString(format, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "null"
        };
    }
}
