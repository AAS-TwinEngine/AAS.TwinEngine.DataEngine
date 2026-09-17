using System.Collections.Concurrent;
using System.Text;
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

    private readonly Uri _baseUrl = generalConfig.Value.DataEngineRepositoryBaseUrl ?? throw new InvalidDependencyException(nameof(generalConfig.Value.DataEngineRepositoryBaseUrl), logger);
    private readonly ConcurrentDictionary<string, Lazy<PreparedSchema>> _schemaCache = new(StringComparer.Ordinal);

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

    public async Task<IReadOnlyDictionary<string, SemanticTreeNode>> TryGetValuesBatchAsync(
        IReadOnlyList<PluginManifest> pluginManifests,
        IReadOnlyList<SubmodelValueRequest> requests,
        int batchSize,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return new Dictionary<string, SemanticTreeNode>();
        }

        List<PreparedBatchRequest> preparedRequests;
        using (var prepareActivity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.PrepareBatchSchemas))
        {
            _ = prepareActivity?.SetTag("value.request.count", requests.Count);
            preparedRequests = requests.Select(request => PrepareBatchRequest(request, pluginManifests)).ToList();
        }

        var pluginNames = preparedRequests.Select(request => request.PluginName).Distinct(StringComparer.Ordinal).ToList();
        if (pluginNames.Count != 1)
        {
            throw new MultiPluginConflictException();
        }

        var batches = preparedRequests.OrderBy(request => request.SchemaKey, StringComparer.Ordinal).Chunk(batchSize).ToList();

        var responseItems = new ConcurrentBag<SubmodelDataBatchResponse>();
        await Parallel.ForEachAsync(
            batches,
            new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency, CancellationToken = cancellationToken },
            async (batch, token) =>
            {
                IReadOnlyList<SubmodelDataBatchRequestGroup> groups = batch
                    .GroupBy(item => item.SchemaKey, StringComparer.Ordinal)
                    .Select(group => new SubmodelDataBatchRequestGroup(
                        group.Select(item => item.Request.SubmodelId.EncodeBase64Url(logger)).ToList(),
                        group.First().Schema)).ToList();

                var pluginRequest = pluginRequestBuilder.Build(pluginNames[0], groups);
                using var requestContent = pluginRequest.Content;
                var responseContent = await pluginDataProvider.GetDataForSubmodelsBatchAsync(pluginRequest, token).ConfigureAwait(false);

                var batchResponses = DeserializeBatchResponse(responseContent);

                foreach (var response in batchResponses)
                {
                    responseItems.Add(response);
                }
            }).ConfigureAwait(false);

        var preparedById = preparedRequests.ToDictionary(item => item.Request.SubmodelId, StringComparer.Ordinal);
        var valuesById = new ConcurrentDictionary<string, SemanticTreeNode>(StringComparer.Ordinal);

        using var processActivity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.ProcessBatchResponses);
        _ = processActivity?.SetTag("response.count", responseItems.Count);
        await Parallel.ForEachAsync(
            responseItems,
            new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency, CancellationToken = cancellationToken },
            (response, _) =>
            {
                var prepared = preparedById[response.SubmodelId];
                var responseContent = response.Result.GetRawText();
                jsonSchemaValidator.ValidateResponseContent(responseContent, prepared.Schema);

                var parsedValues = JsonSchemaParser.ParseJsonSchema(responseContent);
                valuesById[response.SubmodelId] = multiPluginDataHandler.Merge(prepared.Request.SemanticIds, new[] { parsedValues });

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

        return valuesById;
    }

    private PreparedBatchRequest PrepareBatchRequest(SubmodelValueRequest request, IReadOnlyList<PluginManifest> pluginManifests)
    {
        var splitValues = multiPluginDataHandler.SplitByPluginManifests(request.SemanticIds, pluginManifests);
        if (splitValues.Count != 1)
        {
            throw new MultiPluginConflictException();
        }

        var pluginValues = splitValues.Single();
        var semanticTreeKey = BuildSemanticTreeKey(pluginValues.Value);
        var preparedSchema = _schemaCache.GetOrAdd(semanticTreeKey, _ => new Lazy<PreparedSchema>(() => CreatePreparedSchema(pluginValues.Value), LazyThreadSafetyMode.ExecutionAndPublication)).Value;

        return new PreparedBatchRequest(request, pluginValues.Key, preparedSchema.Schema, preparedSchema.SchemaKey);
    }

    private PreparedSchema CreatePreparedSchema(SemanticTreeNode semanticTree)
    {
        var schema = JsonSchemaGenerator.ConvertToJsonSchema(semanticTree);
        jsonSchemaValidator.ValidateRequestSchema(schema);
        return new PreparedSchema(schema, JsonSerializer.Serialize(schema, JsonSerializationOptions.FileAndHttpContent));
    }

    private static string BuildSemanticTreeKey(SemanticTreeNode node)
    {
        var builder = new StringBuilder();
        AppendSemanticTreeKey(builder, node);
        return builder.ToString();
    }

    private static void AppendSemanticTreeKey(StringBuilder builder, SemanticTreeNode node)
    {
        _ = builder.Append(node.GetType().Name).Append('|').Append(node.SemanticId).Append('|').Append(node.Cardinality);

        if (node is SemanticLeafNode leaf)
        {
            _ = builder.Append('|').Append(leaf.DataType);
        }

        if (node is SemanticBranchNode branch)
        {
            foreach (var child in branch.Children)
            {
                _ = builder.Append('[');
                AppendSemanticTreeKey(builder, child);
                _ = builder.Append(']');
            }
        }
    }

    private static IReadOnlyList<SubmodelDataBatchResponse> DeserializeBatchResponse(string responseContent)
    {
        try
        {
            return JsonSerializer.Deserialize<List<SubmodelDataBatchResponse>>(
                responseContent,
                JsonSerializationOptions.DeserializationOption) ?? throw new ResponseParsingException();
        }
        catch (JsonException)
        {
            throw new ResponseParsingException();
        }
    }

    private sealed record PreparedBatchRequest(SubmodelValueRequest Request, string PluginName, JsonSchema Schema, string SchemaKey);

    private sealed record PreparedSchema(JsonSchema Schema, string SchemaKey);

    public async Task<ShellDescriptorsMetaData> GetDataForAllShellDescriptorsAsync(int limit, string? cursor, AssetKind? assetKind, string? assetType, IReadOnlyList<PluginManifest> pluginManifests, CancellationToken cancellationToken)
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

    private void ValidateAssetKindTypeFilterResponse(IList<ShellDescriptorMetaData> shellDescriptors, AssetKind? assetKind, string? encodedAssetType)
    {
        var requestedAssetType = encodedAssetType;

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

    public async Task<ShellDescriptorsMetaData> GetDataForShellsByAssetIdsAsync(IReadOnlyList<PluginManifest> pluginManifests, ShellSearchFilter? filter, int limit, string? cursor, CancellationToken cancellationToken)
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

        var responses = await pluginDataProvider.GetDataForShellDescriptorsByAssetIdsAsync(pluginRequests, assetIdsHeaderValue, filter?.IdShort, limit, cursor, cancellationToken).ConfigureAwait(false);

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
