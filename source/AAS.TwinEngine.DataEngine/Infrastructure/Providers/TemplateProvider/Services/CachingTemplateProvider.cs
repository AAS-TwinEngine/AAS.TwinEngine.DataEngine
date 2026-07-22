using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Caching;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

public class CachingTemplateProvider(
    HybridCache cache,
    IHttpContextAccessor httpContextAccessor,
    ITemplateProvider innerProvider) : ITemplateProvider
{
    private const string MethodGetFilteredSubmodel = "GetFilteredSubmodelTemplate";
    private const string MethodGetFilteredSubmodelBySemanticId = "GetFilteredSubmodelTemplateBySemanticId";
    private const string MethodGetShellDescriptorTemplate = "GetShellDescriptorTemplate";
    private const string MethodGetShellTemplate = "GetShellTemplate";
    private const string MethodGetAssetInformationTemplate = "GetAssetInformationTemplate";
    private const string MethodGetSubmodelRefById = "GetSubmodelRefById";
    private const string MethodGetConceptDescriptionById = "GetConceptDescriptionById";

    public async Task<ISubmodel?> GetFilteredSubmodelTemplateAsync(string templateId, SubmodelQueryOptions? queryOptions, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildCacheKey(
            httpContextAccessor,
            MethodGetFilteredSubmodel,
            templateId,
            queryOptions?.Level ?? string.Empty,
            queryOptions?.Extent ?? string.Empty);

        var json = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await innerProvider.GetFilteredSubmodelTemplateAsync(templateId, queryOptions, token).ConfigureAwait(false);
                return result is not null
                    ? Jsonization.Serialize.ToJsonObject(result).ToJsonString()
                    : null;
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (json is null)
        {
            return null;
        }

        var jsonNode = JsonNode.Parse(json);
        var submodel = Jsonization.Deserialize.SubmodelFrom(jsonNode!);
        return submodel;
    }

    public async Task<ISubmodel?> GetFilteredSubmodelTemplateBySemanticIdAsync(string semanticId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildCacheKey(
            httpContextAccessor,
            MethodGetFilteredSubmodelBySemanticId,
            semanticId);

        var json = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await innerProvider.GetFilteredSubmodelTemplateBySemanticIdAsync(semanticId, token).ConfigureAwait(false);
                return result is not null
                    ? Jsonization.Serialize.ToJsonObject(result).ToJsonString()
                    : null;
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (json is null)
        {
            return null;
        }

        var jsonNode = JsonNode.Parse(json);
        var submodel = Jsonization.Deserialize.SubmodelFrom(jsonNode!);
        return submodel;
    }

    public async Task<ShellDescriptor> GetShellDescriptorTemplateAsync(string templateId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildCacheKey(
            httpContextAccessor,
            MethodGetShellDescriptorTemplate,
            templateId);

        var json = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await innerProvider.GetShellDescriptorTemplateAsync(templateId, token).ConfigureAwait(false);
                return DescriptorSerializer.SerializeShellDescriptor(result);
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return DescriptorSerializer.DeserializeShellDescriptor(json!);
    }

    public async Task<IAssetAdministrationShell> GetShellTemplateAsync(string templateId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildCacheKey(
            httpContextAccessor,
            MethodGetShellTemplate,
            templateId);

        var json = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await innerProvider.GetShellTemplateAsync(templateId, token).ConfigureAwait(false);
                return Jsonization.Serialize.ToJsonObject(result).ToJsonString();
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var jsonNode = JsonNode.Parse(json!);
        return Jsonization.Deserialize.AssetAdministrationShellFrom(jsonNode!);
    }

    public async Task<IAssetInformation> GetAssetInformationTemplateAsync(string templateId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildCacheKey(
            httpContextAccessor,
            MethodGetAssetInformationTemplate,
            templateId);

        var json = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await innerProvider.GetAssetInformationTemplateAsync(templateId, token).ConfigureAwait(false);
                return Jsonization.Serialize.ToJsonObject(result).ToJsonString();
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var jsonNode = JsonNode.Parse(json!);
        return Jsonization.Deserialize.AssetInformationFrom(jsonNode!);
    }

    public async Task<List<IReference>> GetSubmodelRefByIdAsync(string templateId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildCacheKey(
            httpContextAccessor,
            MethodGetSubmodelRefById,
            templateId);

        var json = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await innerProvider.GetSubmodelRefByIdAsync(templateId, token).ConfigureAwait(false);
                var jsonArray = new JsonArray(result.Select(r => Jsonization.Serialize.ToJsonObject(r)).ToArray<JsonNode>());
                return jsonArray.ToJsonString();
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var parsedArray = JsonNode.Parse(json!) as JsonArray;
        return parsedArray!
            .Select(node => (IReference)Jsonization.Deserialize.ReferenceFrom(node!))
            .ToList();
    }

    public async Task<IConceptDescription?> GetConceptDescriptionByIdAsync(string cdIdentifier, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildCacheKey(
            httpContextAccessor,
            MethodGetConceptDescriptionById,
            cdIdentifier);

        var json = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await innerProvider.GetConceptDescriptionByIdAsync(cdIdentifier, token).ConfigureAwait(false);
                return result is not null
                    ? Jsonization.Serialize.ToJsonObject(result).ToJsonString()
                    : null;
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (json is null)
        {
            return null;
        }

        var jsonNode = JsonNode.Parse(json);
        return Jsonization.Deserialize.ConceptDescriptionFrom(jsonNode!);
    }
}
