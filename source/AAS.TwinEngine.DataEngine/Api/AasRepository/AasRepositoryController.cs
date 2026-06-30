using System.Net;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.AasRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRepository.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using AasCore.Aas3_1;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace AAS.TwinEngine.DataEngine.Api.AasRepository;

[ApiController]
[Route("shells")]
[ApiVersion(1)]
public class AasRepositoryController(
    ILogger<AasRepositoryController> logger,
    IAasRepositoryHandler aasRepositoryHandler) : ControllerBase
{
    /// <summary>
    /// Returns Asset Administration Shells, optionally filtered by asset identifiers.
    /// </summary>
    /// <remarks>
    /// IDTA repository semantics: without <c>assetIds</c>, the endpoint returns paginated shells.
    /// With <c>assetIds</c>, only shells linked to the given assets are returned.
    /// </remarks>
    /// <param name="assetIds">Optional base64url encoded asset identifiers. Example: aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYXNzZXQvNDcxMQ.</param>
    /// <param name="limit">Maximum number of shells to return per page. Example: 100.</param>
    /// <param name="cursor">Opaque cursor from a previous response for pagination continuation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Shell list was returned.</response>
    /// <response code="400">Input parameters are invalid.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ShellsDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ShellsDto>> GetShellsByAssetIdAsync(
        [FromQuery, Description("Optional base64url encoded asset identifiers used to filter shells.")] string[]? assetIds,
        [FromQuery, Description("Maximum number of shells to return in one page. Example: 100.")] int? limit,
        [FromQuery, Description("Opaque cursor token from a previous page.")] string? cursor,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Start request to get shells by asset identifiers");
        var response = await aasRepositoryHandler.GetShellsByAssetIdsAsync(assetIds, limit, cursor, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>
    /// Returns a single Asset Administration Shell by identifier.
    /// </summary>
    /// <param name="aasIdentifier">Base64url encoded AAS identifier. Example: aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The shell was returned.</response>
    /// <response code="400">Identifier format is invalid.</response>
    /// <response code="404">No shell exists for the given identifier.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpGet("{aasIdentifier}")]
    [ProducesResponseType(typeof(IAssetAdministrationShell), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetShellByIdAsync(
        [FromRoute, Description("Base64url encoded AAS identifier.")] string aasIdentifier,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Start request to get shell");
        var request = new GetShellRequest(aasIdentifier);
        var response = await aasRepositoryHandler.GetShellByIdAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }

    /// <summary>
    /// Returns asset information for a specific Asset Administration Shell.
    /// </summary>
    /// <param name="aasIdentifier">Base64url encoded AAS identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Asset information was returned.</response>
    /// <response code="400">Identifier format is invalid.</response>
    /// <response code="404">No shell exists for the given identifier.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpGet("{aasIdentifier}/asset-information")]
    [ProducesResponseType(typeof(IAssetInformation), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<JsonObject>> GetAssetInformationByIdAsync(
        [FromRoute, Description("Base64url encoded AAS identifier.")] string aasIdentifier,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Start request to get asset information");
        var request = new GetAssetInformationRequest(aasIdentifier);
        var response = await aasRepositoryHandler.GetAssetInformationByIdAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(Jsonization.Serialize.ToJsonObject(response));
    }

    /// <summary>
    /// Returns submodel references associated with a specific Asset Administration Shell.
    /// </summary>
    /// <param name="aasIdentifier">Base64url encoded AAS identifier.</param>
    /// <param name="limit">Maximum number of references to return in one page. Example: 100.</param>
    /// <param name="cursor">Opaque cursor token from a previous response page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Submodel references were returned.</response>
    /// <response code="400">Input parameters are invalid.</response>
    /// <response code="404">No shell exists for the given identifier.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpGet("{aasIdentifier}/submodel-refs")]
    [ProducesResponseType(typeof(SubmodelRefDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<SubmodelRefDto>> GetSubmodelRefByIdAsync(
        [FromRoute, Description("Base64url encoded AAS identifier.")] string aasIdentifier,
        [FromQuery, Description("Maximum number of references to return in one page. Example: 100.")] int? limit,
        [FromQuery, Description("Opaque cursor token from a previous page.")] string? cursor,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Start request to get submodel-refs for shell");
        var request = new GetSubmodelRefRequest(aasIdentifier, limit, cursor);
        var response = await aasRepositoryHandler.GetSubmodelRefByIdAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
