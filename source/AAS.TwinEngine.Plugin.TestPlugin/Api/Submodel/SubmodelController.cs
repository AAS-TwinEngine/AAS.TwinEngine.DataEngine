using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Handler;
using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using AAS.TwinEngine.Plugin.TestPlugin.Common.Extensions;

using Asp.Versioning;

using Json.Schema;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel;

[ApiController]
[Route("")]
[ApiVersion(1)]
public class SubmodelController(ISubmodelHandler submodelHandler) : ControllerBase
{
    [HttpPost("data/{submodelId}")]
    [ProducesResponseType(typeof(JsonObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ActionResult), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonObject>> RetrieveDataAsync([FromBody] JsonSchema? dataQuery, [FromRoute] string submodelId, CancellationToken cancellationToken)
    {
        var decodedSubmodelId = submodelId.DecodeBase64();
        if (dataQuery is null)
        {
            return BadRequest(ExceptionMessages.InvalidRequestPayload);
        }

        var request = new GetSubmodelDataRequest(decodedSubmodelId, dataQuery);

        var aasData = await submodelHandler.GetSubmodelData(request, cancellationToken);

        return Ok(aasData);
    }
}
