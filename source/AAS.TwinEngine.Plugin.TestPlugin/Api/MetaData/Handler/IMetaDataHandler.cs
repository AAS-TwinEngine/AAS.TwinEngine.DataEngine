using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Requests;
using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Handler;

public interface IMetaDataHandler
{
    Task<ShellDescriptorsDto> GetShellDescriptors(GetShellDescriptorsRequest request, CancellationToken cancellationToken);

    Task<ShellDescriptorDto> GetShellDescriptor(GetShellDescriptorRequest request, CancellationToken cancellationToken);

    Task<AssetDto> GetAsset(GetAssetRequest request, CancellationToken cancellationToken);
}
