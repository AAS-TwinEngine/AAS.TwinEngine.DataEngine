using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.DomainModel.Shared;

public class EndpointData
{
    [JsonPropertyName("interface")]
    public string? Interface { get; set; }

    [JsonPropertyName("protocolInformation")]
    public ProtocolInformationData? ProtocolInformation { get; set; }
}

public class ProtocolInformationData
{
    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("endpointProtocol")]
    public string? EndpointProtocol { get; set; }

    [JsonPropertyName("endpointProtocolVersion")]
    public string? EndpointProtocolVersion { get; set; }

    [JsonPropertyName("subprotocol")]
    public string? SubProtocol { get; set; }

    [JsonPropertyName("subprotocolBody")]
    public string? SubProtocolBody { get; set; }

    [JsonPropertyName("subprotocolBodyEncoding")]
    public string? SubProtocolBodyEncoding { get; set; }

    [JsonPropertyName("securityAttributes")]
    public IList<SecurityAttributesData>? SecurityAttributes { get; init; }
}

public class SecurityAttributesData
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
