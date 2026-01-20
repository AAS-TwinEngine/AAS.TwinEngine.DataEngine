using System.Text.Json;

namespace Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers;

public record MockData
{
    public static JsonDocument MetaData { get; set; }
    public static JsonDocument SubmodelData { get; set; }
}
