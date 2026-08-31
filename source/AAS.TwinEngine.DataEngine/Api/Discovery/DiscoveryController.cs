using System.Net;

using AAS.TwinEngine.DataEngine.Api.Discovery.Handler;
using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using NSwag.Annotations;

namespace AAS.TwinEngine.DataEngine.Api.Discovery;

[ApiController]
[Route("lookup")]
[ApiVersion(1)]
[OpenApiTags("Asset Administration Shell Basic Discovery API")]
public class DiscoveryController(
    ILogger<DiscoveryController> logger,
    IDiscoveryHandler discoveryHandler) : ControllerBase
{
    /// <summary>
    /// Returns a list of Asset Administration Shell IDs linked to specific asset identifiers or the global asset ID
    /// </summary>
    /// <param name="assetLinks">A list of specific asset identifiers. Search for the global asset ID is supported by setting "name" to "globalAssetId" (see Constraint AASd-116).</param>
    /// <param name="limit">The maximum number of elements in the response array</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Requested Asset Administration Shell IDs</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="500">Internal Server Error</response>
    [HttpPost("shellsByAssetLink")]
    [ProducesResponseType(typeof(ShellsByAssetLinkResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ShellsByAssetLinkResponseDto>> SearchShellsByAssetLinkAsync(
        [FromBody] AssetLinkDto[] assetLinks,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken = default,
        [FromQuery] int limit = GeneralConfig.DefaultPaginationLimit)
    {
        logger.LogInformation("Start request to search shells by asset link");
        var request = new SearchShellsByAssetLinkRequest(assetLinks, limit, cursor);
        var response = await discoveryHandler.SearchShellsByAssetLinkAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>
    /// Returns a list of specific Asset identifiers based on an Asset Administration Shell id to edit discoverable content
    /// </summary>
    /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Requested specific Asset identifiers</response>
    /// <response code="400">Bad Request, e.g. the request parameters of the format of the request body is wrong.</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="404">Not Found</response>
    [HttpGet("shells/{aasIdentifier}")]
    [ProducesResponseType(typeof(IList<SpecificAssetId>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<IList<ISpecificAssetId>>> GetSpecificAssetIdByAasIdentifierAsync([FromRoute] string aasIdentifier, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Start request to specific asset identifiers by asset administration shell id");
        var request = new GetSpecificAssetIdByAasIdentifierRequest(aasIdentifier);
        var response = await discoveryHandler.GetSpecificAssetIdByAasIdentifierAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
