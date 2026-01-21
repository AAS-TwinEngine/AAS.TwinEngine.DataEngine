using AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.MappingProfiles;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.MetaData.MappingProfiles;

public class AssetMappingProfileTests
{
    [Fact]
    public void ToDto_MapsAllFieldsCorrectly_WhenThumbnailIsPresent()
    {
        var assetData = new AssetData
        {
            GlobalAssetId = "asset-001",
            SpecificAssetIds = [new SpecificAssetIdsData { Name = "SerialNumber", Value = "SN001" }],
            DefaultThumbnail = new DefaultThumbnailData
            {
                ContentType = "image/png",
                Path = "/images/thumb.png"
            }
        };

        var result = assetData.ToDto();

        Assert.NotNull(result);
        Assert.Equal("asset-001", result.GlobalAssetId);
        Assert.Single(result.SpecificAssetIds!);
        Assert.Equal("SerialNumber", result.SpecificAssetIds![0].Name);
        Assert.Equal("SN001", result.SpecificAssetIds[0].Value);
        Assert.NotNull(result.DefaultThumbnail);
        Assert.Equal("image/png", result.DefaultThumbnail.ContentType);
        Assert.Equal("/images/thumb.png", result.DefaultThumbnail.Path);
    }

    [Fact]
    public void ToDto_SetsDefaultThumbnailToNull_WhenThumbnailIsMissing()
    {
        var assetData = new AssetData
        {
            GlobalAssetId = "asset-002",
            SpecificAssetIds = [],
            DefaultThumbnail = null
        };

        var result = assetData.ToDto();

        Assert.NotNull(result);
        Assert.Equal("asset-002", result.GlobalAssetId);
        Assert.Empty(result.SpecificAssetIds!);
        Assert.Null(result.DefaultThumbnail);
    }

    [Fact]
    public void ToDto_HandlesNullSpecificAssetIds()
    {
        var assetData = new AssetData
        {
            GlobalAssetId = "asset-003",
            SpecificAssetIds = null,
            DefaultThumbnail = null
        };

        var result = assetData.ToDto();

        Assert.NotNull(result);
        Assert.Equal("asset-003", result.GlobalAssetId);
        Assert.Null(result.DefaultThumbnail);
        Assert.Null(result.SpecificAssetIds);
    }
}
