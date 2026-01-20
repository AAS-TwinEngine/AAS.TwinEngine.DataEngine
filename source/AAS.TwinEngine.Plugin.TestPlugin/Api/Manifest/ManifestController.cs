using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.TestPlugin.Api.Manifest.Handler;
using Aas.TwinEngine.Plugin.TestPlugin.Api.Manifest.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.Manifest;

[ApiController]
[Route("")]
[ApiVersion(1)]
public class ManifestController(IManifestHandler manifestHandler) :ControllerBase
{
    [HttpGet("manifest")]
    [ProducesResponseType(typeof(JsonObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ManifestDto>> RetrieveManifestDataAsync(CancellationToken cancellationToken)
    {
        var manifestData = await manifestHandler.GetManifestData(cancellationToken);

        return Ok(manifestData);
    }
}
