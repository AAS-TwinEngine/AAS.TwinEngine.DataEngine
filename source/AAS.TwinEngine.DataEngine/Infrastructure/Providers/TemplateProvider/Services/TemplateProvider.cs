using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Observability;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients.Caching;
using AAS.TwinEngine.DataEngine.Infrastructure.Logging;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Options;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

public class TemplateProvider(ILogger<TemplateProvider> logger, IOptions<TemplateManagementConfig> options, ICachedGetRequestClient cachedHttp) : ITemplateProvider
{
    private const string SubModelRepositoryPath = ApiPaths.Submodels;
    private const string AasRegistryPath = ApiPaths.ShellDescriptors;
    private const string AasRepositoryPath = ApiPaths.Shells;
    private const string SubmodelRefPath = ApiPaths.SubmodelRefs;
    private const string ConceptDescriptionPath = ApiPaths.ConceptDescriptions;

    private readonly TemplateManagementConfig _config = options.Value;

    public async Task<ISubmodel?> GetFilteredSubmodelTemplateAsync(string templateId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetSubmodelTemplate, DataEngineTracing.Attributes.TemplateId, templateId);

        var encodedTemplateId = templateId.EncodeBase64Url(logger);

        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(queryOptions?.Level))
        {
            queryParams.Add($"level={Uri.EscapeDataString(queryOptions.Level)}");
        }

        if (!string.IsNullOrEmpty(queryOptions?.Extent))
        {
            queryParams.Add($"extent={Uri.EscapeDataString(queryOptions.Extent)}");
        }

        var url = queryParams.Count > 0
            ? $"{SubModelRepositoryPath}/{encodedTemplateId}?{string.Join("&", queryParams)}"
            : $"{SubModelRepositoryPath}/{encodedTemplateId}";

        try
        {
            return await GetSubmodelFromUrlAsync(
                url,
                templateId,
                "Failed to parse or deserialize filtered submodel template JSON. TemplateId: {TemplateId}",
                cancellationToken).ConfigureAwait(false);
        }
        catch (ResourceNotFoundException)
        {
            return null;
        }
    }

    private async Task<ISubmodel> GetSubmodelFromUrlAsync(string url, string templateId, string errorMessage, CancellationToken cancellationToken)
    {
        var content = await SendGetRequestAsync(
            url,
            HttpClientNames.SubmodelTemplateRepository,
            _config.SubmodelTemplateRepository.LocalCacheExpirationInMinutes,
            cancellationToken).ConfigureAwait(false);

        try
        {
            var jsonNode = JsonNode.Parse(content);
            var submodel = Jsonization.Deserialize.SubmodelFrom(jsonNode!);
            UpdateSubmodelTemplateKind(submodel);
            return submodel;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, errorMessage, templateId);
            throw new ResponseParsingException();
        }
    }

    public async Task<ISubmodel?> GetFilteredSubmodelTemplateBySemanticIdAsync(string semanticId, CancellationToken cancellationToken)
    {
        var url = $"{SubModelRepositoryPath}?semanticId={Uri.EscapeDataString(semanticId)}";

        var content = await SendGetRequestAsync(url, HttpClientNames.SubmodelTemplateRepository, _config.SubmodelTemplateRepository.LocalCacheExpirationInMinutes, cancellationToken).ConfigureAwait(false);

        try
        {
            var jsonNode = JsonNode.Parse(content);
            var resultArray = jsonNode?["result"] as JsonArray;
            var submodelNode = resultArray?.FirstOrDefault();
            if (submodelNode is null)
            {
                return null;
            }

            return Jsonization.Deserialize.SubmodelFrom(submodelNode);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse or deserialize submodel templates JSON.");
            throw new ResponseParsingException();
        }
    }

    public async Task<ShellDescriptor> GetShellDescriptorTemplateAsync(string templateId, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetShellDescriptorTemplate, DataEngineTracing.Attributes.TemplateId, templateId);

        var encodedTemplateId = templateId.EncodeBase64Url(logger);
        var url = $"{AasRegistryPath}/{encodedTemplateId}";

        var content = await SendGetRequestAsync(url, HttpClientNames.AasRegistry, _config.AasTemplateRegistry.LocalCacheExpirationInMinutes, cancellationToken).ConfigureAwait(false);

        try
        {
            var jsonNode = JsonNode.Parse(content);
            var descriptorNode = jsonNode?["result"] ?? jsonNode;
            var descriptor = DeserializeShellDescriptor(descriptorNode);

            if (descriptor is not null)
            {
                return descriptor;
            }

            logger.LogError("Failed to deserialize shell descriptor template. TemplateId: {TemplateId}", templateId);
            throw new ResponseParsingException();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse or deserialize shell descriptor template JSON. TemplateId: {TemplateId}", templateId);
            throw new ResponseParsingException();
        }
    }

    public async Task<IAssetAdministrationShell> GetShellTemplateAsync(string templateId, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetShellTemplate, DataEngineTracing.Attributes.TemplateId, templateId);

        var encodedTemplateId = templateId.EncodeBase64Url(logger);
        var url = $"{AasRepositoryPath}/{encodedTemplateId}";

        var content = await SendGetRequestAsync(url, HttpClientNames.AasTemplateRepository, _config.AasTemplateRepository.LocalCacheExpirationInMinutes, cancellationToken).ConfigureAwait(false);

        try
        {
            var jsonNode = JsonNode.Parse(content);
            var shell = Jsonization.Deserialize.AssetAdministrationShellFrom(jsonNode!);
            if (shell != null)
            {
                return shell;
            }

            logger.LogError("Failed to deserialize the shell. AasIdentifier: {AasIdentifier}", templateId);
            throw new ResponseParsingException();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse or deserialize shell JSON. AasIdentifier: {AasIdentifier}", templateId);
            throw new ResponseParsingException();
        }
    }

    public async Task<IAssetInformation> GetAssetInformationTemplateAsync(string templateId, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetPluginMetadataAssets, DataEngineTracing.Attributes.ShellId, templateId);

        var encodedTemplateId = templateId.EncodeBase64Url(logger);
        var url = $"{AasRepositoryPath}/{encodedTemplateId}/asset-information";

        var content = await SendGetRequestAsync(url, HttpClientNames.AasTemplateRepository, _config.AasTemplateRepository.LocalCacheExpirationInMinutes, cancellationToken).ConfigureAwait(false);

        try
        {
            var jsonNode = JsonNode.Parse(content);
            var assetInformation = Jsonization.Deserialize.AssetInformationFrom(jsonNode!);
            if (assetInformation == null)
            {
                logger.LogError("Failed to deserialize the asset-information. AasIdentifier: {AasIdentifier}", templateId);
                throw new ResponseParsingException();
            }

            return assetInformation;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse or deserialize asset-information JSON. AasIdentifier: {AasIdentifier}", templateId);
            throw new ResponseParsingException();
        }
    }

    public async Task<List<IReference>> GetSubmodelRefByIdAsync(string templateId, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetSubmodelRefTemplate, DataEngineTracing.Attributes.TemplateId, templateId);

        var encodedTemplateId = templateId.EncodeBase64Url(logger);
        var url = $"{AasRepositoryPath}/{encodedTemplateId}/{SubmodelRefPath}";

        var content = await SendGetRequestAsync(url, HttpClientNames.AasTemplateRepository, _config.AasTemplateRepository.LocalCacheExpirationInMinutes, cancellationToken).ConfigureAwait(false);

        try
        {
            using var document = JsonDocument.Parse(content);

            if (!document.RootElement.TryGetProperty("result", out var resultElement))
            {
                logger.LogWarning("submodel-ref JSON does not contain a 'result' property.");
                throw new ResourceNotFoundException();
            }

            if (resultElement.ValueKind != JsonValueKind.Array || resultElement.GetArrayLength() == 0)
            {
                logger.LogWarning("submodel-ref 'result' is not a non-empty array.");
                throw new ResourceNotFoundException();
            }

            var references = resultElement.EnumerateArray()
                                          .Select(item => JsonNode.Parse(item.GetRawText()))
                                          .Select(Jsonization.Deserialize.ReferenceFrom!)
                                          .Cast<IReference>().ToList();

            if (references.Count == 0)
            {
                logger.LogError("No valid submodel-refs could be deserialized. AasIdentifier: {AasIdentifier}", templateId);
                throw new ResponseParsingException();
            }

            return references;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse or deserialize submodel-refs JSON. AasIdentifier: {AasIdentifier}", templateId);
            throw new ResponseParsingException();
        }
    }

    public async Task<IConceptDescription?> GetConceptDescriptionByIdAsync(string cdIdentifier, CancellationToken cancellationToken)
    {
        using var activity = DataEngineTracing.StartSpan(DataEngineTracing.Spans.GetConceptDescription, DataEngineTracing.Attributes.TemplateId, cdIdentifier);

        var encodedCdId = cdIdentifier.EncodeBase64Url(logger);

        var url = $"{ConceptDescriptionPath}/{encodedCdId}";

        try
        {
            var content = await SendGetRequestAsync(url, HttpClientNames.ConceptDescriptorTemplateRepository, _config.ConceptDescriptionTemplateRepository.LocalCacheExpirationInMinutes, cancellationToken).ConfigureAwait(false);
            var jsonNode = JsonNode.Parse(content);
            return Jsonization.Deserialize.ConceptDescriptionFrom(jsonNode!);
        }
        catch (Exception ex)
        {
            // Intentionally catching all exceptions without rethrowing.
            // Failures in fetching concept descriptions should not break the serialization process.
            // We log the error for observability and return null to allow the caller to continue gracefully.
            logger.LogError(ex, "Failed to fetch or deserialize concept description. CdIdentifier: {CdIdentifier}", cdIdentifier);
            return null;
        }
    }

    private async Task<string> SendGetRequestAsync(string url, string httpClientName, int expirationTime, CancellationToken cancellationToken) => await cachedHttp.GetStringAsync(url, httpClientName, expirationTime, cancellationToken).ConfigureAwait(false);

    private static ShellDescriptor? DeserializeShellDescriptor(JsonNode? descriptorNode)
    {
        if (descriptorNode is null)
        {
            return null;
        }

        return new ShellDescriptor
        {
            Description = AasJsonNodeDeserializer.DeserializeAasArray(descriptorNode["description"], Jsonization.Deserialize.LangStringTextTypeFrom),
            DisplayName = AasJsonNodeDeserializer.DeserializeAasArray(descriptorNode["displayName"], Jsonization.Deserialize.LangStringNameTypeFrom),
            Extensions = AasJsonNodeDeserializer.DeserializeAasArray(descriptorNode["extensions"], Jsonization.Deserialize.ExtensionFrom),
            Administration = AasJsonNodeDeserializer.DeserializeAasNode(descriptorNode["administration"], Jsonization.Deserialize.AdministrativeInformationFrom),
            AssetKind = AasJsonNodeDeserializer.DeserializeEnum<AssetKind>(descriptorNode["assetKind"]) ?? AssetKind.Type,
            AssetType = descriptorNode["assetType"]?.GetValue<string>(),
            Endpoints = descriptorNode["endpoints"]?.Deserialize<List<EndpointData>>(),
            GlobalAssetId = descriptorNode["globalAssetId"]?.GetValue<string>(),
            IdShort = descriptorNode["idShort"]?.GetValue<string>(),
            Id = descriptorNode["id"]?.GetValue<string>(),
            SpecificAssetIds = AasJsonNodeDeserializer.DeserializeAasArray(descriptorNode["specificAssetIds"], Jsonization.Deserialize.SpecificAssetIdFrom),
            SubmodelDescriptors = DeserializeSubmodelDescriptors(descriptorNode["submodelDescriptors"])
        };
    }

    private static IList<SubmodelDescriptor>? DeserializeSubmodelDescriptors(JsonNode? submodelDescriptorsNode)
    {
        if (submodelDescriptorsNode is not JsonArray submodelDescriptorArray)
        {
            return null;
        }

        var descriptors = new List<SubmodelDescriptor>();
        foreach (var item in submodelDescriptorArray)
        {
            if (item is null)
            {
                continue;
            }

            var descriptor = new SubmodelDescriptor
            {
                Description = AasJsonNodeDeserializer.DeserializeAasArray(item["description"], Jsonization.Deserialize.LangStringTextTypeFrom),
                DisplayName = AasJsonNodeDeserializer.DeserializeAasArray(item["displayName"], Jsonization.Deserialize.LangStringNameTypeFrom),
                Extensions = AasJsonNodeDeserializer.DeserializeAasArray(item["extensions"], Jsonization.Deserialize.ExtensionFrom),
                Administration = AasJsonNodeDeserializer.DeserializeAasNode(item["administration"], Jsonization.Deserialize.AdministrativeInformationFrom),
                IdShort = item["idShort"]?.GetValue<string>(),
                Id = item["id"]?.GetValue<string>(),
                SemanticId = AasJsonNodeDeserializer.DeserializeAasNode(item["semanticId"], Jsonization.Deserialize.ReferenceFrom),
                SupplementalSemanticId = AasJsonNodeDeserializer.DeserializeAasArray(item["supplementalSemanticId"], Jsonization.Deserialize.ReferenceFrom),
                Endpoints = item["endpoints"]?.Deserialize<List<EndpointData>>()
            };

            descriptors.Add(descriptor);
        }

        return descriptors;
    }

    private static void UpdateSubmodelTemplateKind(ISubmodel submodel) => submodel.Kind = ModellingKind.Instance;
}
