using AAS.TwinEngine.DataEngine.Api.AasRegistry.MappingProfiles;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.UnitTests.Api.Shared.MappingProfiles;

using AasCore.Aas3_0;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.AasRegistry.MappingProfiles;

public class ShellDescriptorMapperProfileTests
{
    [Fact]
    public void ToDto_ShouldThrow_WhenShellDescriptorsIsNull()
    {
        ShellDescriptors? descriptors = null;

        Assert.Throws<ArgumentNullException>(() => descriptors!.ToDto());
    }

    [Fact]
    public void ToDto_Should_Map_ShellDescriptors_Correctly()
    {
        var sourceList = TestDataMapperProfiles.CreateShellDescriptors();

        var result = sourceList.ToDto();

        Assert.NotNull(result);
        Assert.NotNull(result.Result);
        Assert.Equal(2, result.Result.Count);

        var dto = result.Result[0];
        Assert.Equal("shell-001", dto.Id);
        Assert.Equal("ShellShort", dto.IdShort);
        Assert.Equal(AssetKind.Type, dto.AssetKind);
        Assert.Equal(AssetKind.Type, dto.AssetType);
        Assert.Equal("global-asset-id", dto.GlobalAssetId);

        Assert.NotNull(dto.Description);
        Assert.Single(dto.Description);
        Assert.Equal("en", dto.Description[0].Language);
        Assert.Equal("Sample Text", dto.Description[0].Text);

        Assert.NotNull(dto.DisplayName);
        Assert.Single(dto.DisplayName);
        Assert.Equal("Sample Name", dto.DisplayName[0].Text);

        Assert.NotNull(dto.Extensions);
        Assert.Single(dto.Extensions);
        Assert.Equal("Sample Extension", dto.Extensions[0].Name);

        Assert.NotNull(dto.Administration);
        Assert.Equal("1.0", dto.Administration.Version);
        Assert.Equal("A", dto.Administration.Revision);

        Assert.NotNull(dto.SpecificAssetIds);
        Assert.Single(dto.SpecificAssetIds);
        Assert.Equal("SpecificAssetIds", dto.SpecificAssetIds[0].Name);

        Assert.NotNull(dto.SubmodelDescriptors);
        Assert.Single(dto.SubmodelDescriptors);
        Assert.Equal("submodel-001", dto.SubmodelDescriptors[0].Id);

        Assert.NotNull(dto.Endpoints);
        Assert.Single(dto.Endpoints);
        Assert.Equal("RESTful", dto.Endpoints[0].Interface);

        Assert.NotNull(result.PagingMetaData);
        Assert.Equal("shell-001-encodedValue", result.PagingMetaData.Cursor);
    }

    [Fact]
    public void ToDto_ShellDescriptor_Null_ThrowsArgumentNullException()
    {
        ShellDescriptor? descriptor = null;

        Assert.Throws<ArgumentNullException>(() => descriptor!.ToDto());
    }

    [Fact]
    public void ToDto_ValidShellDescriptor_ReturnsMappedDto()
    {
        var descriptor = TestDataMapperProfiles.CreateShellDescriptor();

        var dto = descriptor.ToDto();

        Assert.NotNull(dto);
        Assert.Equal("shell-001", dto.Id);
        Assert.Equal("ShellShort", dto.IdShort);
        Assert.Equal(AssetKind.Type, dto.AssetKind);
        Assert.Equal(AssetKind.Type, dto.AssetType);
        Assert.Equal("global-asset-id", dto.GlobalAssetId);
        Assert.NotNull(dto.Description);
        Assert.Single(dto.Description);
        Assert.Equal("en", dto.Description[0].Language);
        Assert.Equal("Sample Text", dto.Description[0].Text);
        Assert.NotNull(dto.DisplayName);
        Assert.Single(dto.DisplayName);
        Assert.Equal("Sample Name", dto.DisplayName[0].Text);
        Assert.NotNull(dto.Extensions);
        Assert.Single(dto.Extensions);
        Assert.Equal("Sample Extension", dto.Extensions[0].Name);
        Assert.NotNull(dto.Administration);
        Assert.Equal("1.0", dto.Administration.Version);
        Assert.Equal("A", dto.Administration.Revision);
        Assert.NotNull(dto.SpecificAssetIds);
        Assert.Single(dto.SpecificAssetIds);
        Assert.Equal("SpecificAssetIds", dto.SpecificAssetIds[0].Name);
        Assert.NotNull(dto.SubmodelDescriptors);
        Assert.Single(dto.SubmodelDescriptors);
        Assert.Equal("submodel-001", dto.SubmodelDescriptors[0].Id);
        Assert.NotNull(dto.Endpoints);
        Assert.Single(dto.Endpoints);
        Assert.Equal("RESTful", dto.Endpoints[0].Interface);
    }
}
