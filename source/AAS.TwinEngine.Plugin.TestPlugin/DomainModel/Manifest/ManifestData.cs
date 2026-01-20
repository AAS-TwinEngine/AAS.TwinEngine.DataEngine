using System.Text.Json.Serialization;

namespace Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Manifest;

public class ManifestData
{
    [JsonPropertyName("supportedSemanticIds")]
    public required IList<string> SupportedSemanticIds { get; set; }

    [JsonPropertyName("capabilities")]
    public required CapabilitiesData Capabilities { get; set; }
}

public class CapabilitiesData
{
    [JsonPropertyName("hasShellDescriptor")]
    public bool HasShellDescriptor { get; set; }

    [JsonPropertyName("hasAssetInformation")]
    public bool HasAssetInformation { get; set; }
}
