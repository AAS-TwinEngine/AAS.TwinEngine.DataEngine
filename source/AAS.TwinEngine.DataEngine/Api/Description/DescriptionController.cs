using System.Net;

using AAS.TwinEngine.DataEngine.Api.Description.Handler;
using AAS.TwinEngine.DataEngine.Api.Description.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using NSwag.Annotations;

namespace AAS.TwinEngine.DataEngine.Api.Description;

[ApiController]
[Route("description")]
[OpenApiTags("Description API")]
[ApiVersion(1)]
public class DescriptionController(
    ILogger<DescriptionController> logger,
    IDescriptionHandler descriptionHandler) : ControllerBase
{
    /// <summary>
    /// Returns the self-describing information of a network resource (ServiceDescription)
    /// </summary>
    /// <response code="200">Requested Description</response>
    /// <response code="403">Forbidden</response>
    [HttpGet("")]
    [ProducesResponseType(typeof(ServiceDescriptionDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.Forbidden)]
    public ActionResult<ServiceDescriptionDto> GetDescriptor(CancellationToken cancellationToken)
    {
        logger.LogInformation("Get Description");

        var response = descriptionHandler.GetDescriptor(cancellationToken);

        return Ok(response);
    }
}
