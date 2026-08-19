using AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;
using AAS.TwinEngine.DataEngine.Api.SubmodelRegistry.Responses;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry.Handler;

public interface IShellDescriptorHandler
{
    Task<ShellDescriptorsDto> GetAllShellDescriptors(GetShellDescriptorsRequest request, CancellationToken cancellationToken);

    Task<ShellDescriptorDto> GetShellDescriptorById(GetShellDescriptorRequest request, CancellationToken cancellationToken);

    Task<SubmodelDescriptorsDto> GetAllSubmodelDescriptorsByAasId(GetSubmodelDescriptorsByAasRequest request, CancellationToken cancellationToken);

    Task<SubmodelDescriptorDto> GetSubmodelDescriptorByAasId(GetSubmodelDescriptorByAasRequest request, CancellationToken cancellationToken);
}
