using System.Net;

using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRegistry;

[ApiController]
[Route("submodel-descriptors")]
[ApiVersion(1)]
public class SubmodelDescriptorController(
    ILogger<SubmodelDescriptorController> logger,
    ISubmodelDescriptorHandler submodelDescriptorHandler)
    : ControllerBase
{
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
