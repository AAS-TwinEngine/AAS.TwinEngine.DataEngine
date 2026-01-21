using System.Text.Json;

namespace AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.Providers.SubmodelProviders.Helper;

public class JsonNodeInfo
{
    public JsonValueKind Kind { get; set; }
    public int? ArrayLength { get; set; }
    public string? StringValue { get; set; }
}
