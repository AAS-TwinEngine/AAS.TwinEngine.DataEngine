using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.Api.Discovery.MappingProfiles;
using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.Api.Discovery.Responses;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Discovery;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

namespace AAS.TwinEngine.DataEngine.Api.Discovery.Handler;

public class DiscoveryHandler(ILogger<DiscoveryHandler> logger, IAssetIdSearchService assetIdSearchService) : IDiscoveryHandler
{
    private static readonly Regex ValidAssetLinkPattern = new(@"^[\u0009\u000A\u000D\u0020-\uD7FF\uE000-\uFFFD]*$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public async Task<ShellsByAssetLinkResponseDto> SearchShellsByAssetLinkAsync(
        AssetLinkDto[] assetLinks, int? limit, string? cursor, CancellationToken cancellationToken)
    {
        limit.ValidateLimit(logger);
        cursor?.ValidateCursor(logger);

        ValidateAssetLinks(assetLinks);

        var domainAssetLinks = assetLinks
            .Select(l => new AssetLink { Name = l.Name, Value = l.Value })
            .ToList();

        var result = await assetIdSearchService
            .SearchShellsByAssetLinkAsync(domainAssetLinks, limit, cursor, cancellationToken)
            .ConfigureAwait(false);

        return result.ToDto();
    }

    private void ValidateAssetLinks(AssetLinkDto[] assetLinks)
    {
        if (assetLinks is null || assetLinks.Length == 0)
        {
            logger.LogError("AssetLink array is empty or null.");
            throw new InvalidUserInputException();
        }

        foreach (var link in assetLinks)
        {
            if (string.IsNullOrWhiteSpace(link.Name) ||
                string.IsNullOrWhiteSpace(link.Value))
            {
                logger.LogError("AssetLink name and value are required.");
                throw new InvalidUserInputException();
            }

            if (link.Name.Length > 64)
            {
                logger.LogError("AssetLink name exceeds maximum length of 64 characters.");
                throw new InvalidUserInputException();
            }

            if (link.Value.Length > 2048)
            {
                logger.LogError("AssetLink value exceeds maximum length of 2048 characters.");
                throw new InvalidUserInputException();
            }

            if (!ValidAssetLinkPattern.IsMatch(link.Name))
            {
                logger.LogError("AssetLink name contains invalid characters.");
                throw new InvalidUserInputException();
            }

            if (!ValidAssetLinkPattern.IsMatch(link.Value))
            {
                logger.LogError("AssetLink value contains invalid characters.");
                throw new InvalidUserInputException();
            }
        }
    }
}
