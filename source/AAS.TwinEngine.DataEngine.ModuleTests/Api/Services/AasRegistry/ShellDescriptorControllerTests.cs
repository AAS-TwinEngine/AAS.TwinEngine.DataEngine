using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;
using AAS.TwinEngine.DataEngine.ModuleTests.ApplicationLogic.Services.AasRegistry;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.AasRegistry;

public class ShellDescriptorControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ITemplateProvider _mockTemplateProvider;
    private readonly HttpClient _client;
    private readonly ICreateClient _httpClientFactory;

    public ShellDescriptorControllerTests(WebApplicationFactory<Program> factory)
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
                _ = services.AddSingleton(mockPluginManifestProvider);
                _ = services.AddSingleton(_mockTemplateProvider);
                _ = services.AddSingleton(mockPluginManifestConflictHandler);
            });
        });

        _client = factory1.CreateClient();
        _ = mockPluginManifestConflictHandler.Manifests.Returns(TestData.CreatePluginManifests());
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ReturnsOkAsync()
    {
        // Arrange
        var template = TestData.CreateShellDescriptorsTemplate();
        using var messageHandlerPlugin1 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePlugin1ResponseForShellDescriptors())
        }));

        using var messageHandlerPlugin2 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePlugin2ResponseForShellDescriptors())
        }));

        using var httpClientPlugin1 = new HttpClient(messageHandlerPlugin1);
        httpClientPlugin1.BaseAddress = new Uri("https://testendpoint1.com");

        using var httpClientPlugin2 = new HttpClient(messageHandlerPlugin2);
        httpClientPlugin2.BaseAddress = new Uri("https://testendpoint2.com");

        const string HttpClientNamePlugin1 = $"{PluginConfig.HttpClientNamePrefix}TestPlugin1";
        _httpClientFactory.CreateClient(HttpClientNamePlugin1).Returns(httpClientPlugin1);

        const string HttpClientNamePlugin2 = $"{PluginConfig.HttpClientNamePrefix}TestPlugin2";
        _httpClientFactory.CreateClient(HttpClientNamePlugin2).Returns(httpClientPlugin2);

        _ = _mockTemplateProvider.GetShellDescriptorsTemplateAsync(Arg.Any<CancellationToken>()).Returns(template);

        // Act
        var response = await _client.GetAsync("/shell-descriptors?limit=2&cursor=next123");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var shellDescriptorsResponse = json.ToString();
        var expectedShellDescriptors = TestData.CreateShellDescriptors();
        Assert.Equal(shellDescriptorsResponse, expectedShellDescriptors);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithNagetiveLimit_Returns400Async()
    {
        _ = _mockTemplateProvider.GetShellDescriptorsTemplateAsync(Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync("/shell-descriptors?limit=-1&cursor=next123");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithInValidCursor_Returns400Async()
    {
        _ = _mockTemplateProvider.GetShellDescriptorsTemplateAsync(Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync("/shell-descriptors?limit=4&cursor=invalid cursor");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithNotFound_Returns404Async()
    {
        _ = _mockTemplateProvider.GetShellDescriptorsTemplateAsync(Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync("/shell-descriptors?limit=5&cursor=next123");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithInternalServerError_Returns500Async()
    {
        _ = _mockTemplateProvider.GetShellDescriptorsTemplateAsync(Arg.Any<CancellationToken>()).Throws(new ResponseParsingException());

        var response = await _client.GetAsync("/shell-descriptors?limit=5&cursor=next123");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ReturnsOkAsync()
    {
        // Arrange
        const string AasId = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        var template = TestData.CreateShellDescriptorsTemplate();

        using var messageHandler1 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePlugin1ResponseForShellDescriptor())
        }));

        using var messageHandler2 = new FakeHttpMessageHandler((request, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        }));

        using var httpClient1 = new HttpClient(messageHandler1);
        httpClient1.BaseAddress = new Uri("https://testendpoint1.com");

        using var httpClient2 = new HttpClient(messageHandler2);
        httpClient2.BaseAddress = new Uri("https://testendpoint2.com");

        const string HttpClientName1 = $"{PluginConfig.HttpClientNamePrefix}TestPlugin1";
        _httpClientFactory.CreateClient(HttpClientName1).Returns(httpClient1);

        const string HttpClientName2 = $"{PluginConfig.HttpClientNamePrefix}TestPlugin2";
        _httpClientFactory.CreateClient(HttpClientName2).Returns(httpClient2);

        _ = _mockTemplateProvider.GetShellDescriptorsTemplateAsync(Arg.Any<CancellationToken>()).Returns(template);

        // Act
        var response = await _client.GetAsync($"/shell-descriptors/{AasId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var shellDescriptorResponse = json.ToString();
        var expectedShellDescriptor = TestData.CreateShellDescriptor();
        Assert.Equal(shellDescriptorResponse, expectedShellDescriptor);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_WithNotFound_Returns404Async()
    {
        const string AasId = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";

        _ = _mockTemplateProvider.GetShellDescriptorsTemplateAsync(Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync($"/shell-descriptors/{AasId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_WithInternalServerError_Returns500Async()
    {
        const string AasId = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";

        _ = _mockTemplateProvider.GetShellDescriptorsTemplateAsync(Arg.Any<CancellationToken>()).Throws(new ResponseParsingException());

        var response = await _client.GetAsync($"/shell-descriptors/{AasId}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_WhenIdentifierIsInValid_Returns400Async()
    {
        const string AasId = "in valid";

        var response = await _client.GetAsync($"/shell-descriptors/{AasId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}

