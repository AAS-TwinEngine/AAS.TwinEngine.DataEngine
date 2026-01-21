using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.Entity;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.MapperProfiles;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Infrastructure.DataAccess.MapperProfiles;

public class AssetMappingProfileTests
{
    [Fact]
    public void ToDomainModel_MapsAllFields_WhenThumbnailIsPresent()
    {
        const string GlobalAssetId = "asset-123";
        var entity = new AssetInformationDataEntity { DefaultThumbnail = new DefaultThumbnailDataEntity { ContentType = "image/png", Path = "/images/thumb.png" }, GlobalAssetId = GlobalAssetId };
        var specificAssetIds = new List<SpecificAssetIdsData>
        {
            new() { Name = "SerialNumber", Value = "SN123" }
        };

        var result = entity.ToDomainModel(GlobalAssetId, specificAssetIds);

        Assert.NotNull(result);
        Assert.Equal(GlobalAssetId, result.GlobalAssetId);
        Assert.Equal(specificAssetIds, result.SpecificAssetIds);
        Assert.NotNull(result.DefaultThumbnail);
        Assert.Equal("image/png", result.DefaultThumbnail.ContentType);
        Assert.Equal("/images/thumb.png", result.DefaultThumbnail.Path);
    }

    [Fact]
    public void ToDomainModel_SetsDefaultThumbnailToNull_WhenThumbnailIsMissing()
    {
        var entity = new AssetInformationDataEntity
        {
            DefaultThumbnail = null
        };
        const string GlobalAssetId = "asset-456";
        var specificAssetIds = new List<SpecificAssetIdsData>();

        var result = entity.ToDomainModel(GlobalAssetId, specificAssetIds);

        Assert.NotNull(result);
        Assert.Equal(GlobalAssetId, result.GlobalAssetId);
        Assert.Equal(specificAssetIds, result.SpecificAssetIds);
        Assert.Null(result.DefaultThumbnail);
    }
}
