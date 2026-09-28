using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.Entity;

namespace AAS.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.MapperProfiles;

public static class ShellDescriptorMappingProfile
{
    public static ShellDescriptorData MapToDomainModel(this MetaDataEntity entity)
    {
        return new ShellDescriptorData
        {
            Id = entity.Id,
            GlobalAssetId = entity.GlobalAssetId,
            IdShort = entity.IdShort,
            AssetKind = entity.AssetKind,
            AssetType = entity.AssetType,
            DefaultThumbnail = entity.AssetInformationData?.DefaultThumbnail is null
                ? null
                : new DefaultThumbnailData
                {
                    Path = entity.AssetInformationData.DefaultThumbnail.Path,
                    ContentType = entity.AssetInformationData.DefaultThumbnail.ContentType
                },
            SpecificAssetIds = entity.SpecificAssetIds?.Select(s => new SpecificAssetIdsData
            {
                Name = s.Name,
                Value = s.Value
            }).ToList() ?? []
        };
    }

    public static IList<ShellDescriptorData> ToDomainModelList(this List<MetaDataEntity> dataList)
        => dataList?.Select(MapToDomainModel).ToList() ?? [];
}
