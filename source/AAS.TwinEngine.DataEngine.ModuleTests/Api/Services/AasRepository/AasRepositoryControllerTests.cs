using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

using AasCore.Aas3_0;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.AasRepository;

public class AasRepositoryControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITemplateProvider _mockTemplateProvider;
    private readonly HttpClient _client;
    private readonly ICreateClient _httpClientFactory;

    public AasRepositoryControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockTemplateProvider = Substitute.For<ITemplateProvider>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();
        var mockPluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
        _httpClientFactory = Substitute.For<ICreateClient>();

        var factory1 = factory.WithWebHostBuilder(builder =>
        {
            _ = builder.ConfigureServices(services =>
            {
                _ = services.AddSingleton(mockPluginManifestProvider);
                _ = services.AddSingleton(mockPluginManifestConflictHandler);
                _ = services.AddSingleton(_httpClientFactory);
                _ = services.AddSingleton(_mockTemplateProvider);
            });
        });

        _client = factory1.CreateClient();
        _ = mockPluginManifestConflictHandler.Manifests.Returns(TestData.CreatePluginManifests());
    }

    [Fact]
    public async Task GetShellByIdAsync_ReturnsOkAsync()
    {
        // Arrange
        var aasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1NjgvdGVzdC9hYXM=";
        var mockShellTemplate = TestData.CreateShellTemplate();
        var mockAssetInformationTemplate = TestData.CreateAssetInformationTemplate();
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePluginResponseForAssetinformation())
        }));

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://testendpoint.com");

        const string HttpClientName = $"{PluginConfig.HttpClientNamePrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName).Returns(httpClient);

        _ = _mockTemplateProvider.GetShellTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockShellTemplate);

        _ = _mockTemplateProvider.GetAssetInformationTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockAssetInformationTemplate);

        // Act
        var response = await _client.GetAsync($"/shells/{aasIdentifier}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        var jsonString = await response.Content.ReadAsStringAsync();
        var jsonNode = JsonNode.Parse(jsonString);
        var shell = Jsonization.Deserialize.AssetAdministrationShellFrom(jsonNode!);
        Assert.NotNull(json);
        var shellResponse = json.ToString();
        var expectedShell = TestData.CreateShellResponse();
        Assert.Equal(shellResponse, expectedShell);
        var productId = TestData.GetProductIdFromRule(shell.Submodels!.FirstOrDefault()?.Keys.FirstOrDefault()!.Value!, 5);
        var expectedProductId = TestData.GetProductIdFromRule(aasIdentifier.DecodeBase64Url(), 6);
        Assert.Equal(productId, expectedProductId);
    }

    [Fact]
    public async Task GetShellByIdAsync_ReturnsOkAsync_WhenErrorWhileExtractionOfProductId()
    {
        // Arrange
        var aasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFz";
        var mockShellTemplate = TestData.CreateShellTemplate();
        var mockAssetInformationTemplate = TestData.CreateAssetInformationTemplate();
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePluginResponseForAssetinformation())
        }));

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://testendpoint.com");

        var httpClientName = $"{PluginConfig.HttpClientNamePrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(httpClientName).Returns(httpClient);

        _ = _mockTemplateProvider.GetShellTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockShellTemplate);

        _ = _mockTemplateProvider.GetAssetInformationTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockAssetInformationTemplate);

        // Act
        var response = await _client.GetAsync($"/shells/{aasIdentifier}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_ReturnsOkAsync()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        var mockAssetInformationTemplate = TestData.CreateAssetInformationTemplate();
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePluginResponseForAssetinformation())
        }));

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://testendpoint.com");

        const string HttpClientName = $"{PluginConfig.HttpClientNamePrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName).Returns(httpClient);

        _ = _mockTemplateProvider.GetAssetInformationTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockAssetInformationTemplate);

        // Act
        var response = await _client.GetAsync($"/shells/{AasIdentifier}/asset-information");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var assetResponse = json.ToString();
        var expectedAsset = TestData.CreateAssetInformationResponse();
        Assert.Equal(assetResponse, expectedAsset);
    }

    [Fact]
    public async Task GetShellByIdAsync_WithNotFound_Returns404Async()
    {
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";

        _ = _mockTemplateProvider.GetShellTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync($"/shells/{AasIdentifier}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_WithNotFound_Returns404Async()
    {
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";

        _ = _mockTemplateProvider.GetAssetInformationTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync($"/shells/{AasIdentifier}/asset-information");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetShellByIdAsync_WithInternalServerError_Returns500Async()
    {
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";

        _ = _mockTemplateProvider.GetShellTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResponseParsingException());

        var response = await _client.GetAsync($"/shells/{AasIdentifier}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_WithInternalServerError_Returns500Async()
    {
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";

        _ = _mockTemplateProvider.GetAssetInformationTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResponseParsingException());

        var response = await _client.GetAsync($"/shells/{AasIdentifier}/asset-information");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetShellByIdAsync_WhenIdentifierIsInValid_Returns400Async()
    {
        const string AasIdentifier = "in valid";

        var response = await _client.GetAsync($"/shells/{AasIdentifier}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAssetInformationByIdAsync_WhenIdentifierIsInValid_Returns400Async()
    {
        const string AasIdentifier = "in valid";

        var response = await _client.GetAsync($"/shells/{AasIdentifier}/asset-information");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ReturnsOkAsync()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1NjgvdGVzdC9hYXM=";
        var mockTemplate = TestData.CreateSubmodelRefs();

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockTemplate);

        // Act
        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodel-refs?limit=5&cursor=next123");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_WithInternalServerError_Returns500Async()
    {
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResponseParsingException());

        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodel-refs?limit=5&cursor=next123");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_WhenIdentifierIsInValid_Returns400Async()
    {
        const string AasIdentifier = "in valid";

        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodel-refs?limit=-1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
