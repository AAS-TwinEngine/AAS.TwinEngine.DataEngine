using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.MappingProfiles;

public static class ShellDescriptorProfile
{
    public static ShellDescriptorDto ToDto(this ShellDescriptorData data)
        => new()
        {
            GlobalAssetId = data.GlobalAssetId,
            IdShort = data.IdShort,
            Id = data.Id,
            SpecificAssetIds = data.SpecificAssetIds?
                .Select(x => new SpecificAssetIdsDto
                {
                    Name = x.Name,
                    Value = x.Value
                })
                .ToList()
        };
}
