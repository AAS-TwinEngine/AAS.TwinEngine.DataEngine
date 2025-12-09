using AAS.TwinEngine.DataEngine.Api.AasRegistry.Requests;
using AAS.TwinEngine.DataEngine.Api.AasRegistry.Responses;

namespace AAS.TwinEngine.DataEngine.Api.AasRegistry.Handler;

public interface IShellDescriptorHandler
{
    Task<ShellDescriptorsDto> GetAllShellDescriptors(GetShellDescriptorsRequest request, CancellationToken cancellationToken);

    Task<ShellDescriptorDto> GetShellDescriptorById(GetShellDescriptorRequest request, CancellationToken cancellationToken);
}
