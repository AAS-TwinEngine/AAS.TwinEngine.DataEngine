using AAS.TwinEngine.DataEngine.Api.Shared.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.MappingProfiles;

public static class SubmodelDescriptorMapperProfile
{
    public static SubmodelDescriptorDto ToDto(this SubmodelDescriptor? descriptor)
    {
        return descriptor == null
                   ? null!
                   : new SubmodelDescriptorDto
                   {
                       IdShort = descriptor.IdShort,
                       Id = descriptor.Id,
                       SemanticId = descriptor.SemanticId,
                       Description = descriptor.Description,
                       DisplayName = descriptor.DisplayName,
                       Extensions = descriptor.Extensions,
                       Administration = descriptor.Administration,
                       SupplementalSemanticId = descriptor.SupplementalSemanticId,
                       Endpoints = descriptor.Endpoints?.Select(e => e.ToDto()).ToList()
                   };
    }
}
