using System.Net;

using AAS.TwinEngine.DataEngine.Api.AasRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry;

[ApiController]
[Route("shell-descriptors")]
[ApiVersion(1)]
public class ShellDescriptorController(
    ILogger<ShellDescriptorController> logger,
    IShellDescriptorHandler shellDescriptorHandler)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ShellDescriptorsDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ShellDescriptorsDto>> GetAllShellDescriptorsAsync(int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken)
    {
        logger.LogInformation("Get All ShellDescriptors");
        var request = new GetShellDescriptorsRequest(limit, cursor);
        var response = await shellDescriptorHandler.GetAllShellDescriptors(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    [HttpGet("{aasIdentifier}")]
    [ProducesResponseType(typeof(ShellDescriptorDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ShellDescriptorDto>> GetShellDescriptorByIdAsync([FromRoute] string aasIdentifier, CancellationToken cancellationToken)
    {
        logger.LogInformation("Get ShellDescriptor");
        var request = new GetShellDescriptorRequest(aasIdentifier);
        var response = await shellDescriptorHandler.GetShellDescriptorById(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
