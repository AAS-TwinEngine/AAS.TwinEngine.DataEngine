using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.Entity;

namespace AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.MapperProfiles;

public static class AssetMappingProfile
{
    public static AssetData ToDomainModel(this AssetInformationDataEntity entity, string globalAssetId, List<SpecificAssetIdsData>? specificAssetIds)
    {
        return new AssetData
        {
            GlobalAssetId = globalAssetId,
            SpecificAssetIds = specificAssetIds,
            DefaultThumbnail = entity?.DefaultThumbnail == null
                                   ? null
                                   : new DefaultThumbnailData
                                   {
                                       ContentType = entity.DefaultThumbnail.ContentType,
                                       Path = entity.DefaultThumbnail.Path
                                   }
        };
    }
}
