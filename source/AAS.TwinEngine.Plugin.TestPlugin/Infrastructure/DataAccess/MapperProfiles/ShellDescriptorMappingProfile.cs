using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;
using Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.Entity;

namespace Aas.TwinEngine.Plugin.TestPlugin.Infrastructure.DataAccess.MapperProfiles;

public static class ShellDescriptorMappingProfile
{
    public static ShellDescriptorData MapToDomainModel(this MetaDataEntity entity)
    {
        return new ShellDescriptorData
        {
            Id = entity.Id,
            GlobalAssetId = entity.GlobalAssetId,
            IdShort = entity.IdShort,
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
