using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Json.Schema;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginDataHandler(
    IPluginRequestBuilder pluginRequestBuilder,
    IPluginDataProvider pluginDataProvider,
    IJsonSchemaValidator jsonSchemaValidator,
    IMultiPluginDataHandler multiPluginDataHandler,
    ILogger<PluginDataHandler> logger,
    IOptions<GeneralConfig> generalConfig) : IPluginDataHandler
{
    private const string ShellsBasePath = "shells";
    private const int DefaultFallbackPluginPageSize = 100;
    private const int FallbackPluginPageSizeMultiplier = 10;
    private const int MaxFallbackPluginPageSize = 10_000;

    private readonly Uri _baseUrl = generalConfig.Value.DataEngineRepositoryBaseUrl ?? throw new InvalidDependencyException(nameof(generalConfig.Value.DataEngineRepositoryBaseUrl), logger);

    public (IReadOnlyList<PluginManifest> FallbackOnlyPlugins, IReadOnlyList<PluginManifest> FilterCapablePlugins)
        PartitionShellDescriptorPluginsByFilterCapability(IReadOnlyList<PluginManifest> pluginManifests)
    {
        var shellDescriptorPlugins = pluginManifests
            .Where(m => m.Capabilities.HasShellDescriptor)
            .ToList();

        var fallbackOnlyPlugins = shellDescriptorPlugins
            .Where(m => m.Capabilities.HasAssetKindTypeFilter != true)
            .ToList();

        var filterCapablePlugins = shellDescriptorPlugins
            .Where(m => m.Capabilities.HasAssetKindTypeFilter == true)
            .ToList();

        return (fallbackOnlyPlugins, filterCapablePlugins);
    }

    public async Task<ShellDescriptorsMetaData> GetDataForAllShellDescriptorsWithAssetFilterSupportAsync(
        int? limit,
        string? cursor,
        AssetKind? assetKind,
        string? assetType,
        IReadOnlyList<PluginManifest> pluginManifests,
        CancellationToken cancellationToken)
    {
        if (!RequiresAssetKindTypeFilter(assetKind, assetType))
        {
            return await GetDataForAllShellDescriptorsAsync(limit, cursor, assetKind, assetType, pluginManifests, cancellationToken).ConfigureAwait(false);
        }

        var pluginPartitions = PartitionShellDescriptorPluginsByFilterCapability(pluginManifests);
        var fallbackOnlyPlugins = pluginPartitions.FallbackOnlyPlugins ?? [];
        var filterCapablePlugins = pluginPartitions.FilterCapablePlugins ?? [];

        if (filterCapablePlugins.Count == 0)
        {
            return await GetDataForAllShellDescriptorsWithClientSideAssetFilterAsync(limit, cursor, assetKind, assetType, fallbackOnlyPlugins, cancellationToken).ConfigureAwait(false);
        }

        if (fallbackOnlyPlugins.Count > 0)
        {
            return await GetDataForAllShellDescriptorsWithMixedPluginCapabilitiesAsync(
                limit,
                cursor,
                assetKind,
                assetType,
                fallbackOnlyPlugins,
                filterCapablePlugins,
                cancellationToken).ConfigureAwait(false);
        }

        return await GetDataForAllShellDescriptorsAsync(limit, cursor, assetKind, assetType, filterCapablePlugins, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SemanticTreeNode> TryGetValuesAsync(IReadOnlyList<PluginManifest> pluginManifests, SemanticTreeNode semanticIds, string submodelId, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetPluginData, DataEngineTracing.Attributes.SubmodelId, submodelId);

        var jsonSchemas = new Dictionary<string, JsonSchema>();

        var dicSemanticTreeNode = multiPluginDataHandler.SplitByPluginManifests(semanticIds, pluginManifests);

        foreach (var (key, value) in dicSemanticTreeNode)
        {
            var jsonSchema = JsonSchemaGenerator.ConvertToJsonSchema(value);
            jsonSchemas.Add(key, jsonSchema);
            jsonSchemaValidator.ValidateRequestSchema(jsonSchema);
        }

        var pluginRequests = pluginRequestBuilder.Build(jsonSchemas);

        var responses = await pluginDataProvider.GetDataForSemanticIdsAsync(pluginRequests, submodelId, cancellationToken).ConfigureAwait(false);

        var result = new List<SemanticTreeNode>();

        for (var i = 0; i < responses.Count; i++)
        {
            var responseContent = responses[i];

            var schema = jsonSchemas.ElementAt(i).Value;
            jsonSchemaValidator.ValidateResponseContent(responseContent, schema);

            var semanticTreeNode = JsonSchemaParser.ParseJsonSchema(responseContent);
            result.Add(semanticTreeNode);
        }

        var mergedValues = multiPluginDataHandler.Merge(semanticIds, result);

        return mergedValues;
    }

    public async Task<ShellDescriptorsMetaData> GetDataForAllShellDescriptorsAsync(int? limit, string? cursor, AssetKind? assetKind, string? assetType, IReadOnlyList<PluginManifest> pluginManifests, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetPluginMetadataShells);

        var requiresAssetKindTypeFilter = assetKind.HasValue || !string.IsNullOrWhiteSpace(assetType);
        var availablePlugins = multiPluginDataHandler.GetAvailablePlugins(pluginManifests, c => c.HasShellDescriptor && (!requiresAssetKindTypeFilter || c.HasAssetKindTypeFilter == true));
        var usingFilterCapablePlugins = requiresAssetKindTypeFilter && availablePlugins.Count > 0;

        if (requiresAssetKindTypeFilter && availablePlugins.Count == 0)
        {
            logger.LogWarning("No plugins available that support asset kind/type filtering. Falling back to plugins with shell descriptor capability.");
            availablePlugins = multiPluginDataHandler.GetAvailablePlugins(pluginManifests, c => c.HasShellDescriptor);
        }

        var pluginRequests = pluginRequestBuilder.Build(availablePlugins);

        var responses = await pluginDataProvider.GetDataForAllShellDescriptorsAsync(limit, cursor, assetKind, assetType, pluginRequests, cancellationToken).ConfigureAwait(false);

        var result = new ShellDescriptorsMetaData();

        const string Url = $"{ShellsBasePath}";

        foreach (var responseContent in responses)
        {
            try
            {
                var shellDescriptorData = JsonSerializer.Deserialize<ShellDescriptorsMetaData>(responseContent, JsonSerializationOptions.DeserializationOption);
                if (shellDescriptorData == null)
                {
                    logger.LogError("Failed to deserialize All ShellDescriptorData. Response content: {Content}", responseContent);
                    throw new ResponseParsingException();
                }

                var shellDescriptors = shellDescriptorData.ShellDescriptors ?? [];

                if (usingFilterCapablePlugins)
                {
                    ValidateAssetKindTypeFilterResponse(shellDescriptors, assetKind, assetType);
                }

                var invalidDescriptors = shellDescriptors
                                         .Where(x => string.IsNullOrWhiteSpace(x.Id))
                                         .Select(x => new
                                         {
                                             IdShort = x.IdShort ?? "<null>",
                                             GlobalAssetId = x.GlobalAssetId ?? "<null>"
                                         })
                                         .ToList();

                if (invalidDescriptors.Count > 0)
                {
                    logger.LogError("Invalid shell descriptor metadata response. {InvalidCount} descriptor(s) contain null or empty id. Invalid descriptors (IdShort/GlobalAssetId): {@InvalidDescriptors}", invalidDescriptors.Count, invalidDescriptors);
                    throw new ValidationFailedException();
                }

                SetHref(shellDescriptors);

                result.PagingMetaData = shellDescriptorData.PagingMetaData;

                result.ShellDescriptors?.AddRange(shellDescriptors);
            }
            catch (JsonException)
            {
                logger.LogError("Invalid response format. Endpoint: {Url}", Url);
                throw new ResponseParsingException();
            }
        }

        return result;
    }

    private async Task<ShellDescriptorsMetaData> GetDataForAllShellDescriptorsWithMixedPluginCapabilitiesAsync(
        int? limit,
        string? cursor,
        AssetKind? assetKind,
        string? assetType,
        IReadOnlyList<PluginManifest> fallbackOnlyPlugins,
        IReadOnlyList<PluginManifest> filterCapablePlugins,
        CancellationToken cancellationToken)
    {
        var fallbackResult = await GetDataForAllShellDescriptorsWithClientSideAssetFilterAsync(
            limit,
            cursor,
            assetKind,
            assetType,
            fallbackOnlyPlugins,
            cancellationToken).ConfigureAwait(false);

        var collectedMetadata = fallbackResult.ShellDescriptors?.ToList() ?? [];

        if (limit.HasValue && collectedMetadata.Count >= limit.Value)
        {
            fallbackResult.ShellDescriptors = [.. collectedMetadata.Take(limit.Value)];
            return fallbackResult;
        }

        var remainingLimit = limit.HasValue
            ? Math.Max(limit.Value - collectedMetadata.Count, 0)
            : (int?)null;

        if (remainingLimit == 0)
        {
            fallbackResult.ShellDescriptors = collectedMetadata;
            return fallbackResult;
        }

        // Strict staged-cursor rule:
        // - If fallback plugins contributed to this page, capable plugins must start from their own first page (cursor null).
        // - If fallback contributed nothing, preserve incoming cursor for capable plugins.
        var capableCursor = collectedMetadata.Count > 0 ? null : cursor;

        var capableMetadata = await GetDataForAllShellDescriptorsAsync(
            remainingLimit,
            capableCursor,
            assetKind,
            assetType,
            filterCapablePlugins,
            cancellationToken).ConfigureAwait(false);

        var decodedAssetType = DecodeAssetTypeIfPresent(assetType);
        var filteredCapable = capableMetadata.ShellDescriptors?
            .Where(descriptor => MatchesAssetKindTypeFilter(descriptor, assetKind, decodedAssetType))
            .ToList() ?? [];

        collectedMetadata.AddRange(filteredCapable);

        return new ShellDescriptorsMetaData
        {
            PagingMetaData = capableMetadata.PagingMetaData ?? fallbackResult.PagingMetaData,
            ShellDescriptors = limit.HasValue ? [.. collectedMetadata.Take(limit.Value)] : collectedMetadata
        };
    }

    private async Task<ShellDescriptorsMetaData> GetDataForAllShellDescriptorsWithClientSideAssetFilterAsync(
        int? limit,
        string? cursor,
        AssetKind? assetKind,
        string? assetType,
        IReadOnlyList<PluginManifest> pluginManifests,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Falling back to client-side asset kind/type filtering for shell descriptor metadata.");

        var decodedAssetType = DecodeAssetTypeIfPresent(assetType);
        var collectedMetadata = new List<ShellDescriptorMetaData>();
        var pluginLimit = limit is > 0 ? limit.Value : DefaultFallbackPluginPageSize;
        var pluginCursor = cursor;
        var pagingMetaData = new PagingMetaData();

        while (true)
        {
            var metadata = await GetDataForAllShellDescriptorsAsync(pluginLimit, pluginCursor, null, null, pluginManifests, cancellationToken).ConfigureAwait(false);

            foreach (var descriptor in (metadata.ShellDescriptors ?? []).Where(descriptor => MatchesAssetKindTypeFilter(descriptor, assetKind, decodedAssetType)))
            {
                collectedMetadata.Add(descriptor);

                if (limit.HasValue && collectedMetadata.Count >= limit.Value)
                {
                    break;
                }
            }

            pagingMetaData = metadata.PagingMetaData ?? new PagingMetaData();

            if (limit.HasValue && collectedMetadata.Count >= limit.Value)
            {
                break;
            }

            pluginCursor = metadata.PagingMetaData?.Cursor;
            if (string.IsNullOrWhiteSpace(pluginCursor))
            {
                break;
            }

            pluginLimit = Math.Min(pluginLimit * FallbackPluginPageSizeMultiplier, MaxFallbackPluginPageSize);
        }

        return new ShellDescriptorsMetaData
        {
            PagingMetaData = pagingMetaData,
            ShellDescriptors = limit.HasValue ? [.. collectedMetadata.Take(limit.Value)] : collectedMetadata
        };
    }

    private bool RequiresAssetKindTypeFilter(AssetKind? assetKind, string? assetType)
        => assetKind.HasValue || !string.IsNullOrWhiteSpace(assetType);

    private string? DecodeAssetTypeIfPresent(string? assetType)
        => string.IsNullOrWhiteSpace(assetType) ? null : assetType.DecodeBase64Url(logger);

    private static bool MatchesAssetKindTypeFilter(ShellDescriptorMetaData descriptor, AssetKind? assetKind, string? decodedAssetType)
    {
        if (assetKind.HasValue && descriptor.ParsedAssetKind != assetKind.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(decodedAssetType)
            && !string.Equals(descriptor.AssetType, decodedAssetType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private void ValidateAssetKindTypeFilterResponse(IList<ShellDescriptorMetaData> shellDescriptors, AssetKind? assetKind, string? encodedAssetType)
    {
        var requestedAssetType = string.IsNullOrWhiteSpace(encodedAssetType)
            ? null
            : encodedAssetType.DecodeBase64Url(logger);

        foreach (var descriptor in shellDescriptors)
        {
            if (assetKind.HasValue && !string.Equals(descriptor.AssetKind, assetKind.Value.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError("Plugin returned mismatched assetKind. Requested: {RequestedAssetKind}, Actual: {ActualAssetKind}, DescriptorId: {DescriptorId}",
                    assetKind.Value,
                    descriptor.AssetKind,
                    descriptor.Id);
                throw new ValidationFailedException();
            }

            if (!string.IsNullOrWhiteSpace(requestedAssetType)
                && !string.Equals(descriptor.AssetType, requestedAssetType, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError("Plugin returned mismatched assetType. Requested: {RequestedAssetType}, Actual: {ActualAssetType}, DescriptorId: {DescriptorId}",
                    requestedAssetType,
                    descriptor.AssetType,
                    descriptor.Id);
                throw new ValidationFailedException();
            }
        }
    }

    public async Task<ShellDescriptorMetaData> GetDataForShellDescriptorAsync(IReadOnlyList<PluginManifest> pluginManifests, string id, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetPluginMetadataShells, DataEngineTracing.Attributes.ShellId, id);

        var availablePlugins = multiPluginDataHandler.GetAvailablePlugins(pluginManifests, c => c.HasShellDescriptor);

        var pluginRequests = pluginRequestBuilder.Build(availablePlugins, id);

        var responses = await pluginDataProvider.GetDataForShellDescriptorByIdAsync(pluginRequests, cancellationToken).ConfigureAwait(false);

        var url = $"{ShellsBasePath}/{id.EncodeBase64Url()}";

        foreach (var responseContent in responses)
        {
            try
            {
                var shellDescriptorData = JsonSerializer.Deserialize<ShellDescriptorMetaData>(responseContent, JsonSerializationOptions.DeserializationOption);
                if (shellDescriptorData != null)
                {
                    if (string.IsNullOrWhiteSpace(shellDescriptorData.Id))
                    {
                        logger.LogError("Invalid shell descriptor metadata response for requested id {RequestedId}. Descriptor id is null or empty in response.", id);
                        throw new ValidationFailedException();
                    }

                    SetHref(shellDescriptorData);
                    return shellDescriptorData;
                }
            }
            catch (JsonException)
            {
                logger.LogError("Invalid response format. Endpoint: {Url}", url);
                throw new ResponseParsingException();
            }
        }

        logger.LogError("Failed to deserialize ShellDescriptorData.");
        throw new ResponseParsingException();
    }

    public async Task<AssetData> GetDataForAssetInformationByIdAsync(IReadOnlyList<PluginManifest> pluginManifests, string id, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetPluginMetadataAssets, DataEngineTracing.Attributes.ShellId, id);

        var availablePlugins = multiPluginDataHandler.GetAvailablePlugins(pluginManifests, c => c.HasAssetInformation);

        var pluginRequests = pluginRequestBuilder.Build(availablePlugins, id);

        var responses = await pluginDataProvider.GetDataForAssetInformationByIdAsync(pluginRequests, cancellationToken).ConfigureAwait(false);

        var url = $"assets/{id.EncodeBase64Url()}";

        foreach (var responseContent in responses)
        {
            try
            {
                var assetData = JsonSerializer.Deserialize<AssetData>(responseContent);
                if (assetData != null)
                {
                    return assetData;
                }
            }
            catch (JsonException)
            {
                logger.LogError("Invalid response format. Endpoint: {Url}", url);
                throw new ResponseParsingException();
            }
        }

        logger.LogError("Failed to deserialize AssetInformationData.");
        throw new ResponseParsingException();
    }

    public async Task<ShellDescriptorsMetaData> GetDataForShellsByAssetIdsAsync(IReadOnlyList<PluginManifest> pluginManifests, ShellSearchFilter? filter, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetPluginMetadataShells);

        var availablePlugins = multiPluginDataHandler.GetAvailablePlugins(pluginManifests, c => c.HasAssetIdSearch == true);

        if (availablePlugins.Count == 0)
        {
            logger.LogWarning("No plugins available that support asset ID search.");
            throw new PluginCapabilityNotSupportedException();
        }

        var pluginRequests = pluginRequestBuilder.Build(availablePlugins);

        var assetIdsHeaderValue = filter?.SpecificAssetIds is not null && filter.SpecificAssetIds.Count > 0
            ? JsonSerializer.Serialize(
                                       filter.SpecificAssetIds.Select(x => new
                                       {
                                           name = x.Name,
                                           value = x.Value
                                       }))
            : null;

        var responses = await pluginDataProvider.GetDataForShellDescriptorsByAssetIdsAsync(pluginRequests, assetIdsHeaderValue, filter?.IdShort, cancellationToken).ConfigureAwait(false);

        var result = new ShellDescriptorsMetaData();

        foreach (var responseContent in responses)
        {
            try
            {
                var shellDescriptorData = JsonSerializer.Deserialize<ShellDescriptorsMetaData>(responseContent, JsonSerializationOptions.DeserializationOption);
                if (shellDescriptorData == null)
                {
                    logger.LogError("Failed to deserialize ShellDescriptorData from asset ID search. Response content: {Content}", responseContent);
                    throw new ResponseParsingException();
                }

                var shellDescriptors = shellDescriptorData.ShellDescriptors ?? [];
                SetHref(shellDescriptors);
                result.PagingMetaData = shellDescriptorData.PagingMetaData;
                result.ShellDescriptors?.AddRange(shellDescriptors);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Invalid response format from asset ID search.");
                throw new ResponseParsingException();
            }
        }

        return result;
    }

    private void SetHref(IList<ShellDescriptorMetaData> values)
    {
        foreach (var value in values)
        {
            SetHref(value);
        }
    }

    private void SetHref(ShellDescriptorMetaData value)
    {
        var encodedId = value.Id.EncodeBase64Url();
        value.Href = $"{_baseUrl}{ShellsBasePath}/{encodedId}";
    }
}
