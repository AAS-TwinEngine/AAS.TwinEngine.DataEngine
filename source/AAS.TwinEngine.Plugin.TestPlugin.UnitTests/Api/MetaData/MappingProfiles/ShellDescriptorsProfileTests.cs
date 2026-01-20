using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.MappingProfiles;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Api.MetaData.MappingProfiles;

public class ShellDescriptorsProfileTests
{
    [Fact]
    public void ToDto_MapsAllFieldsCorrectly()
    {
        var shellDescriptorsData = new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData { Cursor = "cursor-123" },
            Result = new List<ShellDescriptorData>
            {
                new()
                {
                    Id = "shell-001",
                    GlobalAssetId = "asset-001",
                    IdShort = "Shell001",
                    SpecificAssetIds = [new SpecificAssetIdsData { Name = "SerialNumber", Value = "SN001" }]
                }
            }
        };

        var result = shellDescriptorsData.ToDto();

        Assert.NotNull(result);
        Assert.NotNull(result.PagingMetaData);
        Assert.Equal("cursor-123", result.PagingMetaData.Cursor);
        Assert.Single(result.Result!);
        Assert.Equal("shell-001", result.Result![0].Id);
        Assert.Equal("asset-001", result.Result[0].GlobalAssetId);
        Assert.Equal("Shell001", result.Result[0].IdShort);
        Assert.Single(result.Result[0]!.SpecificAssetIds!);
        Assert.Equal("SerialNumber", result.Result[0].SpecificAssetIds?[0]!.Name);
        Assert.Equal("SN001", result.Result[0].SpecificAssetIds?[0].Value);
    }

    [Fact]
    public void ToDto_HandlesNullResultList()
    {
        var shellDescriptorsData = new ShellDescriptorsData
        {
            PagingMetaData = new PagingMetaData { Cursor = "cursor-456" },
            Result = null
        };

        var result = shellDescriptorsData.ToDto();

        Assert.NotNull(result);
        Assert.NotNull(result.PagingMetaData);
        Assert.Equal("cursor-456", result.PagingMetaData.Cursor);
        Assert.Null(result.Result);
    }
}
