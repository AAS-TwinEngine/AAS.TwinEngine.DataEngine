using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;

public interface IShellDescriptorService
{
    Task<ShellDescriptors?> GetAllShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken);

    Task<ShellDescriptor?> GetShellDescriptorByIdAsync(string id, CancellationToken cancellationToken);

    Task SyncShellDescriptorsAsync(CancellationToken cancellationToken);
}
