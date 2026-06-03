using System.Net;

using AAS.TwinEngine.DataEngine.Api.Discovery.Handler;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.Discovery;

[ApiController]
[ApiVersion(1)]
public class DiscoveryController(
    ILogger<DiscoveryController> logger,
    IDiscoveryHandler discoveryHandler) : ControllerBase
{
    [HttpPost("lookup/shellsByAssetLink")]
    [ProducesResponseType(typeof(ShellsByAssetLinkResponseDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ShellsByAssetLinkResponseDto>> SearchShellsByAssetLinkAsync(
        [FromBody] AssetLink[] assetLinks,
        [FromQuery] int? limit,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Start request to search shells by asset link");
        var response = await discoveryHandler.SearchShellsByAssetLinkAsync(assetLinks, limit, cursor, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
