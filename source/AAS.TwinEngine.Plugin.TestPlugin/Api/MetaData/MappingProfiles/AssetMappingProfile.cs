using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.MappingProfiles;

public static class AssetMappingProfile
{
    public static AssetDto ToDto(this AssetData? data)
    {
        return new AssetDto
        {
            GlobalAssetId = data.GlobalAssetId,
            SpecificAssetIds = data.SpecificAssetIds?.Select(id => new SpecificAssetIdsDto
            {
                Name = id.Name,
                Value = id.Value
            }).ToList(),
            DefaultThumbnail = data.DefaultThumbnail == null
                                   ? null
                                   : new DefaultThumbnailDto
                                   {
                                       ContentType = data.DefaultThumbnail.ContentType,
                                       Path = data.DefaultThumbnail.Path
                                   }
        };
    }
}
