using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.MappingProfiles;

public static class SubmodelsMapperProfile
{
    public static SubmodelsDto ToDto(this SubmodelList submodelList)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.MapSubmodelResponse);
        _ = activity?.SetTag("submodel.count", submodelList.Result.Count);

        return new SubmodelsDto
        {
            PagingMetaData = new PagingMetaDataDto
            {
                Cursor = submodelList.PagingMetaData?.Cursor
            },
            Result = SerializeSubmodelsInParallel(submodelList.Result)
        };
    }

    private static IList<JsonObject> SerializeSubmodelsInParallel(IList<ISubmodel> submodels)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.SerializeSubmodels);
        _ = activity?.SetTag("submodel.count", submodels.Count);

        var results = new JsonObject[submodels.Count];
        Parallel.For(0, submodels.Count, index =>
        {
            results[index] = Jsonization.Serialize.ToJsonObject(submodels[index]);
        });
        return results;
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
