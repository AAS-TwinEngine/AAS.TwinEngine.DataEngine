using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.MappingProfiles;

public static class ShellDescriptorsProfile
{
    public static ShellDescriptorsDto ToDto(this ShellDescriptorsData descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);

        return new ShellDescriptorsDto
        {
            PagingMetaData = new PagingMetaDataDto
            {
                Cursor = descriptors.PagingMetaData!.Cursor
            },
            Result = descriptors.Result?.Select(s => s.ToDto()).ToList()
        };
    }
}
