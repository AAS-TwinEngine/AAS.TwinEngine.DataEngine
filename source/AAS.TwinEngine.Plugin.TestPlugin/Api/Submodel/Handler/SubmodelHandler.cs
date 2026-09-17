using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;
using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Responses;
using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;
using AAS.TwinEngine.Plugin.TestPlugin.Common.Extensions;

using NJsonSchema.Validation;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Handler;

public class SubmodelHandler(
    ILogger<SubmodelHandler> logger,
    ISubmodelService submodelService,
    IJsonSchemaValidator jsonSchemaValidator,
    IJsonSchemaParser jsonSchemaParser,
    ISemanticTreeHandler semanticTreeHandler) : ISubmodelHandler
{

    public Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        jsonSchemaValidator.ValidateRequestSchema(request.dataQuery);

        return GetResourceByIdAsync(
            request.submodelId,
            async (decodedId) =>
            {
                var semanticIds = jsonSchemaParser.ParseJsonSchema(request.dataQuery);

                var semanticTree = await submodelService.GetValuesBySemanticIds(
                    semanticIds,
                    decodedId).ConfigureAwait(false);

                return semanticTree;
            },
            (semanticTree) => semanticTreeHandler.GetJson(semanticTree, request.dataQuery)
        );
    }

    public async Task<IReadOnlyList<SubmodelDataBatchResult>> GetSubmodelDataBatch(
        IReadOnlyList<GetSubmodelDataBatchRequest> requests,
        CancellationToken cancellationToken)
    {
        var tasks = requests
            .SelectMany(request => request.SubmodelIds.Select(async encodedSubmodelId =>
            {
                string submodelId;
                try
                {
                    submodelId = encodedSubmodelId.DecodeBase64();
                }
                catch (ArgumentException ex)
                {
                    throw new BadRequestException("The request payload contains an invalid submodel ID.", ex);
                }
                var result = await GetSubmodelData(
                    new GetSubmodelDataRequest(submodelId, request.Schema),
                    cancellationToken).ConfigureAwait(false);

                return new SubmodelDataBatchResult(submodelId, result);
            }));

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task<TDto> GetResourceByIdAsync<TModel, TDto>(
        string? Id,
        Func<string, Task<TModel?>> fetchFunc,
        Func<TModel, TDto> mapFunc)
    {
        var result = await fetchFunc(Id!).ConfigureAwait(false);

        return mapFunc(result!);
    }
}

