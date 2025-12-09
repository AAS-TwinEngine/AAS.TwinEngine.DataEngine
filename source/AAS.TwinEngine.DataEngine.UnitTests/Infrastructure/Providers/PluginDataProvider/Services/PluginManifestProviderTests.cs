using System.Net;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginManifestProviderTests
{
    private readonly ILogger<PluginManifestProvider> _logger = Substitute.For<ILogger<PluginManifestProvider>>();
    private readonly ICreateClient _clientFactory = Substitute.For<ICreateClient>();
    private readonly IOptions<PluginConfig> _options;

    private readonly List<Plugin> _plugins =
    [
        new Plugin
        {
            PluginName = "TestPlugin",
            PluginUrl = new Uri("https://plugin.url")
}
    ];

    public PluginManifestProviderTests() => _options = Options.Create(new PluginConfig { Plugins = _plugins });

    private PluginManifestProvider CreateSut(HttpClient httpClient)
    {
        _clientFactory.CreateClient($"{PluginConfig.HttpClientNamePrefix}TestPlugin").Returns(httpClient);

        return new PluginManifestProvider(_logger, _options, _clientFactory);
    }

    private static string ValidJsonResponse => """
    {
      "supportedSemanticIds": [
        "0112/2///61987#ABP464#002_en",
        "0112/2///61987#ABA581#007"
      ],
      "capabilities": {
        "hasShellDescriptor": true,
        "hasAssetInformation": false
      }
    }
    """;

    [Fact]
    public async Task GetAllPluginManifestsAsync_ReturnsDeserializedManifest()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidJsonResponse)
        };

        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(response));
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://plugin.url")
        };

        var sut = CreateSut(httpClient);

        // Act
        var result = await sut.GetAllPluginManifestsAsync(CancellationToken.None);

        // Assert
        var manifest = Assert.Single(result);
        Assert.Equal("TestPlugin", manifest.PluginName);
        Assert.Equal(new Uri("https://plugin.url"), manifest.PluginUrl);
        Assert.Contains("0112/2///61987#ABP464#002_en", manifest.SupportedSemanticIds);
        Assert.True(manifest.Capabilities.HasShellDescriptor);
        Assert.False(manifest.Capabilities.HasAssetInformation);
    }

    [Fact]
    public async Task GetAllPluginManifestsAsync_WhenTaskCanceled_ThrowsRequestTimeoutException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler((_, _) => throw new TaskCanceledException("timeout"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://plugin.url") };

        var sut = CreateSut(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<RequestTimeoutException>(() =>
            sut.GetAllPluginManifestsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAllPluginManifestsAsync_WhenHttpRequestFails_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("error"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://plugin.url") };

        var sut = CreateSut(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.GetAllPluginManifestsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAllPluginManifestsAsync_WithInvalidJson_ThrowsResponseParsingException()
    {
        // Arrange
        var invalidJson = "this is not json!";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(invalidJson)
        };

        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(response));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://plugin.url") };

        var sut = CreateSut(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<ResponseParsingException>(() =>
            sut.GetAllPluginManifestsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAllPluginManifestsAsync_WhenPluginListIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        var emptyOptions = Options.Create(new PluginConfig { Plugins = [] });

        var sut = new PluginManifestProvider(_logger, emptyOptions, _clientFactory);

        // Act
        var result = await sut.GetAllPluginManifestsAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }
}
