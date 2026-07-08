using System.ComponentModel;
using System.Net;

using AAS.TwinEngine.DataEngine.Api.AasRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using NSwag.Annotations;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry;

[ApiController]
[Route("shell-descriptors")]
[OpenApiTags("Asset Administration Shell Registry API")]
[ApiVersion(1)]
public class ShellDescriptorController(
    ILogger<ShellDescriptorController> logger,
    IShellDescriptorHandler shellDescriptorHandler)
    : ControllerBase
{
    /// <summary>
    /// Returns all Asset Administration Shell Descriptors
    /// </summary>
    /// <param name="limit">The maximum number of elements in the response array</param>
    /// <param name="cursor">A server-generated identifier retrieved from pagingMetadata that specifies from which position the result listing should continue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Requested Asset Administration Shell Descriptors</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="404">Not Found</response>
    [HttpGet]
    [ProducesResponseType(typeof(ShellDescriptorsDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<ShellDescriptorsDto>> GetAllShellDescriptorsAsync([FromQuery] int? limit, [FromQuery] string? cursor, CancellationToken cancellationToken)
    {
        logger.LogInformation("Get All ShellDescriptors");
        var request = new GetShellDescriptorsRequest(limit, cursor);
        var response = await shellDescriptorHandler.GetAllShellDescriptors(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>
    /// Returns a specific Asset Administration Shell Descriptor
    /// </summary>
    /// <param name="aasIdentifier">The Asset Administration Shell’s unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Requested Asset Administration Shell Descriptor</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
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
