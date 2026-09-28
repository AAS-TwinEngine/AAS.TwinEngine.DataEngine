using AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.MappingProfiles;

public static class ShellDescriptorProfile
{
    public static ShellDescriptorDto ToDto(this ShellDescriptorData data)
        => new()
        {
            GlobalAssetId = data.GlobalAssetId,
            IdShort = data.IdShort,
            Id = data.Id,
            AssetKind = data.AssetKind,
            AssetType = data.AssetType,
            DefaultThumbnail = data.DefaultThumbnail is null
                ? null
                : new DefaultThumbnailDto
                {
                    Path = data.DefaultThumbnail.Path,
                    ContentType = data.DefaultThumbnail.ContentType
                },
            SpecificAssetIds = data.SpecificAssetIds?
                .Select(x => new SpecificAssetIdsDto
                {
                    Name = x.Name,
                    Value = x.Value
                })
                .ToList()
        };
}
