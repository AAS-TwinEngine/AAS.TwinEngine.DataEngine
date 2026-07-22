using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Caching;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;

namespace AAS.TwinEngine.DataEngine.Infrastructure.Providers.SubmodelRegistryProvider.Services;

public class CachingSubmodelDescriptorProvider(
    HybridCache cache,
    IHttpContextAccessor httpContextAccessor,
    ISubmodelDescriptorProvider innerProvider) : ISubmodelDescriptorProvider
{
    private const string MethodGetSubmodelDescriptor = "GetDataForSubmodelDescriptorById";

    public async Task<SubmodelDescriptor> GetDataForSubmodelDescriptorByIdAsync(string id, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeyBuilder.BuildCacheKey(
            httpContextAccessor,
            MethodGetSubmodelDescriptor,
            id);

        var json = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await innerProvider.GetDataForSubmodelDescriptorByIdAsync(id, token).ConfigureAwait(false);
                return DescriptorSerializer.SerializeSubmodelDescriptor(result);
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return DescriptorSerializer.DeserializeSubmodelDescriptor(json!);
    }
}
