using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

using AasCore.Aas3_0;

using Microsoft.AspNetCore.WebUtilities;

namespace AAS.TwinEngine.DataEngine.Api.Shared;

public static class AssetIdHelper
{
    public static IList<SpecificAssetIdFilter> DecodeAssetIds(string[] assetIds, ILogger logger)
    {
        var result = new List<SpecificAssetIdFilter>();

        foreach (var encodedAssetId in assetIds)
        {
            var decodedJson = encodedAssetId.DecodeBase64Url(logger);

            SpecificAssetIdFilter? filter;
            try
            {
                filter = JsonSerializer.Deserialize<SpecificAssetIdFilter>(
                    decodedJson,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to parse SpecificAssetId JSON: {Json}", decodedJson);
                throw new InvalidUserInputException();
            }

            if (filter is null ||
                string.IsNullOrWhiteSpace(filter.Name) ||
                string.IsNullOrWhiteSpace(filter.Value))
            {
                logger.LogError("Invalid SpecificAssetId: name and value are required.");
                throw new InvalidUserInputException();
            }

            if (filter.Name.Length > 64 || filter.Value.Length > 2048)
            {
                logger.LogError("SpecificAssetId name or value exceeds maximum length.");
                throw new InvalidUserInputException();
            }

            result.Add(filter);
        }

        return result;
    }

    public static void FillShellFromMetadata(IAssetAdministrationShell shell, ShellDescriptorMetaData metadata)
    {
        shell.Id = metadata.Id;

        if (!string.IsNullOrWhiteSpace(metadata.IdShort))
        {
            shell.IdShort = metadata.IdShort;
        }

        shell.AssetInformation ??= new AssetInformation(AssetKind.Instance);
        shell.AssetInformation.GlobalAssetId = metadata.GlobalAssetId;

        if (metadata.SpecificAssetIds is not null)
        {
            shell.AssetInformation.SpecificAssetIds = [];
            foreach (var assetId in metadata.SpecificAssetIds)
            {
                shell.AssetInformation.SpecificAssetIds.Add(assetId);
            }
        }
    }
}
