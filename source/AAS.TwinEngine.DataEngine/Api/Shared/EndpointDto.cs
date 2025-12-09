using System.Text.Json.Serialization;

namespace AAS.TwinEngine.DataEngine.Api.Shared;

public class EndpointDto
{
    [JsonPropertyName("interface")]
    public string? Interface { get; set; }

    [JsonPropertyName("protocolInformation")]
    public ProtocolInformationDto? ProtocolInformation { get; set; }
}

public class ProtocolInformationDto
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
    public IList<SecurityAttributesDto>? SecurityAttributes { get; init; }
}

public class SecurityAttributesDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
