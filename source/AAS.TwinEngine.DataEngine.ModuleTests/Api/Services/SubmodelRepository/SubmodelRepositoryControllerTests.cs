using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.SubmodelRepository;

public class SubmodelRepositoryControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITemplateProvider _mockTemplateProvider;
    private readonly HttpClient _client;
    private readonly ICreateClient _httpClientFactory;

    public SubmodelRepositoryControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockTemplateProvider = Substitute.For<ITemplateProvider>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();
        var mockPluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
        _httpClientFactory = Substitute.For<ICreateClient>();

        var factory1 = factory.WithWebHostBuilder(builder =>
        {
            _ = builder.ConfigureServices(services =>
            {
                _ = services.AddSingleton(_httpClientFactory);
                _ = services.AddSingleton(_mockTemplateProvider);
                _ = services.AddSingleton(mockPluginManifestProvider);
                _ = services.AddSingleton(mockPluginManifestConflictHandler);
            });
        });

        _client = factory1.CreateClient();
        _ = mockPluginManifestConflictHandler.Manifests.Returns(TestData.CreatePluginManifests());
    }

    [Fact]
    public async Task GetSubmodelAsync_WithValidIdentifier_ReturnsOkAsync()
    {
        // Arrange
        using var messageHandlerPlugin1 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePlugin1ResponseForSubmodel())
        }));

        using var messageHandlerPlugin2 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePlugin2ResponseForSubmodel())
        }));

        using var httpClientPlugin1 = new HttpClient(messageHandlerPlugin1);
        httpClientPlugin1.BaseAddress = new Uri("https://testendpoint1.com");

        using var httpClientPlugin2 = new HttpClient(messageHandlerPlugin2);
        httpClientPlugin2.BaseAddress = new Uri("https://testendpoint2.com");

        const string HttpClientNamePlugin1 = $"{PluginConfig.HttpClientNamePrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin1).Returns(httpClientPlugin1);

        const string HttpClientNamePlugin2 = $"{PluginConfig.HttpClientNamePrefix}TestPlugin2";
        _httpClientFactory.CreateClient(HttpClientNamePlugin2).Returns(httpClientPlugin2);

        var submodelId = "Q29udGFjdEluZm9ybWF0aW9u";
        var mockSubmodel = TestData.CreateSubmodel();

        _ = _mockTemplateProvider.GetSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockSubmodel);

        // Act
        var response = await _client.GetAsync($"/submodels/{submodelId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var submodelResponse = json.ToString();
        var expectedSubmodel = TestData.CreateSubmodelWithValues();
        Assert.Equal(submodelResponse, expectedSubmodel);
    }

    [Fact]
    public async Task GetSubmodelAsync_WithNotFound_Returns404Async()
    {
        const string SubmodelId = "Q29udGFjdEluZm9ybWF0aW9u";

        _ = _mockTemplateProvider.GetSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync($"/submodels/{SubmodelId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelAsync_WithInternalServerError_Returns500Async()
    {
        const string SubmodelId = "Q29udGFjdEluZm9ybWF0aW9u";

        _ = _mockTemplateProvider.GetSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResponseParsingException());

        var response = await _client.GetAsync($"/submodels/{SubmodelId}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelAsync_WhenIdentifierIsInValid_Returns400Async()
    {
        const string SubmodelId = "in valid";

        var response = await _client.GetAsync($"/submodels/{SubmodelId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelElementAsync_ReturnsOkAsync()
    {
        // Arrange
        const string SubmodelId = "Q29udGFjdEluZm9ybWF0aW9u";
        const string IdShortPath = "ContactName";
        var mockSubmodel = TestData.CreateSubmodel();
        TestData.CreatePluginResponseForSubmodelElement();

        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePluginResponseForSubmodelElement())
        }));

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://testendpoint.com");

        const string HttpClientName = $"{PluginConfig.HttpClientNamePrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName).Returns(httpClient);

        _ = _mockTemplateProvider.GetSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockSubmodel);

        // Act
        var response = await _client.GetAsync($"/submodels/{SubmodelId}/submodel-elements/{IdShortPath}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var submodelElementResponse = json.ToString();
        var expectedSubmodelElement = TestData.CreateSubmodelElementWithValues();
        Assert.Equal(submodelElementResponse, expectedSubmodelElement);
    }

    [Fact]
    public async Task GetSubmodelElementAsync_WithNotFound_Returns404Async()
    {
        const string SubmodelId = "Q29udGFjdEluZm9ybWF0aW9u";

        _ = _mockTemplateProvider.GetSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync($"/submodels/{SubmodelId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelElementAsync_WhenIdentifierIsInValid_Returns400Async()
    {
        const string SubmodelId = "in valid";

        var response = await _client.GetAsync($"/submodels/{SubmodelId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelElementAsync__WithInternalServerError_Returns500Async()
    {
        const string SubmodelId = "Q29udGFjdEluZm9ybWF0aW9u";

        _ = _mockTemplateProvider.GetSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResponseParsingException());

        var response = await _client.GetAsync($"/submodels/{SubmodelId}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}

public class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
