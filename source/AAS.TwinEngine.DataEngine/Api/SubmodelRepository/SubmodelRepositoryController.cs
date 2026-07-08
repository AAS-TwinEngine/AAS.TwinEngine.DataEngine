using System.Net;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using AasCore.Aas3_1;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository;

[ApiController]
[Route("submodels")]
[ApiVersion(1)]
public class SubmodelRepositoryController(
    ILogger<SubmodelRepositoryController> logger,
    ISubmodelRepositoryHandler submodelRepositoryHandler)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SubmodelsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubmodelsDto>> GetAllSubmodelsAsync(
        [FromQuery] GetAllSubmodelsRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get All Submodels");

        var response = await submodelRepositoryHandler
            .GetAllSubmodels(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{submodelIdentifier}")]
    [ProducesResponseType(typeof(ISubmodel), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetSubmodelAsync([FromRoute] string submodelIdentifier, CancellationToken cancellationToken)
    {
        logger.LogInformation("Get Submodel");
        var request = new GetSubmodelRequest(submodelIdentifier);
        var response = await submodelRepositoryHandler.GetSubmodel(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }

    [HttpGet("{submodelIdentifier}/submodel-elements/{idShortPath}")]
    [ProducesResponseType(typeof(ISubmodelElement), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetSubmodelElementAsync([FromRoute] string submodelIdentifier, [FromRoute] string idShortPath, CancellationToken cancellationToken)
    {
        logger.LogInformation("Get Submodel Element");
        var request = new GetSubmodelElementRequest(submodelIdentifier, idShortPath);
        var response = await submodelRepositoryHandler.GetSubmodelElement(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }
}
