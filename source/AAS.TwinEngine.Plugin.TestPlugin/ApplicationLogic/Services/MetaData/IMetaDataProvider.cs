using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;

public interface IMetaDataProvider
{
    Task<ShellDescriptorsData> GetShellDescriptorsAsync(int? limit, string? cursor, CancellationToken cancellationToken);

    Task<ShellDescriptorData> GetShellDescriptorAsync(string aasIdentifier, CancellationToken cancellationToken);

    Task<AssetData> GetAssetAsync(string assetIdentifier, CancellationToken cancellationToken);
}
