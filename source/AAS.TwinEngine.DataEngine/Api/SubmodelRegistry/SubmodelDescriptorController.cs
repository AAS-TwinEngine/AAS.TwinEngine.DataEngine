using System.ComponentModel;
using System.Net;

using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using NSwag.Annotations;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRegistry;

[ApiController]
[Route("submodel-descriptors")]
[OpenApiTags("Submodel Registry API")]
[ApiVersion(1)]
public class SubmodelDescriptorController(
    ILogger<SubmodelDescriptorController> logger,
    ISubmodelDescriptorHandler submodelDescriptorHandler)
    : ControllerBase
{
    /// <summary>
    /// Returns a specific Submodel Descriptor.
    /// </summary>
    /// <param name="submodelIdentifier">The Submodel’s unique id (UTF8-BASE64-URL-encoded)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Requested Submodel Descriptor</response>
    /// <response code="400">Bad Request, e.g.the request parameters of the format of the request body is wrong.</response>
    /// <response code="404">Not Found</response>
    /// <response code="500">Internal Server Error</response>
    [HttpGet("{submodelIdentifier}")]
    [ProducesResponseType(typeof(SubmodelDescriptorDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<SubmodelDescriptorDto>> GetSubmodelDescriptorByIdAsync([FromRoute] string submodelIdentifier, CancellationToken cancellationToken)
    {
        logger.LogInformation("Get Submodel Descriptor");
        var request = new GetSubmodelDescriptorRequest(submodelIdentifier);
        var response = await submodelDescriptorHandler.GetSubmodelDescriptorById(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
