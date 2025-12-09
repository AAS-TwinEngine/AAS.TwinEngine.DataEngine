using AAS.TwinEngine.DataEngine.Api.Shared.MappingProfiles;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Shared.MappingProfiles;

public class EndpointMappingProfileTests
{
    [Fact]
    public void ToDto_Should_Map_EndpointData_Correctly()
    {
        var source = TestDataMapperProfiles.CreateEndpointData();

        var result = source.ToDto();

        Assert.NotNull(result);
        Assert.Equal(source.Interface, result.Interface);
        Assert.NotNull(result.ProtocolInformation);
        Assert.Equal(source.ProtocolInformation!.Href, result.ProtocolInformation.Href);
        Assert.Equal(source.ProtocolInformation.EndpointProtocol, result.ProtocolInformation.EndpointProtocol);
        Assert.Equal(source.ProtocolInformation.EndpointProtocolVersion, result.ProtocolInformation.EndpointProtocolVersion);
        Assert.Equal(source.ProtocolInformation.SubProtocol, result.ProtocolInformation.SubProtocol);
        Assert.Equal(source.ProtocolInformation.SubProtocolBody, result.ProtocolInformation.SubProtocolBody);
        Assert.Equal(source.ProtocolInformation.SubProtocolBodyEncoding, result.ProtocolInformation.SubProtocolBodyEncoding);
        Assert.NotNull(result.ProtocolInformation.SecurityAttributes);
        Assert.Single(result.ProtocolInformation.SecurityAttributes);
        Assert.Equal("OAuth2", result.ProtocolInformation.SecurityAttributes[0].Type);
    }

    [Fact]
    public void ToDto_Should_Return_Null_When_EndpointData_Is_Null()
    {
        EndpointData? source = null;

        var result = source.ToDto();

        Assert.Null(result);
    }

    [Fact]
    public void ToDto_Should_Map_ProtocolInformationData_Correctly()
    {
        var source = TestDataMapperProfiles.CreateProtocolInformationData();

        var result = source.ToDto();

        Assert.Equal(source.Href, result.Href);
        Assert.Equal(source.EndpointProtocol, result.EndpointProtocol);
        Assert.Equal(source.EndpointProtocolVersion, result.EndpointProtocolVersion);
        Assert.Equal(source.SubProtocol, result.SubProtocol);
        Assert.Equal(source.SubProtocolBody, result.SubProtocolBody);
        Assert.Equal(source.SubProtocolBodyEncoding, result.SubProtocolBodyEncoding);
        Assert.NotNull(result.SecurityAttributes);
        Assert.Single(result.SecurityAttributes);
    }

    [Fact]
    public void ToDto_Should_Return_Null_When_ProtocolInformationData_Is_Null()
    {
        ProtocolInformationData? source = null;

        var result = source.ToDto();

        Assert.Null(result);
    }
}
