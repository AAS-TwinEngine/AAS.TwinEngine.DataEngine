using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository.MappingProfiles;

public static class SubmodelRefMapperProfile
{
    public static SubmodelRefDto ToDto(this SubmodelRef submodelRefs)
    {
        return new SubmodelRefDto
        {
            PagingMetaData = new PagingMetaDataDto
            {
                Cursor = submodelRefs.PagingMetaData!.Cursor
            },
            Result = submodelRefs.Result
        };
    }
}
