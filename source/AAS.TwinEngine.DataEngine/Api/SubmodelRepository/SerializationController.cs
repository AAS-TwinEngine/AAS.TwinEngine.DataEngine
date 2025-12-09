using System.Net;

using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;
using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Responses;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository;

[ApiController]
[Route("serialization")]
[ApiVersion(1)]
public class SerializationController(
    ILogger<SerializationController> logger,
    ISerializationHandler serializationHandler) : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType(typeof(FileStreamResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.InternalServerError)]
    [ProducesResponseType(typeof(ServiceErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> SerializeAasxAsync([FromQuery] string[] aasIds,
                                                       [FromQuery] string[] submodelIds,
                                                       CancellationToken cancellationToken,
                                                       [FromQuery] bool includeConceptDescriptions = true)
    {
        logger.LogInformation("Start request to get aasx file");

        var request = new SerializeAasxRequest(aasIds, submodelIds, includeConceptDescriptions);

        var response = await serializationHandler.GetAasxFileAsync(request, cancellationToken).ConfigureAwait(false);

        return response;
    }
}
