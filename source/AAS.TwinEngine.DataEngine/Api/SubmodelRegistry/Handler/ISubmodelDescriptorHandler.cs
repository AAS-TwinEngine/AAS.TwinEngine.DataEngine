using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;

namespace AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Handler;

public interface ISubmodelDescriptorHandler
{
    Task<SubmodelDescriptorDto> GetSubmodelDescriptorById(GetSubmodelDescriptorRequest request, CancellationToken cancellationToken);
}
