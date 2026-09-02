using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;

public interface IShellDescriptorService
{
    Task<ShellDescriptors?> GetAllShellDescriptorsAsync(int limit, string? cursor, CancellationToken cancellationToken);

    Task<ShellDescriptor?> GetShellDescriptorByIdAsync(string id, CancellationToken cancellationToken);

    Task<SubmodelDescriptors?> GetAllSubmodelDescriptorsByAasIdAsync(string aasId, int limit, string? cursor, CancellationToken cancellationToken);

    Task<SubmodelDescriptor?> GetSubmodelDescriptorByAasIdAsync(string aasId, string submodelId, CancellationToken cancellationToken);
}
