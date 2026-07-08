using AAS.TwinEngine.DataEngine.Api.Discovery.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.Api.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Handler;

public class DiscoveryHandler(ILogger<DiscoveryHandler> logger, IAssetIdSearchService assetIdSearchService) : IDiscoveryHandler
{
    public async Task<ShellsByAssetLinkResponseDto> SearchShellsByAssetLinkAsync(
        SearchShellsByAssetLinkRequest request, CancellationToken cancellationToken)
    {
        request.Limit.ValidateLimit(logger);
        request.Cursor?.ValidateCursor(logger);

        ValidateAssetLinks(request.AssetLinks);

        var domainAssetLinks = request.AssetLinks
            .Select(l => new AssetLink { Name = l.Name, Value = l.Value })
            .ToList();

        var result = await assetIdSearchService
            .SearchShellsByAssetLinkAsync(domainAssetLinks, request.Limit, request.Cursor, cancellationToken)
            .ConfigureAwait(false);

        return result.ToDto();
    }

    public async Task<IList<ISpecificAssetId>> GetSpecificAssetIdByAasIdentifierAsync(
        GetSpecificAssetIdByAasIdentifierRequest request, CancellationToken cancellationToken)
    {
        var decodedId = request.AasIdentifier.DecodeBase64Url(logger);

        var result = await assetIdSearchService
            .GetSpecificAssetIdByAasIdentifierAsync(decodedId, cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    private void ValidateAssetLinks(AssetLinkDto[] assetLinks)
    {
        if (assetLinks.Length == 0)
        {
            logger.LogError("AssetLink array is empty or null.");
            throw new InvalidUserInputException();
        }

        foreach (var link in assetLinks)
        {
            AssetIdHelper.ValidateAssetLinks(link.Name, link.Value, logger, "AssetLink");
        }
    }
}
