using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.DomainModel.Plugin;

public class PluginManifest
{
    [JsonPropertyName("pluginName")]
    public string PluginName { get; set; }

    [JsonPropertyName("pluginUrl")]
    public Uri PluginUrl { get; set; }

    [JsonPropertyName("supportedSemanticIds")]
    public required IList<string> SupportedSemanticIds { get; set; }

    [JsonPropertyName("capabilities")]
    public required Capabilities Capabilities { get; set; }
}

public class Capabilities
{
    [JsonPropertyName("hasShellDescriptor")]
    public bool HasShellDescriptor { get; set; }

    [JsonPropertyName("hasAssetInformation")]
    public bool HasAssetInformation { get; set; }
}
