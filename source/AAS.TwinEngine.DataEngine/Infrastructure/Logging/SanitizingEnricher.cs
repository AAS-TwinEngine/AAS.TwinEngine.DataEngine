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
        return value switch
        {
            ScalarValue { Value: string s } => new ScalarValue(LogSanitizer.Sanitize(s)),
            SequenceValue seq => new SequenceValue(seq.Elements.Select(SanitizeValue)),
            StructureValue str => new StructureValue(
                str.Properties.Select(p => new LogEventProperty(p.Name, SanitizeValue(p.Value))),
                str.TypeTag),
            DictionaryValue dict => new DictionaryValue(
                dict.Elements.Select(kvp => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                    SanitizeScalar(kvp.Key), SanitizeValue(kvp.Value)))),
            _ => value
        };
    }

    private static ScalarValue SanitizeScalar(ScalarValue scalar) => scalar.Value is string s ? new ScalarValue(LogSanitizer.Sanitize(s)) : scalar;
}
