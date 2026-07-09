using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.ModuleTests.Common;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.Discovery;

public abstract class DiscoveryControllerTests : IDisposable
{
    private readonly ConfigTestFactory _factory;
    private readonly IAasRepositoryTemplateService _mockTemplateService;
    private readonly HttpClient _client;
    private readonly ICreateClient _httpClientFactory;
    private readonly IPluginManifestConflictHandler _mockPluginManifestConflictHandler;

    protected DiscoveryControllerTests(string configDir)
    {
        _mockTemplateService = Substitute.For<IAasRepositoryTemplateService>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();
        _mockPluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
        _httpClientFactory = Substitute.For<ICreateClient>();

        _factory = new ConfigTestFactory(configDir, services =>
        {
            _ = services.AddSingleton(mockPluginManifestProvider);
            _ = services.AddSingleton(_mockPluginManifestConflictHandler);
            _ = services.AddSingleton(_httpClientFactory);
            _ = services.AddSingleton(_mockTemplateService);
        });

        _client = _factory.CreateClient();
        _ = _mockPluginManifestConflictHandler.Manifests.Returns(TestData.CreatePluginManifestsWithAssetIdSearch());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SearchShellsByAssetLink_ReturnsOkWithAasIdsAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForAssetIdSearch());

        var assetLinks = new[]
        {
            new AssetLinkDto { Name = "SerialNumber", Value = "SN-4711" }
        };

        var response = await _client.PostAsJsonAsync("/lookup/shellsByAssetLink", assetLinks);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("urn:manufacturer-x:aas:motor:001", result[0]?.GetValue<string>());
        Assert.Equal("urn:manufacturer-x:aas:motor:002", result[1]?.GetValue<string>());
    }

    [Fact]
    public async Task SearchShellsByAssetLink_WithMultipleAssetLinks_ReturnsOkAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForAssetIdSearch());

        var assetLinks = new[]
        {
            new AssetLink { Name = "SerialNumber", Value = "SN-4711" },
            new AssetLink { Name = "BatchId", Value = "B-2026-03" }
        };

        var response = await _client.PostAsJsonAsync("/lookup/shellsByAssetLink", assetLinks);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SearchShellsByAssetLink_WithNoMatches_ReturnsEmptyResultAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForAssetIdSearchEmpty());

        var assetLinks = new[]
        {
            new AssetLink { Name = "SerialNumber", Value = "non-existent" }
        };

        var response = await _client.PostAsJsonAsync("/lookup/shellsByAssetLink", assetLinks);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchShellsByAssetLink_WithEmptyBody_Returns400Async()
    {
        var assetLinks = Array.Empty<AssetLink>();

        var response = await _client.PostAsJsonAsync("/lookup/shellsByAssetLink", assetLinks);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchShellsByAssetLink_WithNegativeLimit_Returns400Async()
    {
        var assetLinks = new[]
        {
            new AssetLink { Name = "SerialNumber", Value = "SN-4711" }
        };

        var response = await _client.PostAsJsonAsync("/lookup/shellsByAssetLink?limit=-1", assetLinks);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchShellsByAssetLink_WithPagination_ReturnsPagedResultAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForAssetIdSearch());

        var assetLinks = new[]
        {
            new AssetLink { Name = "SerialNumber", Value = "SN-4711" }
        };

        var response = await _client.PostAsJsonAsync("/lookup/shellsByAssetLink?limit=1", assetLinks);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        _ = Assert.Single(result);
    }

    [Fact]
    public async Task GetShellsByAssetIds_WithInvalidBase64_Returns400Async()
    {
        var response = await _client.GetAsync("/shells?assetIds=not-valid-base64!!!");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private void SetupPluginHttpClient(string pluginResponse)
    {
        var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(pluginResponse)
        }));
        var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://testendpoint1.com");
        const string HttpClientName = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName).Returns(httpClient);
    }
}

public class DiscoveryControllerTestsV2Config() : DiscoveryControllerTests("v2-config");

public class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
