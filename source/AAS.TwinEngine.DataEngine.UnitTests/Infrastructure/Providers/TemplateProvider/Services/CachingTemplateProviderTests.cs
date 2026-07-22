using System.Security.Claims;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;
using AAS.TwinEngine.DataEngine.Infrastructure.Caching;
using AAS.TwinEngine.DataEngine.Infrastructure.Shared;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.TemplateProvider.Services;

public class CachingTemplateProviderTests : IDisposable
{
    private readonly ITemplateProvider _innerProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CachingTemplateProvider _sut;
    private readonly ServiceProvider _serviceProvider;

    private const string TemplateId = "Nameplate";

    public CachingTemplateProviderTests()
    {
        _innerProvider = Substitute.For<ITemplateProvider>();
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
        _sut = new CachingTemplateProvider(hybridCache, _httpContextAccessor, _innerProvider);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_CacheHit_DoesNotCallInnerProviderTwice()
    {
        SetupAnonymousContext();
        var submodel = CreateTestSubmodel("submodel-1");
        _innerProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISubmodel?>(submodel));

        var result1 = await _sut.GetFilteredSubmodelTemplateAsync(TemplateId, null, CancellationToken.None);
        var result2 = await _sut.GetFilteredSubmodelTemplateAsync(TemplateId, null, CancellationToken.None);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("submodel-1", result1!.Id);
        Assert.Equal("submodel-1", result2!.Id);
        await _innerProvider.Received(1).GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_CacheMiss_CallsInnerProvider()
    {
        SetupAnonymousContext();
        var submodel = CreateTestSubmodel("submodel-1");
        _innerProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISubmodel?>(submodel));

        var result = await _sut.GetFilteredSubmodelTemplateAsync(TemplateId, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("submodel-1", result!.Id);
        await _innerProvider.Received(1).GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_ReturnsNull_WhenInnerProviderReturnsNull()
    {
        SetupAnonymousContext();
        _innerProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISubmodel?>(null));

        var result = await _sut.GetFilteredSubmodelTemplateAsync(TemplateId, null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_PermissionChange_CausesCacheMiss()
    {
        var submodel = CreateTestSubmodel("submodel-1");
        _innerProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISubmodel?>(submodel));

        SetupAuthenticatedContext("user-1", [new Claim(ClaimTypes.Role, "Admin")]);
        await _sut.GetFilteredSubmodelTemplateAsync(TemplateId, null, CancellationToken.None);

        SetupAuthenticatedContext("user-1", [new Claim(ClaimTypes.Role, "Reader")]);
        await _sut.GetFilteredSubmodelTemplateAsync(TemplateId, null, CancellationToken.None);

        await _innerProvider.Received(2).GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_Unauthenticated_UsesAnonymousKeyAndCachesResult()
    {
        SetupAnonymousContext();
        var submodel = CreateTestSubmodel("submodel-anon");
        _innerProvider.GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISubmodel?>(submodel));

        var result1 = await _sut.GetFilteredSubmodelTemplateAsync(TemplateId, null, CancellationToken.None);
        var result2 = await _sut.GetFilteredSubmodelTemplateAsync(TemplateId, null, CancellationToken.None);

        Assert.Equal("submodel-anon", result1!.Id);
        Assert.Equal("submodel-anon", result2!.Id);
        await _innerProvider.Received(1).GetFilteredSubmodelTemplateAsync(TemplateId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateAsync_DifferentQueryOptions_ProduceDifferentCacheKeys()
    {
        SetupAnonymousContext();
        var submodel = CreateTestSubmodel("submodel-1");
        _innerProvider.GetFilteredSubmodelTemplateAsync(TemplateId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISubmodel?>(submodel));

        var options1 = new SubmodelQueryOptions("deep", null);
        var options2 = new SubmodelQueryOptions("core", null);

        await _sut.GetFilteredSubmodelTemplateAsync(TemplateId, options1, CancellationToken.None);
        await _sut.GetFilteredSubmodelTemplateAsync(TemplateId, options2, CancellationToken.None);

        await _innerProvider.Received(2).GetFilteredSubmodelTemplateAsync(TemplateId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_CacheHit_DoesNotCallInnerProviderTwice()
    {
        SetupAnonymousContext();
        var descriptor = new ShellDescriptor { Id = "shell-1", IdShort = "TestShell" };
        _innerProvider.GetShellDescriptorTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(descriptor));

        var result1 = await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);
        var result2 = await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal("shell-1", result1.Id);
        Assert.Equal("shell-1", result2.Id);
        await _innerProvider.Received(1).GetShellDescriptorTemplateAsync(TemplateId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_PermissionChange_CausesCacheMiss()
    {
        var descriptor = new ShellDescriptor { Id = "shell-1" };
        _innerProvider.GetShellDescriptorTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(descriptor));

        SetupAuthenticatedContext("user-1", [new Claim(ClaimTypes.Role, "Admin")]);
        await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);

        SetupAuthenticatedContext("user-1", [new Claim(ClaimTypes.Role, "Reader")]);
        await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);

        await _innerProvider.Received(2).GetShellDescriptorTemplateAsync(TemplateId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellDescriptorTemplateAsync_WithSpecificAssetIds_RoundtripsCorrectly()
    {
        SetupAnonymousContext();
        var externalSubjectId = new Reference(ReferenceTypes.ExternalReference,
        [
            new Key(KeyTypes.GlobalReference, "http://subject-id")
        ]);
        var specificAssetId = new SpecificAssetId("asset-name", "asset-value", externalSubjectId);

        var descriptor = new ShellDescriptor
        {
            Id = "shell-1",
            SpecificAssetIds = [specificAssetId]
        };

        _innerProvider.GetShellDescriptorTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(descriptor));

        var result1 = await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);
        var result2 = await _sut.GetShellDescriptorTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal("shell-1", result1.Id);
        Assert.Equal("shell-1", result2.Id);
        Assert.NotNull(result2.SpecificAssetIds);
        Assert.Single(result2.SpecificAssetIds);
        Assert.Equal("asset-name", result2.SpecificAssetIds[0].Name);
        Assert.NotNull(result2.SpecificAssetIds[0].ExternalSubjectId);
        Assert.Equal("http://subject-id", result2.SpecificAssetIds[0].ExternalSubjectId!.Keys[0].Value);
        await _innerProvider.Received(1).GetShellDescriptorTemplateAsync(TemplateId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShellTemplateAsync_CacheHit_DoesNotCallInnerProviderTwice()
    {
        SetupAnonymousContext();
        var shell = CreateTestShell("shell-1");
        _innerProvider.GetShellTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(shell));

        var result1 = await _sut.GetShellTemplateAsync(TemplateId, CancellationToken.None);
        var result2 = await _sut.GetShellTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal("shell-1", result1.Id);
        Assert.Equal("shell-1", result2.Id);
        await _innerProvider.Received(1).GetShellTemplateAsync(TemplateId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAssetInformationTemplateAsync_CacheHit_DoesNotCallInnerProviderTwice()
    {
        SetupAnonymousContext();
        var assetInfo = CreateTestAssetInformation("global-asset-1");
        _innerProvider.GetAssetInformationTemplateAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(assetInfo));

        var result1 = await _sut.GetAssetInformationTemplateAsync(TemplateId, CancellationToken.None);
        var result2 = await _sut.GetAssetInformationTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal("global-asset-1", result1.GlobalAssetId);
        Assert.Equal("global-asset-1", result2.GlobalAssetId);
        await _innerProvider.Received(1).GetAssetInformationTemplateAsync(TemplateId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_CacheHit_DoesNotCallInnerProviderTwice()
    {
        SetupAnonymousContext();
        var refs = new List<IReference>
        {
            new Reference(ReferenceTypes.ExternalReference, new List<IKey>
            {
                new Key(KeyTypes.Submodel, "ref-1")
            })
        };
        _innerProvider.GetSubmodelRefByIdAsync(TemplateId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(refs));

        var result1 = await _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None);
        var result2 = await _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None);

        Assert.Single(result1);
        Assert.Single(result2);
        Assert.Equal("ref-1", result1[0].Keys[0].Value);
        await _innerProvider.Received(1).GetSubmodelRefByIdAsync(TemplateId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetConceptDescriptionByIdAsync_CacheHit_DoesNotCallInnerProviderTwice()
    {
        SetupAnonymousContext();
        var cd = CreateTestConceptDescription("cd-1");
        _innerProvider.GetConceptDescriptionByIdAsync("cd-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IConceptDescription?>(cd));

        var result1 = await _sut.GetConceptDescriptionByIdAsync("cd-1", CancellationToken.None);
        var result2 = await _sut.GetConceptDescriptionByIdAsync("cd-1", CancellationToken.None);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("cd-1", result1!.Id);
        await _innerProvider.Received(1).GetConceptDescriptionByIdAsync("cd-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetConceptDescriptionByIdAsync_ReturnsNull_WhenInnerProviderReturnsNull()
    {
        SetupAnonymousContext();
        _innerProvider.GetConceptDescriptionByIdAsync("cd-missing", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IConceptDescription?>(null));

        var result = await _sut.GetConceptDescriptionByIdAsync("cd-missing", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFilteredSubmodelTemplateBySemanticIdAsync_CacheHit_DoesNotCallInnerProviderTwice()
    {
        SetupAnonymousContext();
        var submodel = CreateTestSubmodel("semantic-submodel");
        _innerProvider.GetFilteredSubmodelTemplateBySemanticIdAsync("sem-id-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ISubmodel?>(submodel));

        var result1 = await _sut.GetFilteredSubmodelTemplateBySemanticIdAsync("sem-id-1", CancellationToken.None);
        var result2 = await _sut.GetFilteredSubmodelTemplateBySemanticIdAsync("sem-id-1", CancellationToken.None);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("semantic-submodel", result1!.Id);
        await _innerProvider.Received(1).GetFilteredSubmodelTemplateBySemanticIdAsync("sem-id-1", Arg.Any<CancellationToken>());
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

    private static ISubmodel CreateTestSubmodel(string id) => new Submodel(id);

    private static IAssetAdministrationShell CreateTestShell(string id) => new AssetAdministrationShell(id, new AssetInformation(AssetKind.Instance));

    private static IAssetInformation CreateTestAssetInformation(string globalAssetId) => new AssetInformation(AssetKind.Instance) { GlobalAssetId = globalAssetId };

    private static IConceptDescription CreateTestConceptDescription(string id) => new ConceptDescription(id);
}
