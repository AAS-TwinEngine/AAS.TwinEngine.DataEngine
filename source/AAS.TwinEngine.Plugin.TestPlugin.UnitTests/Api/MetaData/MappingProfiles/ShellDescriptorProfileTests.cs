using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.MappingProfiles;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Api.MetaData.MappingProfiles;

public class ShellDescriptorProfileTests
{
    [Fact]
    public void ToDto_MapsAllFieldsCorrectly()
    {
        var shellDescriptorData = new ShellDescriptorData
        {
            Id = "shell-001",
            GlobalAssetId = "asset-001",
            IdShort = "Shell001",
            SpecificAssetIds =
            [
                new SpecificAssetIdsData { Name = "SerialNumber", Value = "SN001" },
                new SpecificAssetIdsData { Name = "PartNumber", Value = "PN001" }
            ]
        };

        var result = shellDescriptorData.ToDto();

        Assert.NotNull(result);
        Assert.Equal("shell-001", result.Id);
        Assert.Equal("asset-001", result.GlobalAssetId);
        Assert.Equal("Shell001", result.IdShort);
        Assert.Equal(2, result.SpecificAssetIds!.Count);
        Assert.Equal("SerialNumber", result.SpecificAssetIds[0].Name);
        Assert.Equal("SN001", result.SpecificAssetIds[0].Value);
    }

    [Fact]
    public void ToDto_HandlesNullSpecificAssetIds()
    {
        var shellDescriptorData = new ShellDescriptorData
        {
            Id = "shell-002",
            GlobalAssetId = "asset-002",
            IdShort = "Shell002",
            SpecificAssetIds = null
        };

        var result = shellDescriptorData.ToDto();

        Assert.NotNull(result);
        Assert.Equal("shell-002", result.Id);
        Assert.Equal("asset-002", result.GlobalAssetId);
        Assert.Equal("Shell002", result.IdShort);
        Assert.Null(result.SpecificAssetIds);
    }

    [Fact]
    public void ToDto_HandlesEmptySpecificAssetIds()
    {
        var shellDescriptorData = new ShellDescriptorData
        {
            Id = "shell-003",
            GlobalAssetId = "asset-003",
            IdShort = "Shell003",
            SpecificAssetIds = []
        };

        var result = shellDescriptorData.ToDto();

        Assert.NotNull(result);
        Assert.Equal("shell-003", result.Id);
        Assert.Equal("asset-003", result.GlobalAssetId);
        Assert.Equal("Shell003", result.IdShort);
        Assert.Empty(result.SpecificAssetIds!);
    }
}
