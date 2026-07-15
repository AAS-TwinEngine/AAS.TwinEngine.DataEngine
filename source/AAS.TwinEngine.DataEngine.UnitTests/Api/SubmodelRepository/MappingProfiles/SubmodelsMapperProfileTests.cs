using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.MappingProfiles;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.SubmodelRepository.MappingProfiles;

public class SubmodelsMapperProfileTests
{
    [Fact]
    public void ToDto_ReturnsDto_WithPagingMetadataAndResults_WhenSubmodelListIsPopulated()
    {
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = "nextCursor" },
            Result =
            [
                new Submodel(
                    id: "https://mm-software.com/submodels/Nameplate",
                    idShort: "Nameplate",
                    semanticId: new Reference(
                        ReferenceTypes.ExternalReference,
                        [new Key(KeyTypes.Submodel, "https://admin-shell.io/ZVEI/Nameplate/2/0")]))
            ]
        };

        var result = submodelList.ToDto();

        Assert.NotNull(result);
        Assert.Equal("nextCursor", result.PagingMetaData?.Cursor);
        Assert.NotNull(result.Result);
        Assert.Single(result.Result!);
    }

    [Fact]
    public void ToDto_ReturnsEmptyResultList_WhenSubmodelListResultIsEmpty()
    {
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        var result = submodelList.ToDto();

        Assert.NotNull(result);
        Assert.Empty(result.Result!);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public void ToDto_ReturnsNullCursor_WhenPagingMetaDataIsNull()
    {
        var submodelList = new SubmodelList
        {
            PagingMetaData = null,
            Result = []
        };

        var result = submodelList.ToDto();

        Assert.NotNull(result);
        Assert.Null(result.PagingMetaData?.Cursor);
    }

    [Fact]
    public void ToDto_SerializesSubmodelToJsonObject_WithCorrectId()
    {
        const string SubmodelId = "https://mm-software.com/submodels/ContactInformation";
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData(),
            Result =
            [
                new Submodel(id: SubmodelId)
            ]
        };

        var result = submodelList.ToDto();

        Assert.NotNull(result.Result);
        Assert.Single(result.Result!);
        var jsonObj = result.Result![0];
        Assert.NotNull(jsonObj);
        Assert.Equal(SubmodelId, jsonObj["id"]?.GetValue<string>());
    }

    [Fact]
    public void ToDto_ReturnsMultipleJsonObjects_WhenResultContainsMultipleSubmodels()
    {
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = "page2" },
            Result =
            [
                new Submodel(id: "https://mm-software.com/submodels/sm-1"),
                new Submodel(id: "https://mm-software.com/submodels/sm-2"),
                new Submodel(id: "https://mm-software.com/submodels/sm-3")
            ]
        };

        var result = submodelList.ToDto();

        Assert.Equal(3, result.Result!.Count);
        Assert.Equal("https://mm-software.com/submodels/sm-1", result.Result![0]["id"]?.GetValue<string>());
        Assert.Equal("https://mm-software.com/submodels/sm-2", result.Result![1]["id"]?.GetValue<string>());
        Assert.Equal("https://mm-software.com/submodels/sm-3", result.Result![2]["id"]?.GetValue<string>());
    }
}
