using AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.Shared.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.MappingProfiles;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry.MappingProfiles;

public static class ShellDescriptorMapperProfile
{
    public static ShellDescriptorsDto ToDto(this ShellDescriptors descriptors)
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

    public static ShellDescriptorDto ToDto(this ShellDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return new ShellDescriptorDto
        {
            Description = descriptor.Description,
            DisplayName = descriptor.DisplayName,
            Extensions = descriptor.Extensions,
            Administration = descriptor.Administration,
            AssetKind = descriptor.AssetKind,
            AssetType = descriptor.AssetType,
            GlobalAssetId = descriptor.GlobalAssetId,
            IdShort = descriptor.IdShort,
            Id = descriptor.Id,
            SpecificAssetIds = descriptor.SpecificAssetIds,
            SubmodelDescriptors = descriptor.SubmodelDescriptors?.Select(s => s.ToDto()).ToList(),
            Endpoints = descriptor.Endpoints?.Select(e => e.ToDto()).ToList()
        };
    }
}
