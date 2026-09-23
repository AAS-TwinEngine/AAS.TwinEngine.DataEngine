using System.Text.Json.Nodes;

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
            Result = SerializeSubmodelsInParallel(submodelList.Result)
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

    private static IList<JsonObject> SerializeSubmodelsInParallel(IList<ISubmodel>? submodels)
    {
        var results = new JsonObject[submodels.Count];
        _ = Parallel.For(0, submodels.Count, index => results[index] = Jsonization.Serialize.ToJsonObject(submodels[index]));
        return results;
    }
}
