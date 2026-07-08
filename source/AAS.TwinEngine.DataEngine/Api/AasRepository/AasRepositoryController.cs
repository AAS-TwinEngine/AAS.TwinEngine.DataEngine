using System.ComponentModel;
using System.Net;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using AasCore.Aas3_1;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using NSwag.Annotations;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository;

[ApiController]
[Route("shells")]
[OpenApiTags("Asset Administration Shell Repository API")]
[ApiVersion(1)]
public class AasRepositoryController(
    ILogger<AasRepositoryController> logger,
    IAasRepositoryHandler aasRepositoryHandler) : ControllerBase
{
    /// <summary>
    /// Returns all Asset Administration Shells
    /// </summary>
    /// <param name="assetIds">A list of specific Asset identifiers. Each Asset identifier is a base64-url-encoded SpecificAssetId</param>
    /// <param name="idShort">The Asset Administration Shell’s IdShort</param>
    /// <param name="limit">The maximum number of elements in the response array</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Requested Asset Administration Shells</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet]
    [ProducesResponseType(typeof(ShellsDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ShellsDto>> GetShellsByAssetIdAsync(
        [FromQuery] string[]? assetIds,
        [FromQuery] string? idShort,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Start request to get shells (assetIds/idShort filters)");
        var response = await aasRepositoryHandler.GetShellsByAssetIdsAsync(assetIds, idShort, limit, cursor, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>
    /// Returns a specific Asset Administration Shell
    /// </summary>
    /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Requested Asset Administration Shell</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{aasIdentifier}")]
    [ProducesResponseType(typeof(AssetAdministrationShell), (int)HttpStatusCode.OK)]
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

    /// <summary>
    /// Returns the Asset Information
    /// </summary>
    /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Requested Asset Information</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{aasIdentifier}/asset-information")]
    [ProducesResponseType(typeof(IAssetInformation), (int)HttpStatusCode.OK)]
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

    /// <summary>
    /// Returns all submodel references
    /// </summary>
    /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="limit">The maximum number of elements in the response array</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Requested submodel references</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{aasIdentifier}/submodel-refs")]
    [ProducesResponseType(typeof(SubmodelRefDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetSubmodelRefByIdAsync([FromRoute] string aasIdentifier, [FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken)
    {
        logger.LogInformation("Start request to get submodel-refs for shell");
        var request = new GetSubmodelRefRequest(aasIdentifier, limit, cursor);
        var response = await aasRepositoryHandler.GetSubmodelRefByIdAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
