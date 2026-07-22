using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.MappingProfiles;

public static class SubmodelsMapperProfile
{
    public static SubmodelsDto ToDto(this SubmodelList submodelList)
    {
        return new SubmodelsDto
        {
            PagingMetaData = new PagingMetaDataDto
            {
                Cursor = submodelList.PagingMetaData?.Cursor
            },
            Result = [.. submodelList.Result.Select(Jsonization.Serialize.ToJsonObject)]
        };
    }

    public static SubmodelElementsDto ToDto(this SubmodelElementsPage submodelElementsPage)
    {
        return new SubmodelElementsDto
        {
            PagingMetaData = new PagingMetaDataDto
            {
                Cursor = submodelElementsPage.PagingMetaData?.Cursor
            },
            Result = [.. submodelElementsPage.Result.Select(Jsonization.Serialize.ToJsonObject)]
        };
    }
}
