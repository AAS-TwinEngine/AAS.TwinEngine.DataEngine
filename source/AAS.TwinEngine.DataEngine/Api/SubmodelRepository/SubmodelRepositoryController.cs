using System.ComponentModel;
using System.Net;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared.Results;
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
    /// Returns all Submodels.
    /// </summary>
    /// <param name="semanticId">The value of the semantic id reference (UTF8-BASE64-URL-encoded).</param>
    /// <param name="idShort">The Asset Administration Shell's IdShort.</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata.</param>
    /// <param name="level">Determines the structural depth of the returned content. Accepted values: <c>deep</c>, <c>core</c>.</param>
    /// <param name="extent">Determines the serialization of the returned content. Accepted values: <c>withBlobValue</c>, <c>withoutBlobValue</c>.</param>
    /// <response code="200">Returns the requested Submodels.</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found.</response>
    /// <response code="500">Internal Server Error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(SubmodelsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubmodelsDto>> GetAllSubmodelsAsync(
        [FromQuery] string? semanticId,
        [FromQuery] string? idShort,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery] Level? level,
        [FromQuery] Extent? extent,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get All Submodels");

        var request = new GetAllSubmodelsRequest
        {
            SemanticId = semanticId,
            IdShort = idShort,
            Limit = limit,
            Cursor = cursor,
            Level = level,
            Extent = extent
        };

        var response = await submodelRepositoryHandler
            .GetAllSubmodels(request, cancellationToken)
            .ConfigureAwait(false);

        return Ok(response);
    }

    /// <summary>
    /// Returns a specific Submodel.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="level">Determines the structural depth of the returned content. Accepted values: <c>deep</c>, <c>core</c>.</param>
    /// <param name="extent">Controls how blob values are serialized. Accepted values: <c>withBlobValue</c>, <c>withoutBlobValue</c>.</param>
    /// <response code="200">Requested Submodel</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}")]
    [ProducesResponseType(typeof(Submodel), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetSubmodelAsync(
        [FromRoute] string submodelIdentifier,
        [FromQuery] Level? level,
        [FromQuery] Extent? extent,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get Submodel");
        var request = new GetSubmodelRequest(submodelIdentifier) { Level = level, Extent = extent };
        var response = await submodelRepositoryHandler.GetSubmodel(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }

    /// <summary>
    /// Returns all SubmodelElements including their hierarchy.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="limit">The maximum number of elements in the response array.</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue.</param>
    /// <param name="level">Determines the structural depth of the returned content. Accepted values: <c>deep</c>, <c>core</c>.</param>
    /// <param name="extent">Determines the serialization of the returned content. Accepted values: <c>withBlobValue</c>, <c>withoutBlobValue</c>.</param>
    /// <response code="200">List of found submodel elements</response>
    /// <response code="400">Bad Request, e.g. the request parameters or request format are invalid.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements")]
    [ProducesResponseType(typeof(SubmodelElementsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SubmodelElementsDto>> GetAllSubmodelElementsAsync(
        [FromRoute] string submodelIdentifier,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        [FromQuery] Level? level,
        [FromQuery] Extent? extent,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get All Submodel Elements");
        var request = new GetAllSubmodelElementsRequest(submodelIdentifier, limit, cursor, level, extent);
        var response = await submodelRepositoryHandler.GetAllSubmodelElements(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
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

    /// <summary>
    /// Downloads file content from a specific submodel element from the Submodel at a specified path.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel's unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="idShortPath">The IdShort path to the File SubmodelElement (dot-separated)</param>
    /// <response code="200">Requested File.</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}/submodel-elements/{idShortPath}/attachment")]
    [ProducesResponseType(typeof(FileResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetFileAttachmentAsync(
        [FromRoute] string submodelIdentifier,
        [FromRoute] string idShortPath,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get File Attachment");
        var request = new GetSubmodelElementRequest(submodelIdentifier, idShortPath);
        var attachment = await submodelRepositoryHandler.GetFileAttachment(request, cancellationToken).ConfigureAwait(false);
        return new FileContentStreamResult(attachment, ContentDispositionType.attachment);
    }
}
