using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.MappingProfiles;

public static class SubmodelsMapperProfile
{
    public static SubmodelsDto ToDto(this SubmodelList? submodelList)
    {
        return new SubmodelsDto
        {
            PagingMetaData = new PagingMetaDataDto
            {
                Cursor = submodelList?.PagingMetaData?.Cursor
            },
            Result = SerializeSubmodelsInParallel(submodelList?.Result)
        };
    }

    private static IList<JsonObject> SerializeSubmodelsInParallel(IList<ISubmodel>? submodels)
    {
        if (submodels is null || submodels.Count == 0)
        {
            return [];
        }

        var results = new JsonObject?[submodels.Count];
        _ = Parallel.For(0, submodels.Count, index =>
        {
            var submodel = submodels[index];
            if (submodel is null)
            {
                return;
            }

            try
            {
                results[index] = Jsonization.Serialize.ToJsonObject(submodel);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results[index] = null;
            }
        });

        return [.. results.OfType<JsonObject>()];
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
