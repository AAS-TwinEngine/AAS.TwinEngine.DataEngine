using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.MappingProfiles;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.UnitTests.Api.Shared.MappingProfiles;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.SubmodelRegistry.MappingProfiles;

public class SubmodelDescriptorMapperProfileTests
{
    [Fact]
    public void ToDto_Should_Map_SubmodelDescriptor_Correctly()
    {
        var source = TestDataMapperProfiles.CreateSubmodelDescriptor();

        var result = source.ToDto();

        Assert.NotNull(result);
        Assert.Equal("submodel-001", result.Id);
        Assert.Equal("SubShort", result.IdShort);

        Assert.NotNull(result.SemanticId);
        Assert.Equal(ReferenceTypes.ExternalReference, result.SemanticId.Type);
        Assert.Single(result.SemanticId.Keys!);
        Assert.Equal("https://example.com/key", result.SemanticId.Keys![0].Value);

        Assert.NotNull(result.Description);
        Assert.Single(result.Description);
        Assert.Equal("en", result.Description[0].Language);
        Assert.Equal("Sample Text", result.Description[0].Text);

        Assert.NotNull(result.DisplayName);
        Assert.Single(result.DisplayName);
        Assert.Equal("Sample Name", result.DisplayName[0].Text);

        Assert.NotNull(result.Extensions);
        Assert.Single(result.Extensions);
        Assert.Equal("Sample Extension", result.Extensions[0].Name);

        Assert.NotNull(result.Administration);
        Assert.Equal("1.0", result.Administration.Version);
        Assert.Equal("A", result.Administration.Revision);

        Assert.NotNull(result.SupplementalSemanticId);
        Assert.Single(result.SupplementalSemanticId);
        Assert.Equal(ReferenceTypes.ExternalReference, result.SupplementalSemanticId[0].Type);

        Assert.NotNull(result.Endpoints);
        Assert.Single(result.Endpoints);
        Assert.Equal("RESTful", result.Endpoints[0].Interface);
    }

    [Fact]
    public void ToDto_Should_Return_Null_When_SubmodelDescriptor_Is_Null()
    {
        SubmodelDescriptor? source = null;

        var result = source.ToDto();

        Assert.Null(result);
    }
}
