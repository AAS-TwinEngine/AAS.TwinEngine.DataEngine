using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.Services;

public interface IAssetIdsFilterHeaderValidation
{
    AssetIdFilterHeader? ParseToDomainModel(string? headerValue);
}
