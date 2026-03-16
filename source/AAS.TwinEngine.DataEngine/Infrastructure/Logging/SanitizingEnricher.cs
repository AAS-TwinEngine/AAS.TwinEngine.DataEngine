using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using Serilog.Core;
using Serilog.Events;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Logging;

/// <summary>
/// A Serilog enricher that automatically sanitizes all string property values
/// in log events to prevent log poisoning attacks.
/// This removes the need to manually call <see cref="LogSanitizer.Sanitize"/> at every log call site.
/// </summary>
public class SanitizingEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        var propertiesToUpdate = new List<LogEventProperty>();

        foreach (var property in logEvent.Properties)
        {
            var sanitized = SanitizeValue(property.Value);
            if (!ReferenceEquals(sanitized, property.Value))
            {
                propertiesToUpdate.Add(new LogEventProperty(property.Key, sanitized));
            }
        }

        foreach (var property in propertiesToUpdate)
        {
            logEvent.AddOrUpdateProperty(property);
        }
    }

    private static LogEventPropertyValue SanitizeValue(LogEventPropertyValue value)
    {
        switch (value)
        {
            case ScalarValue { Value: string s }:
                {
                    var sanitized = LogSanitizer.Sanitize(s);
                    return sanitized == s ? value : new ScalarValue(sanitized);
                }
            case SequenceValue seq:
                {
                    var elements = seq.Elements;
                    var sanitizedElements = new List<LogEventPropertyValue>(elements.Count);
                    var anyChanged = false;

                    foreach (var element in elements)
                    {
                        var sanitizedElement = SanitizeValue(element);
                        if (!ReferenceEquals(element, sanitizedElement))
                        {
                            anyChanged = true;
                        }

                        sanitizedElements.Add(sanitizedElement);
                    }

                    return anyChanged ? new SequenceValue(sanitizedElements) : value;
                }
            case StructureValue str:
                {
                    var properties = str.Properties;
                    var sanitizedProperties = new List<LogEventProperty>(properties.Count);
                    var anyChanged = false;

                    foreach (var prop in properties)
                    {
                        var sanitizedValue = SanitizeValue(prop.Value);
                        if (ReferenceEquals(prop.Value, sanitizedValue))
                        {
                            sanitizedProperties.Add(prop);
                        }
                        else
                        {
                            anyChanged = true;
                            sanitizedProperties.Add(new LogEventProperty(prop.Name, sanitizedValue));
                        }
                    }

                    return anyChanged ? new StructureValue(sanitizedProperties, str.TypeTag) : value;
                }
            case DictionaryValue dict:
                {
                    var elements = dict.Elements;
                    var sanitizedElements = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>(elements.Count);
                    var anyChanged = false;

                    foreach (var kvp in elements)
                    {
                        var sanitizedKey = SanitizeScalar(kvp.Key);
                        var sanitizedValue = SanitizeValue(kvp.Value);

                        if (!ReferenceEquals(kvp.Key, sanitizedKey) || !ReferenceEquals(kvp.Value, sanitizedValue))
                        {
                            anyChanged = true;
                        }

                        sanitizedElements.Add(new KeyValuePair<ScalarValue, LogEventPropertyValue>(sanitizedKey, sanitizedValue));
                    }

                    return anyChanged ? new DictionaryValue(sanitizedElements) : value;
                }
            default:
                return value;
        }
    }

    private static ScalarValue SanitizeScalar(ScalarValue scalar)
    {
        if (scalar.Value is string s)
        {
            var sanitized = LogSanitizer.Sanitize(s);
            return sanitized == s ? scalar : new ScalarValue(sanitized);
        }

        return scalar;
    }
}
