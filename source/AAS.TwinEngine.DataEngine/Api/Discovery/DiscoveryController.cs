using System.Net;

using AAS.TwinEngine.DataEngine.Api.Discovery.Handler;
using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.Discovery;

[ApiController]
[Route("lookup")]
[ApiVersion(1)]
public class DiscoveryController(
    ILogger<DiscoveryController> logger,
    IDiscoveryHandler discoveryHandler) : ControllerBase
{
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
