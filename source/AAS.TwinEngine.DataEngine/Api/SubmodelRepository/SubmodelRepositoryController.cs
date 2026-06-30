using System.Net;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using AasCore.Aas3_1;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository;

[ApiController]
[Route("submodels")]
[ApiVersion(1)]
public class SubmodelRepositoryController(
    ILogger<SubmodelRepositoryController> logger,
    ISubmodelRepositoryHandler submodelRepositoryHandler)
    : ControllerBase
{
    /// <summary>
    /// Returns a submodel by identifier.
    /// </summary>
    /// <param name="submodelIdentifier">Base64url encoded submodel identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Submodel was returned.</response>
    /// <response code="400">Identifier format is invalid.</response>
    /// <response code="404">No submodel exists for the given identifier.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpGet("{submodelIdentifier}")]
    [ProducesResponseType(typeof(ISubmodel), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetSubmodelAsync(
        [FromRoute, Description("Base64url encoded submodel identifier.")] string submodelIdentifier,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get Submodel");
        var request = new GetSubmodelRequest(submodelIdentifier);
        var response = await submodelRepositoryHandler.GetSubmodel(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }

    /// <summary>
    /// Returns a submodel element by idShort path.
    /// </summary>
    /// <param name="submodelIdentifier">Base64url encoded submodel identifier.</param>
    /// <param name="idShortPath">URL-encoded idShort path to a nested submodel element. Example: Nameplate/ManufacturerName.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Submodel element was returned.</response>
    /// <response code="400">Input format is invalid.</response>
    /// <response code="404">No element exists for the given identifier or path.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/{idShortPath}")]
    [ProducesResponseType(typeof(ISubmodelElement), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetSubmodelElementAsync(
        [FromRoute, Description("Base64url encoded submodel identifier.")] string submodelIdentifier,
        [FromRoute, Description("URL-encoded idShort path to the target submodel element.")] string idShortPath,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get Submodel Element");
        var request = new GetSubmodelElementRequest(submodelIdentifier, idShortPath);
        var response = await submodelRepositoryHandler.GetSubmodelElement(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }
}
