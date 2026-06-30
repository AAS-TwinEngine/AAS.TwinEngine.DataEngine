using System.Net;

using AAS.TwinEngine.DataEngine.Api.AasRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry;

[ApiController]
[Route("shell-descriptors")]
[ApiVersion(1)]
public class ShellDescriptorController(
    ILogger<ShellDescriptorController> logger,
    IShellDescriptorHandler shellDescriptorHandler)
    : ControllerBase
{
    /// <summary>
    /// Returns shell descriptors from the AAS registry.
    /// </summary>
    /// <remarks>
    /// IDTA registry semantics with cursor-based pagination.
    /// </remarks>
    /// <param name="limit">Maximum number of descriptors to return in one page. Example: 100.</param>
    /// <param name="cursor">Opaque cursor token returned by a previous response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Shell descriptors were returned.</response>
    /// <response code="404">No descriptors are available for the current source configuration.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ShellDescriptorsDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ShellDescriptorsDto>> GetAllShellDescriptorsAsync(
        [FromQuery, Description("Maximum number of descriptors to return in one page. Example: 100.")] int? limit,
        [FromQuery, Description("Opaque cursor token from a previous page.")] string? cursor,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get All ShellDescriptors");
        var request = new GetShellDescriptorsRequest(limit, cursor);
        var response = await shellDescriptorHandler.GetAllShellDescriptors(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    /// <summary>
    /// Returns a shell descriptor by identifier.
    /// </summary>
    /// <param name="aasIdentifier">Base64url encoded AAS identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Shell descriptor was returned.</response>
    /// <response code="400">Identifier format is invalid.</response>
    /// <response code="404">No descriptor exists for the given identifier.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpGet("{aasIdentifier}")]
    [ProducesResponseType(typeof(ShellDescriptorDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<ShellDescriptorDto>> GetShellDescriptorByIdAsync(
        [FromRoute, Description("Base64url encoded AAS identifier.")] string aasIdentifier,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get ShellDescriptor");
        var request = new GetShellDescriptorRequest(aasIdentifier);
        var response = await shellDescriptorHandler.GetShellDescriptorById(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
