using AAS.TwinEngine.DataEngine.DomainModel.Shared;

namespace AAS.TwinEngine.DataEngine.Api.Shared.MappingProfiles;

public static class EndpointMappingProfile
{
    public static EndpointDto ToDto(this EndpointData? endpoint)
    {
        return endpoint == null
                   ? null!
                   : new EndpointDto
                   {
                       Interface = endpoint.Interface,
                       ProtocolInformation = endpoint.ProtocolInformation?.ToDto()
                   };
    }

    public static ProtocolInformationDto ToDto(this ProtocolInformationData? protocolInformation)
    {
        return protocolInformation == null
                   ? null!
                   : new ProtocolInformationDto
                   {
                       Href = protocolInformation.Href,
                       EndpointProtocol = protocolInformation.EndpointProtocol,
                       EndpointProtocolVersion = protocolInformation.EndpointProtocolVersion,
                       SubProtocol = protocolInformation.SubProtocol,
                       SubProtocolBody = protocolInformation.SubProtocolBody,
                       SubProtocolBodyEncoding = protocolInformation.SubProtocolBodyEncoding,
                       SecurityAttributes = protocolInformation.SecurityAttributes?.Select(sa => new SecurityAttributesDto { Type = sa.Type, Key = sa.Key, Value = sa.Value }).ToList()
                   };
    }
}
