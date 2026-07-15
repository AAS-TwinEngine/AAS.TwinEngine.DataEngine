using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Discovery.Requests;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Discovery;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.ModuleTests.Common;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.Discovery;

public abstract class DiscoveryControllerTests : IDisposable
{
    private readonly ConfigTestFactory _factory;
    private readonly IAasRepositoryTemplateService _mockTemplateService;
    private readonly IAasRepositoryService _mockAasRepositoryService;
    private readonly HttpClient _client;
    private readonly ICreateClient _httpClientFactory;
    private readonly IPluginManifestConflictHandler _mockPluginManifestConflictHandler;

    protected DiscoveryControllerTests(string configDir)
    {
        _mockTemplateService = Substitute.For<IAasRepositoryTemplateService>();
        _mockAasRepositoryService = Substitute.For<IAasRepositoryService>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();
        _mockPluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
        _httpClientFactory = Substitute.For<ICreateClient>();

        _factory = new ConfigTestFactory(configDir, services =>
        {
            _ = services.AddSingleton(mockPluginManifestProvider);
            _ = services.AddSingleton(_mockPluginManifestConflictHandler);
            _ = services.AddSingleton(_httpClientFactory);
            _ = services.AddSingleton(_mockTemplateService);
            _ = services.AddSingleton(_mockAasRepositoryService);
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

    private void SetupPluginHttpClientNotCalled()
    {
        const string HttpClientName = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName).Returns((HttpClient)null!);
    }

    private void SetupTemplateProvider()
    {
        _ = _mockTemplateService.GetShellTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new AssetAdministrationShell(
                callInfo.ArgAt<string>(0),
                new AssetInformation(AssetKind.Instance))
            {
                Submodels =
                [
                    new Reference(ReferenceTypes.ModelReference,
                    [
                        new Key(KeyTypes.Submodel, "urn:example:sm:nameplate:001")
                    ])
                ]
            });
    }

    [Fact]
    public async Task GetSpecificAssetIdByAasIdentifier_ReturnsOkWithSpecificAssetIdsAsync()
    {
        // Arrange
        var aasId = "urn:example:aas:001";
        var encodedAasId = aasId.EncodeBase64Url();
        var specificAssetId = new SpecificAssetId("Manufacturer", "Corp");
        var shell = new AssetAdministrationShell(aasId, new AssetInformation(AssetKind.Instance)
        {
            SpecificAssetIds = [specificAssetId]
        });

        _ = _mockAasRepositoryService.GetShellByIdAsync(aasId, Arg.Any<CancellationToken>())
            .Returns(shell);

        // Act
        var response = await _client.GetAsync($"/lookup/shells/{encodedAasId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonArray>();
        Assert.NotNull(json);
        Assert.Single(json);
        var item = json[0]?.AsObject();
        Assert.NotNull(item);
        Assert.Equal("Manufacturer", item["name"]?.GetValue<string>());
        Assert.Equal("Corp", item["value"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetSpecificAssetIdByAasIdentifier_WhenShellNotFound_ReturnsNotFoundAsync()
    {
        // Arrange
        var aasId = "urn:example:aas:nonexistent";
        var encodedAasId = aasId.EncodeBase64Url();

        _ = _mockAasRepositoryService.GetShellByIdAsync(aasId, Arg.Any<CancellationToken>())
            .Returns((IAssetAdministrationShell)null!);

        // Act
        var response = await _client.GetAsync($"/lookup/shells/{encodedAasId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSpecificAssetIdByAasIdentifier_WhenSpecificAssetIdsEmpty_ReturnsNotFoundAsync()
    {
        // Arrange
        var aasId = "urn:example:aas:no-ids";
        var encodedAasId = aasId.EncodeBase64Url();
        var shell = new AssetAdministrationShell(aasId, new AssetInformation(AssetKind.Instance)
        {
            SpecificAssetIds = []
        });

        _ = _mockAasRepositoryService.GetShellByIdAsync(aasId, Arg.Any<CancellationToken>())
            .Returns(shell);

        // Act
        var response = await _client.GetAsync($"/lookup/shells/{encodedAasId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class DiscoveryControllerTestsV2Config() : DiscoveryControllerTests("v2-config");

public class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
