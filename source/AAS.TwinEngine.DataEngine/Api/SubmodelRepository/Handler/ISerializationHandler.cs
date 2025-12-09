using AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Requests;

using Microsoft.AspNetCore.Mvc;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRepository.Handler;

public interface ISerializationHandler
{
    Task<FileStreamResult> GetAasxFileAsync(SerializeAasxRequest request, CancellationToken cancellationToken);
}
