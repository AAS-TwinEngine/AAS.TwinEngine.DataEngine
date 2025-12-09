using System.Net;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using AasCore.Aas3_0;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository;

[ApiController]
[Route("shells")]
[ApiVersion(1)]
public class AasRepositoryController(
    ILogger<AasRepositoryController> logger,
    IAasRepositoryHandler aasRepositoryHandler) : ControllerBase
{
    [HttpGet("{aasIdentifier}")]
    [ProducesResponseType(typeof(IAssetAdministrationShell), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetShellByIdAsync([FromRoute] string aasIdentifier, CancellationToken cancellationToken)
    {
        logger.LogInformation("Start request to get shell");
        var request = new GetShellRequest(aasIdentifier);
        var response = await aasRepositoryHandler.GetShellByIdAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }

    [HttpGet("{aasIdentifier}/asset-information")]
    [ProducesResponseType(typeof(IAssetAdministrationShell), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetAssetInformationByIdAsync([FromRoute] string aasIdentifier, CancellationToken cancellationToken)
    {
        logger.LogInformation("Start request to get asset information");
        var request = new GetAssetInformationRequest(aasIdentifier);
        var response = await aasRepositoryHandler.GetAssetInformationByIdAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }

    [HttpGet("{aasIdentifier}/submodel-refs")]
    [ProducesResponseType(typeof(SubmodelRefDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetSubmodelRefByIdAsync([FromRoute] string aasIdentifier, int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken)
    {
        logger.LogInformation("Start request to get submodel-refs for shell");
        var request = new GetSubmodelRefRequest(aasIdentifier, limit, cursor);
        var response = await aasRepositoryHandler.GetSubmodelRefByIdAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
