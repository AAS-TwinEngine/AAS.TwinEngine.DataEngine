using System.Text.Json;

using AAS.TwinEngine.Plugin.TestPlugin.Api.Shared;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Exceptions;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Extensions;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.TestPlugin.Api.MetaData.Services;

public class AssetIdsFilterHeaderValidation(ILogger<AssetIdsFilterHeaderValidation> logger) : IAssetIdsFilterHeaderValidation
{
    public const int NameMaxLength = 64;
    private const int MaxIdentifiers = 10;
    public const int ValueMaxLength = 2000;

    public AssetIdFilterHeader? ParseToDomainModel(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        if (!TryParseHeader(headerValue, out var filter, out var parseError))
        {
            logger.LogError("Invalid asset id filter header provided: {ParseError}", parseError);
            throw new BadRequestException($"Invalid aastwinengine-assetids header: {parseError}");
        }

        return filter;
    }

    private bool TryParseHeader(string headerValue, out AssetIdFilterHeader? filter, out string? error)
    {
        filter = null;
        error = null;

        try
        {
            using var jsonDocument = JsonDocument.Parse(headerValue);
            var root = jsonDocument.RootElement;

            if (!IsValidRootArray(root, out error))
            {
                return false;
            }

            var identifiers = new List<SpecificAssetIdsData>();

            foreach (var element in root.EnumerateArray())
            {
                if (!TryParseIdentifierElement(element, out var identifier, out error))
                {
                    return false;
                }

                identifiers.Add(identifier);

                if (ExceedsMaximumIdentifiers(identifiers.Count, out error))
                {
                    return false;
                }
            }

            filter = new AssetIdFilterHeader
            {
                Identifiers = identifiers
            };

            return true;
        }
        catch (JsonException)
        {
            error = "Invalid JSON in header";
            return false;
        }
        catch (Exception)
        {
            error = "Unexpected error parsing header";
            return false;
        }
    }

    private static bool IsValidRootArray(JsonElement root, out string? error)
    {
        error = null;

        if (root.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        error = "Header value must be a JSON array of SpecificAssetId objects";
        return false;
    }

    private bool TryParseIdentifierElement(JsonElement element, out SpecificAssetIdsData identifier, out string? error)
    {
        identifier = null!;
        error = null;

        if (element.ValueKind != JsonValueKind.Object)
        {
            error = "Each element in the array must be a JSON object";
            return false;
        }

        var parsedIdentifier = ParseIdentifier(element, out error);

        if (parsedIdentifier == null)
        {
            return false;
        }

        identifier = parsedIdentifier;
        return true;
    }

    private SpecificAssetIdsData? ParseIdentifier(JsonElement element, out string? error)
    {
        error = null;

        if (!ValidateSupportedProperties(element, out error))
        {
            return null;
        }

        if (!TryGetRequiredProperty(element, "name", out var nameElement, out error))
        {
            return null;
        }

        if (!TryGetRequiredProperty(element, "value", out var valueElement, out error))
        {
            return null;
        }

        var name = nameElement.ToString();
        var value = valueElement.ToString();

        if (!ValidateIdentifierField(name, "name", NameMaxLength, out error))
        {
            return null;
        }

        if (!ValidateIdentifierField(value, "value", ValueMaxLength, out error))
        {
            return null;
        }

        return new SpecificAssetIdsData
        {
            Name = name,
            Value = value
        };
    }

    private static bool ValidateSupportedProperties(JsonElement element, out string? error)
    {
        error = null;

        var unsupportedProperty = element.EnumerateObject()
            .FirstOrDefault(property =>
                !string.Equals(property.Name, "name", StringComparison.Ordinal) &&
                !string.Equals(property.Name, "value", StringComparison.Ordinal));

        if (unsupportedProperty.Value.ValueKind == JsonValueKind.Undefined)
        {
            return true;
        }

        error = $"Unsupported property '{unsupportedProperty.Name}'. Only 'name' and 'value' are allowed";
        return false;
    }

    private static bool TryGetRequiredProperty(JsonElement element, string propertyName, out JsonElement propertyValue, out string? error)
    {
        error = null;

        if (element.TryGetProperty(propertyName, out propertyValue))
        {
            return true;
        }

        error = $"SpecificAssetId must have a '{propertyName}' property";
        return false;
    }

    private bool ValidateIdentifierField(string input, string fieldName, int maxLength, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = $"SpecificAssetId '{fieldName}' must not be empty";
            return false;
        }

        if (input.Length > maxLength)
        {
            error = $"SpecificAssetId '{fieldName}' must not exceed {maxLength} characters";
            return false;
        }

        if (ContainsInvalidXmlCharacters(input) ||
            ContainsNullByte(input))
        {
            error = $"SpecificAssetId '{fieldName}' contains invalid characters";
            return false;
        }

        if (!input.IsValidIdentifier(logger))
        {
            error = $"SpecificAssetId '{fieldName}' contains potentially malicious patterns";
            return false;
        }

        return true;
    }

    private static bool ExceedsMaximumIdentifiers(int identifierCount, out string? error)
    {
        error = null;

        if (identifierCount <= MaxIdentifiers)
        {
            return false;
        }

        error = $"A maximum of {MaxIdentifiers} asset identifiers are allowed";
        return true;
    }

    private static bool ContainsInvalidXmlCharacters(string input)
    {
        if (input == null)
        {
            return true;
        }

        return !InputValidationPatterns.XmlCharacterPattern().IsMatch(input);
    }

    private static bool ContainsNullByte(string input)
    {
        if (input == null)
        {
            return true;
        }

        return input.Contains('\0', StringComparison.Ordinal) || input.Contains("%00", StringComparison.OrdinalIgnoreCase);
    }
}
