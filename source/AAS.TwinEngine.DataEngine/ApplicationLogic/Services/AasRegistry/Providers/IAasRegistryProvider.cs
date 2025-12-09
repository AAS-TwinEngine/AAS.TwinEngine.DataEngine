using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry.Providers;

public interface IAasRegistryProvider
{
    Task<List<ShellDescriptor>> GetAllAsync(CancellationToken cancellationToken);

    Task<ShellDescriptor> GetByIdAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task PutAsync(string aasIdentifier, ShellDescriptor shellDescriptorData, CancellationToken cancellationToken);

    Task DeleteByIdAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task CreateAsync(ShellDescriptor shellDescriptorData, CancellationToken cancellationToken);
}
