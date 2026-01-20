using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Handler;
using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Requests;
using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions.Responses;
using Aas.TwinEngine.Plugin.TestPlugin.Common.Extensions;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData;

[ApiController]
[Route("metadata")]
[ApiVersion(1)]
public class MetaDataController(IMetaDataHandler metaDataHandler) : ControllerBase
{
    [HttpGet("shells")]
    [ProducesResponseType(typeof(ShellDescriptorsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShellDescriptorsDto>> GetShellDescriptorsAsync([FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken)
    {
        var request = new GetShellDescriptorsRequest(limit, cursor);

        var response = await metaDataHandler.GetShellDescriptors(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("shells/{AasIdentifier}")]
    [ProducesResponseType(typeof(ShellDescriptorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShellDescriptorDto>> GetShellDescriptorAsync([FromRoute] string aasIdentifier, CancellationToken cancellationToken)
    {
        var decodedAasIdentifier = aasIdentifier.DecodeBase64();

        var request = new GetShellDescriptorRequest(decodedAasIdentifier);

        var response = await metaDataHandler.GetShellDescriptor(request, cancellationToken);

        return Ok(response);
    }

    [HttpGet("assets/{shellIdentifier}")]
    [ProducesResponseType(typeof(AssetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssetDto>> GetAssetAsync([FromRoute] string shellIdentifier, CancellationToken cancellationToken)
    {
        var decodedAasIdentifier = shellIdentifier.DecodeBase64();

        var request = new GetAssetRequest(decodedAasIdentifier);

        var response = await metaDataHandler.GetAsset(request, cancellationToken);

        return Ok(response);
    }
}
