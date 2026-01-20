using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;
using Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Handler;

public class SubmodelHandler(
    ILogger<SubmodelHandler> logger,
    ISubmodelService submodelService,
    IJsonSchemaParser jsonSchemaParser,
    ISemanticTreeHandler semanticTreeHandler) : ISubmodelHandler
{
    public async Task<JsonObject> GetSubmodelData(GetSubmodelDataRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Start executing get request for product data");

        logger.LogInformation("Processing request for submodel ID: {submodelId}", request.submodelId);

        var semanticIds = jsonSchemaParser.ParseJsonSchema(request.dataQuery);

        var filledSemanticIds = await submodelService.GetValuesBySemanticIds(semanticIds, request.submodelId);

        var result = semanticTreeHandler.GetJson(filledSemanticIds, request.dataQuery);

        return result;
    }
}
