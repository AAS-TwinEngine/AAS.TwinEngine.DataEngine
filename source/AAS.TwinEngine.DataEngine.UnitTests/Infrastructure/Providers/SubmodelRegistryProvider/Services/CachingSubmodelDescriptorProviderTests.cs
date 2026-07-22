using System.Security.Claims;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.SubmodelRegistryProvider.Services;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.SubmodelRegistryProvider.Services;

public class CachingSubmodelDescriptorProviderTests : IDisposable
{
    private readonly ISubmodelDescriptorProvider _innerProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CachingSubmodelDescriptorProvider _sut;
    private readonly ServiceProvider _serviceProvider;

    private const string DescriptorId = "ContactInformation";

    public CachingSubmodelDescriptorProviderTests()
    {
        _innerProvider = Substitute.For<ISubmodelDescriptorProvider>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        var services = new ServiceCollection();
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });
        _serviceProvider = services.BuildServiceProvider();

        var hybridCache = _serviceProvider.GetRequiredService<HybridCache>();

        var options = Substitute.For<Microsoft.Extensions.Options.IOptions<TemplateManagementConfig>>();
        options.Value.Returns(new TemplateManagementConfig
        {
            SubmodelTemplateRegistry = new ServiceInstance { LocalCacheExpirationInMinutes = 5 }
        });

        _sut = new CachingSubmodelDescriptorProvider(hybridCache, _httpContextAccessor, _innerProvider, options);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_CacheHit_DoesNotCallInnerProviderTwice()
    {
        SetupAnonymousContext();
        var descriptor = new SubmodelDescriptor { Id = DescriptorId, IdShort = "Contact" };
        _innerProvider.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(descriptor));

        var result1 = await _sut.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, CancellationToken.None);
        var result2 = await _sut.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, CancellationToken.None);

        Assert.Equal(DescriptorId, result1.Id);
        Assert.Equal(DescriptorId, result2.Id);
        Assert.Equal("Contact", result1.IdShort);
        await _innerProvider.Received(1).GetDataForSubmodelDescriptorByIdAsync(DescriptorId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_CacheMiss_CallsInnerProvider()
    {
        SetupAnonymousContext();
        var descriptor = new SubmodelDescriptor { Id = DescriptorId };
        _innerProvider.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(descriptor));

        var result = await _sut.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, CancellationToken.None);

        Assert.Equal(DescriptorId, result.Id);
        await _innerProvider.Received(1).GetDataForSubmodelDescriptorByIdAsync(DescriptorId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_PermissionChange_CausesCacheMiss()
    {
        var descriptor = new SubmodelDescriptor { Id = DescriptorId };
        _innerProvider.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(descriptor));

        SetupAuthenticatedContext("user-1", [new Claim(ClaimTypes.Role, "Admin")]);
        await _sut.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, CancellationToken.None);

        SetupAuthenticatedContext("user-1", [new Claim(ClaimTypes.Role, "Reader")]);
        await _sut.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, CancellationToken.None);

        await _innerProvider.Received(2).GetDataForSubmodelDescriptorByIdAsync(DescriptorId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_Unauthenticated_UsesAnonymousKeyAndCachesResult()
    {
        SetupAnonymousContext();
        var descriptor = new SubmodelDescriptor { Id = DescriptorId };
        _innerProvider.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(descriptor));

        var result1 = await _sut.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, CancellationToken.None);
        var result2 = await _sut.GetDataForSubmodelDescriptorByIdAsync(DescriptorId, CancellationToken.None);

        Assert.Equal(DescriptorId, result1.Id);
        Assert.Equal(DescriptorId, result2.Id);
        await _innerProvider.Received(1).GetDataForSubmodelDescriptorByIdAsync(DescriptorId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_DifferentIds_ProduceDifferentCacheKeys()
    {
        SetupAnonymousContext();
        _innerProvider.GetDataForSubmodelDescriptorByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var id = callInfo.ArgAt<string>(0);
                return Task.FromResult(new SubmodelDescriptor { Id = id });
            });

        await _sut.GetDataForSubmodelDescriptorByIdAsync("id-1", CancellationToken.None);
        await _sut.GetDataForSubmodelDescriptorByIdAsync("id-2", CancellationToken.None);

        await _innerProvider.Received(1).GetDataForSubmodelDescriptorByIdAsync("id-1", Arg.Any<CancellationToken>());
        await _innerProvider.Received(1).GetDataForSubmodelDescriptorByIdAsync("id-2", Arg.Any<CancellationToken>());
    }

    private void SetupAnonymousContext() => _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

    private void SetupAuthenticatedContext(string userId, Claim[] additionalClaims)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(additionalClaims);

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(httpContext);
    }
}
