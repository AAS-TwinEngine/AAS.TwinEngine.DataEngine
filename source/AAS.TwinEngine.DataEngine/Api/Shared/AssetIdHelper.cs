using System.Text.Json;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;

namespace AAS.TwinEngine.DataEngine.Api.Shared;

public static class AssetIdHelper
{
    private static readonly Regex ValidAssetLinkPattern = new(@"^[\u0009\u000A\u000D\u0020-\uD7FF\uE000-\uFFFD]*$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private const int MaxNameLength = 64;
    private const int MaxValueLength = 2048;

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

            ValidateAssetLinks(filter.Name, filter.Value, logger, "SpecificAssetId");

            result.Add(filter);
        }

        return result;
    }

    public static void ValidateAssetLinks(string name, string value, ILogger logger, string entityName)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
        {
            logger.LogError("{EntityName} name and value are required.", entityName);
            throw new InvalidUserInputException();
        }

        if (name.Length > MaxNameLength)
        {
            logger.LogError("{EntityName} name exceeds maximum length of 64 characters.", entityName);
            throw new InvalidUserInputException();
        }

        if (value.Length > MaxValueLength)
        {
            logger.LogError("{EntityName} value exceeds maximum length of 2048 characters.", entityName);
            throw new InvalidUserInputException();
        }

        if (!ValidAssetLinkPattern.IsMatch(name))
        {
            logger.LogError("{EntityName} name contains invalid characters.", entityName);
            throw new InvalidUserInputException();
        }

        if (!ValidAssetLinkPattern.IsMatch(value))
        {
            logger.LogError("{EntityName} value contains invalid characters.", entityName);
            throw new InvalidUserInputException();
        }
    }
}
