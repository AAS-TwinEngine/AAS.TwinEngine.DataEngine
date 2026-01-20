using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.MappingProfiles;
using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Requests;
using Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Responses;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.MetaData;
using Aas.TwinEngine.Plugin.TestPlugin.Common.Extensions;

namespace Aas.TwinEngine.Plugin.TestPlugin.Api.MetaData.Handler;

public class MetaDataHandler(
    ILogger<MetaDataHandler> logger,
    IMetaDataService metaDataService) : IMetaDataHandler
{
    public async Task<ShellDescriptorsDto> GetShellDescriptors(GetShellDescriptorsRequest request, CancellationToken cancellationToken)
    {
        request?.Limit.ValidateLimit(logger);

        logger.LogDebug("Start executing get request for shell-descriptors metadata");

        var shellDescriptors = await metaDataService.GetShellDescriptorsAsync(request?.Limit, request?.Cursor, cancellationToken);

        return shellDescriptors.ToDto();
    }

    public async Task<ShellDescriptorDto> GetShellDescriptor(GetShellDescriptorRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug($"Start executing get request for shell-descriptor metadata for {request.aasIdentifier}");

        var shellDescriptorMetaData = await metaDataService.GetShellDescriptorAsync(request.aasIdentifier, cancellationToken);

        if (shellDescriptorMetaData != null)
        {
            var response = shellDescriptorMetaData.ToDto();
            return response;
        }

        logger.LogWarning($"Shell-descriptor metadata not found for {request.aasIdentifier}.");
        throw new NotFoundException(ExceptionMessages.ShellDescriptorDataNotFound);
    }

    public async Task<AssetDto> GetAsset(GetAssetRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Start executing get request for asset metadata element");

        var assetMetaData = await metaDataService.GetAssetAsync(request.shellIdentifier, cancellationToken);

        if (assetMetaData != null)
        {
            var response = assetMetaData.ToDto();
            return response;
        }

        logger.LogWarning($"Asset metadata not found for {request.shellIdentifier}.");
        throw new NotFoundException(ExceptionMessages.AssetNotFound);
    }
}
