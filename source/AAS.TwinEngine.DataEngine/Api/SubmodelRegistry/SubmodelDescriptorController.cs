using System.Net;

using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRegistry;

[ApiController]
[Route("submodel-descriptors")]
[ApiVersion(1)]
public class SubmodelDescriptorController(
    ILogger<SubmodelDescriptorController> logger,
    ISubmodelDescriptorHandler submodelDescriptorHandler)
    : ControllerBase
{
    /// <summary>
    /// Returns a submodel descriptor by identifier.
    /// </summary>
    /// <param name="submodelIdentifier">Base64url encoded submodel identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Submodel descriptor was returned.</response>
    /// <response code="400">Identifier format is invalid.</response>
    /// <response code="404">No descriptor exists for the given identifier.</response>
    /// <response code="500">Unexpected server-side error occurred.</response>
    [HttpGet("{submodelIdentifier}")]
    [ProducesResponseType(typeof(SubmodelDescriptorDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<ActionResult<SubmodelDescriptorDto>> GetSubmodelDescriptorByIdAsync(
        [FromRoute, Description("Base64url encoded submodel identifier.")] string submodelIdentifier,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get Submodel Descriptor");
        var request = new GetSubmodelDescriptorRequest(submodelIdentifier);
        var response = await submodelDescriptorHandler.GetSubmodelDescriptorById(request, cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }
}
