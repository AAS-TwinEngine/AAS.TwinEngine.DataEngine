using System.ComponentModel;
using System.Net;

using AAS.TwinEngine.DataEngine.Api.Discovery.Handler;
using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

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
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Start request to search shells by asset link");
        var response = await discoveryHandler.SearchShellsByAssetLinkAsync(assetLinks, limit, cursor, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
