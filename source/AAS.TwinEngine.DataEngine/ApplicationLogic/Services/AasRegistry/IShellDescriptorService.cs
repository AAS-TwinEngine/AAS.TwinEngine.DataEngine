using AasCore.Aas3_1;

using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRegistry;

public interface IShellDescriptorService
{
    Task<ShellDescriptors?> GetAllShellDescriptorsAsync(int? limit, string? cursor, AssetKind? assetKind, string? assetType, CancellationToken cancellationToken);

    Task<ShellDescriptor?> GetShellDescriptorByIdAsync(string id, CancellationToken cancellationToken);
}
