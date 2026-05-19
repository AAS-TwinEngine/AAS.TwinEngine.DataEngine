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
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

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

    public async Task<SemanticTreeNode> TryGetValuesAsync(IReadOnlyList<PluginManifest> pluginManifests, SemanticTreeNode semanticIds, string submodelId, CancellationToken cancellationToken)
    {
        var jsonSchemas = new Dictionary<string, JsonSchema>();

        var dicSemanticTreeNode = multiPluginDataHandler.SplitByPluginManifests(semanticIds, pluginManifests);

        foreach (var (key, value) in dicSemanticTreeNode)
        {
            var jsonSchema = JsonSchemaGenerator.ConvertToJsonSchema(value);
            jsonSchemas.Add(key, jsonSchema);
            jsonSchemaValidator.ValidateRequestSchema(jsonSchema);
        }

        var pluginRequests = pluginRequestBuilder.Build(jsonSchemas);

        var response = await pluginDataProvider.GetDataForSemanticIdsAsync(pluginRequests, submodelId, cancellationToken).ConfigureAwait(false);

        var result = new List<SemanticTreeNode>();

        for (var i = 0; i < response.Count; i++)
        {
            var responseContent = await response[i].ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var schema = jsonSchemas.ElementAt(i).Value;
            jsonSchemaValidator.ValidateResponseContent(responseContent, schema);

            var semanticTreeNode = JsonSchemaParser.ParseJsonSchema(responseContent);
            result.Add(semanticTreeNode);
        }

        var mergedValues = multiPluginDataHandler.Merge(semanticIds, result);

        return mergedValues;
    }

    public async Task<ShellDescriptorsMetaData> GetDataForAllShellDescriptorsAsync(int? limit, string? cursor, IReadOnlyList<PluginManifest> pluginManifests, CancellationToken cancellationToken)
    {
        var availablePlugins = multiPluginDataHandler.GetAvailablePlugins(pluginManifests, c => c.HasShellDescriptor);

        var pluginRequests = pluginRequestBuilder.Build(availablePlugins);

        var response = await pluginDataProvider.GetDataForAllShellDescriptorsAsync(limit, cursor, pluginRequests, cancellationToken).ConfigureAwait(false);

        var result = new ShellDescriptorsMetaData();

        const string Url = $"{ShellsBasePath}";

        for (var i = 0; i < response.Count; i++)
        {
            var responseContent = await response[i].ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var shellDescriptorData = JsonSerializer.Deserialize<ShellDescriptorsMetaData>(responseContent, JsonSerializationOptions.DeserializationOption);
                if (shellDescriptorData == null)
                {
                    logger.LogError("Failed to deserialize All ShellDescriptorData. Response content: {Content}", responseContent);
                    throw new ResponseParsingException();
                }

                var shellDescriptors = shellDescriptorData.ShellDescriptors ?? [];

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

    public async Task<ShellDescriptorMetaData> GetDataForShellDescriptorAsync(IReadOnlyList<PluginManifest> pluginManifests, string id, CancellationToken cancellationToken)
    {
        var availablePlugins = multiPluginDataHandler.GetAvailablePlugins(pluginManifests, c => c.HasShellDescriptor);

        var pluginRequests = pluginRequestBuilder.Build(availablePlugins, id);

        var response = await pluginDataProvider.GetDataForShellDescriptorByIdAsync(pluginRequests, cancellationToken).ConfigureAwait(false);

        var url = $"{ShellsBasePath}/{id.EncodeBase64Url()}";

        for (var i = 0; i < response.Count; i++)
        {
            var responseContent = await response[i].ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

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
        var availablePlugins = multiPluginDataHandler.GetAvailablePlugins(pluginManifests, c => c.HasAssetInformation);

        var pluginRequests = pluginRequestBuilder.Build(availablePlugins, id);

        var response = await pluginDataProvider.GetDataForAssetInformationByIdAsync(pluginRequests, cancellationToken).ConfigureAwait(false);

        var url = $"assets/{id.EncodeBase64Url()}";

        for (var i = 0; i < response.Count; i++)
        {
            var responseContent = await response[i].ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

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
