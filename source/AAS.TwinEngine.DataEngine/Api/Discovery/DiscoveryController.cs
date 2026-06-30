using System.Net;

using AAS.TwinEngine.DataEngine.Api.Discovery.Handler;
using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace AAS.TwinEngine.DataEngine.Api.Discovery;

[ApiController]
[Route("lookup")]
[ApiVersion(1)]
public class DiscoveryController(
    ILogger<DiscoveryController> logger,
    IDiscoveryHandler discoveryHandler) : ControllerBase
{
    /// <summary>
    /// Finds AAS identifiers by asset links.
    /// </summary>
    /// <remarks>
    /// IDTA Basic Discovery API semantics: each asset link is a name/value pair and the result contains matching shell identifiers.
    ///
    /// Example request body:
    /// [
    ///   { "name": "globalAssetId", "value": "https://example.com/ids/asset/4711" }
    /// ]
    /// </remarks>
    /// <param name="assetLinks">Asset links used for lookup. At least one entry is expected.</param>
    /// <param name="limit">Maximum number of returned identifiers for one page. Example: 100.</param>
    /// <param name="cursor">Opaque cursor token returned by a previous response for pagination continuation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Matching AAS identifiers and pagination metadata were returned.</response>
    /// <response code="400">Input is invalid, for example an empty body or malformed asset link entry.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpPost("shellsByAssetLink")]
    [ProducesResponseType(typeof(ShellsByAssetLinkResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ShellsByAssetLinkResponseDto>> SearchShellsByAssetLinkAsync(
        [FromBody] AssetLinkDto[] assetLinks,
        [FromQuery, Description("Maximum number of identifiers to return in one page. Example: 100.")] int? limit,
        [FromQuery, Description("Opaque cursor from a previous response used to continue paginated discovery results.")] string? cursor,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Start request to search shells by asset link");
        var response = await discoveryHandler.SearchShellsByAssetLinkAsync(assetLinks, limit, cursor, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
