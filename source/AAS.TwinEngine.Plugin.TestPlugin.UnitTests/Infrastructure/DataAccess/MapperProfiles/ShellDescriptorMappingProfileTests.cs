using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.Entity;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.MapperProfiles;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.DataAccess.MapperProfiles;

public class ShellDescriptorMappingProfileTests
{
    [Fact]
    public void MapToDomainModel_MapsAllFieldsCorrectly()
    {
        var entity = new MetaDataEntity
        {
            Id = "shell-001",
            GlobalAssetId = "asset-001",
            IdShort = "Shell001",
            SpecificAssetIds =
            [
                new SpecificAssetIdEntity { Name = "SerialNumber", Value = "SN001" },
                new SpecificAssetIdEntity { Name = "PartNumber", Value = "PN001" }
            ]
        };

        var result = entity.MapToDomainModel();

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.GlobalAssetId, result.GlobalAssetId);
        Assert.Equal(entity.IdShort, result.IdShort);
        Assert.Equal(2, result.SpecificAssetIds!.Count);
        Assert.Equal("SerialNumber", result.SpecificAssetIds[0].Name);
        Assert.Equal("SN001", result.SpecificAssetIds[0].Value);
    }

    [Fact]
    public void MapToDomainModel_HandlesNullSpecificAssetIds()
    {
        var entity = new MetaDataEntity
        {
            Id = "shell-002",
            GlobalAssetId = "asset-002",
            IdShort = "Shell002",
            SpecificAssetIds = null
        };

        var result = entity.MapToDomainModel();

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Empty(result.SpecificAssetIds!);
    }

    [Fact]
    public void ToDomainModelList_ReturnsEmptyList_WhenInputIsNull()
    {
        List<MetaDataEntity>? entities = null;

        var result = entities!.ToDomainModelList();

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
