using System.Text.Json.Serialization;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Manifest.Responses;

public class ManifestDto
{
    [JsonPropertyName("supportedSemanticIds")]
    public required IList<string> SupportedSemanticIds { get; init; }

    [JsonPropertyName("capabilities")]
    public required CapabilitiesDto Capabilities { get; set; }
}

public class CapabilitiesDto
{
    [JsonPropertyName("hasShellDescriptor")]
    public bool HasShellDescriptor { get; set; }

    [JsonPropertyName("hasAssetInformation")]
    public bool HasAssetInformation { get; set; }
}
