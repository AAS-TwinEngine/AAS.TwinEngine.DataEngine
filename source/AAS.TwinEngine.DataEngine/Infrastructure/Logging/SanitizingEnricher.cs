using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

using Serilog.Core;
using Serilog.Events;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Logging;

/// <summary>
/// A Serilog enricher that automatically sanitizes all string property values
/// in log events to prevent log poisoning attacks.
/// </summary>
public class SanitizingEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent is null)
        {
            throw new InvalidDependencyException(nameof(logEvent));
        }

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
        return value switch
        {
            ScalarValue { Value: string s } scalar => SanitizeScalarString(scalar, s),
            SequenceValue seq => SanitizeSequence(seq),
            StructureValue str => SanitizeStructure(str),
            DictionaryValue dict => SanitizeDictionary(dict),
            _ => value
        };
    }

    private static LogEventPropertyValue SanitizeScalarString(ScalarValue scalar, string value)
    {
        var sanitized = LogSanitizerExtension.Sanitize(value);
        return sanitized == value ? scalar : new ScalarValue(sanitized);
    }

    private static LogEventPropertyValue SanitizeSequence(SequenceValue sequence)
    {
        var sanitizedElements = new List<LogEventPropertyValue>(sequence.Elements.Count);
        var anyChanged = false;

        foreach (var element in sequence.Elements)
        {
            var sanitizedElement = SanitizeValue(element);

            if (!ReferenceEquals(element, sanitizedElement))
            {
                anyChanged = true;
            }

            sanitizedElements.Add(sanitizedElement);
        }

        return anyChanged ? new SequenceValue(sanitizedElements) : sequence;
    }

    private static LogEventPropertyValue SanitizeStructure(StructureValue structure)
    {
        var sanitizedProperties = new List<LogEventProperty>(structure.Properties.Count);
        var anyChanged = false;

        foreach (var prop in structure.Properties)
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

        return anyChanged ? new StructureValue(sanitizedProperties, structure.TypeTag) : structure;
    }

    private static LogEventPropertyValue SanitizeDictionary(DictionaryValue dictionary)
    {
        var sanitizedElements = new List<KeyValuePair<ScalarValue, LogEventPropertyValue>>(dictionary.Elements.Count);
        var anyChanged = false;

        foreach (var kvp in dictionary.Elements)
        {
            var sanitizedKey = SanitizeScalar(kvp.Key);
            var sanitizedValue = SanitizeValue(kvp.Value);

            if (!ReferenceEquals(kvp.Key, sanitizedKey) || !ReferenceEquals(kvp.Value, sanitizedValue))
            {
                anyChanged = true;
            }

            sanitizedElements.Add(new KeyValuePair<ScalarValue, LogEventPropertyValue>(sanitizedKey, sanitizedValue));
        }

        return anyChanged ? new DictionaryValue(sanitizedElements) : dictionary;
    }

    private static ScalarValue SanitizeScalar(ScalarValue scalar)
    {
        if (scalar.Value is string s)
        {
            var sanitized = LogSanitizerExtension.Sanitize(s);
            return sanitized == s ? scalar : new ScalarValue(sanitized);
        }

        return scalar;
    }
}
