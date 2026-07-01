using System.ComponentModel;
using System.Net;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using AasCore.Aas3_1;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using NSwag.Annotations;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository;

[ApiController]
[Route("submodels")]
[ApiVersion(1)]
[OpenApiTags("Submodel Repository API")]
public class SubmodelRepositoryController(
    ILogger<SubmodelRepositoryController> logger,
    ISubmodelRepositoryHandler submodelRepositoryHandler)
    : ControllerBase
{
    /// <summary>
    /// Returns a specific Submodel.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <response code="200">Requested Submodel</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}")]
    [ProducesResponseType(typeof(Submodel), (int)HttpStatusCode.OK)]
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

    /// <summary>
    /// Returns a specific submodel element from the submodel at a specified path
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="idShortPath">The IdShort path to the submodel element (dot-separated)</param>
    /// <response code="200">Requested submodel element</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
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
