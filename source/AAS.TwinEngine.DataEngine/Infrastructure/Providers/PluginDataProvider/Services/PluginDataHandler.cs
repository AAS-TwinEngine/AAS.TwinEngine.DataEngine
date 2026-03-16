using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;

using Json.Schema;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginDataHandler(
    IPluginRequestBuilder pluginRequestBuilder,
    IPluginDataProvider pluginDataProvider,
    IJsonSchemaValidator jsonSchemaValidator,
    IOptions<AasEnvironmentConfig> aasEnvironment,
    IMultiPluginDataHandler multiPluginDataHandler,
    ILogger<PluginDataHandler> logger) : IPluginDataHandler
{
    private readonly Uri _dataEngineRepositoryBaseUrl = aasEnvironment.Value.DataEngineRepositoryBaseUrl ?? throw new ArgumentNullException(nameof(aasEnvironment), "DataEngineRepositoryBaseUrl is required.");
    private const string ShellsBasePath = "shells";

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

                SetHref(shellDescriptorData.ShellDescriptors);

                result.PagingMetaData = shellDescriptorData.PagingMetaData;

                result.ShellDescriptors.AddRange(shellDescriptorData.ShellDescriptors);
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
                var shellDescriptorData = JsonSerializer.Deserialize<ShellDescriptorMetaData>(responseContent);
                if (shellDescriptorData != null)
                {
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
        var encodedId = value.Id!.EncodeBase64Url();
        value.Href = $"{_dataEngineRepositoryBaseUrl}{ShellsBasePath}/{encodedId}";
    }
}
