using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.ModuleTests.Common;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.AasRegistry;

public abstract class ShellDescriptorControllerTests : IDisposable
{
    private readonly ConfigTestFactory _factory;
    private readonly ITemplateProvider _mockTemplateProvider;
    private readonly ISubmodelDescriptorProvider _mockSubmodelDescriptorProvider;
    private readonly HttpClient _client;
    private readonly ICreateClient _httpClientFactory;
    private readonly IPluginManifestConflictHandler _mockPluginManifestConflictHandler;

    protected ShellDescriptorControllerTests(string configDir)
    {
        _mockTemplateProvider = Substitute.For<ITemplateProvider>();
        _mockSubmodelDescriptorProvider = Substitute.For<ISubmodelDescriptorProvider>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();
        _mockPluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
        _httpClientFactory = Substitute.For<ICreateClient>();

        _factory = new ConfigTestFactory(configDir, services =>
        {
            _ = services.AddSingleton(_httpClientFactory);
            _ = services.AddSingleton(mockPluginManifestProvider);
            _ = services.AddSingleton(_mockTemplateProvider);
            _ = services.AddSingleton(_mockSubmodelDescriptorProvider);
            _ = services.AddSingleton(_mockPluginManifestConflictHandler);
        });

        _client = _factory.CreateClient();
        SetPluginManifests(TestData.CreatePluginManifests());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_ReturnsOkAsync()
    {
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
        const string HttpClientNamePlugin1 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin1).Returns(httpClientPlugin1);
        const string HttpClientNamePlugin2 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin2).Returns(httpClientPlugin2);
        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(_ => TestData.CreateShellDescriptorsTemplate());

        var response = await _client.GetAsync("/shell-descriptors?limit=2&cursor=bmV4dDEyMw==");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var shellDescriptorsResponse = json.ToString();
        var expectedShellDescriptors = TestData.CreateShellDescriptors();
        Assert.Equal(shellDescriptorsResponse, expectedShellDescriptors);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithAssetKindAndAssetTypeAndNoCapablePlugin_Returns501Async()
    {
        var assetType = "asset-type-value".EncodeBase64Url();
        var response = await _client.GetAsync(new Uri($"/shell-descriptors?limit=2&cursor=bmV4dDEyMw==&assetKind=Instance&assetType={assetType}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Functionality Not Supported", content, StringComparison.OrdinalIgnoreCase);
        _ = _httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithMatchingAssetKindAndAssetType_ReturnsOkAsync()
    {
        SetPluginManifests(TestData.CreatePluginManifestsWithShellDescriptorCapabilities(
            ("TestPlugin1", true, true),
            ("TestPlugin2", true, true)));

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
        const string HttpClientNamePlugin1 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin1).Returns(httpClientPlugin1);
        const string HttpClientNamePlugin2 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin2).Returns(httpClientPlugin2);
        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(_ => TestData.CreateShellDescriptorsTemplate());

        // Template has assetKind="Type" and assetType="Type" → filter by Type matches all descriptors.
        var assetType = "Type".EncodeBase64Url();
        var response = await _client.GetAsync(new Uri($"/shell-descriptors?limit=2&cursor=bmV4dDEyMw==&assetKind=Type&assetType={assetType}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithMixedAssetKindTypeFilterCapabilities_SkipsUnsupportedPluginsAndPreservesManifestOrderAsync()
    {
        SetPluginManifests(TestData.CreatePluginManifestsWithShellDescriptorCapabilities(
            ("TestPluginWithoutFilter", true, false),
            ("TestPlugin2", true, true),
            ("TestPluginWithoutShellDescriptor", false, true),
            ("TestPlugin3", true, true)));

        var pluginCalls = new List<(string PluginName, HttpRequestMessage Request)>();
        using var messageHandlerPlugin2 = new FakeHttpMessageHandler((request, _) =>
        {
            pluginCalls.Add(("TestPlugin2", CloneRequest(request)));
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.CreatePluginResponseForShellDescriptorsByIds("shell-from-plugin-2"))
            });
        });
        using var messageHandlerPlugin3 = new FakeHttpMessageHandler((request, _) =>
        {
            pluginCalls.Add(("TestPlugin3", CloneRequest(request)));
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.CreatePluginResponseForShellDescriptorsByIds("shell-from-plugin-3a", "shell-from-plugin-3b"))
            });
        });
        using var httpClientPlugin2 = new HttpClient(messageHandlerPlugin2);
        httpClientPlugin2.BaseAddress = new Uri("https://testendpoint2.com");
        using var httpClientPlugin3 = new HttpClient(messageHandlerPlugin3);
        httpClientPlugin3.BaseAddress = new Uri("https://testendpoint3.com");
        _ = _httpClientFactory.CreateClient($"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2").Returns(httpClientPlugin2);
        _ = _httpClientFactory.CreateClient($"{HttpClientNames.PluginDataProviderPrefix}TestPlugin3").Returns(httpClientPlugin3);
        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(_ => TestData.CreateShellDescriptorsTemplate());

        var assetType = "Type".EncodeBase64Url();
        var response = await _client.GetAsync(new Uri($"/shell-descriptors?limit=3&cursor=bmV4dDEyMw==&assetKind=Instance&assetType={assetType}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["TestPlugin2", "TestPlugin3"], pluginCalls.Select(call => call.PluginName));
        Assert.Equal([
            "https://example.com/ids/aas/shell-from-plugin-2",
            "https://example.com/ids/aas/shell-from-plugin-3a",
            "https://example.com/ids/aas/shell-from-plugin-3b"
        ], await ReadShellDescriptorIdsAsync(response));

        var firstQuery = QueryHelpers.ParseQuery(pluginCalls[0].Request.RequestUri!.Query);
        Assert.Equal("3", firstQuery["limit"]);
        Assert.Equal("bmV4dDEyMw==", firstQuery["cursor"]);
        Assert.Equal("Instance", pluginCalls[0].Request.Headers.GetValues("aastwinengine-assetkind").Single());
        Assert.Equal("Type", pluginCalls[0].Request.Headers.GetValues("aastwinengine-assettype").Single());

        var secondQuery = QueryHelpers.ParseQuery(pluginCalls[1].Request.RequestUri!.Query);
        Assert.Equal("2", secondQuery["limit"]);
        Assert.False(secondQuery.ContainsKey("cursor"));
        Assert.Equal("Instance", pluginCalls[1].Request.Headers.GetValues("aastwinengine-assetkind").Single());
        Assert.Equal("Type", pluginCalls[1].Request.Headers.GetValues("aastwinengine-assettype").Single());
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WhenFirstCapablePluginSatisfiesLimit_DoesNotCallLaterCapablePluginsAsync()
    {
        SetPluginManifests(TestData.CreatePluginManifestsWithShellDescriptorCapabilities(
            ("TestPlugin1", true, true),
            ("TestPlugin2", true, true)));

        var calledPlugins = new List<string>();
        using var messageHandlerPlugin1 = new FakeHttpMessageHandler((_, _) =>
        {
            calledPlugins.Add("TestPlugin1");
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(TestData.CreatePluginResponseForShellDescriptorsByIds("shell-from-plugin-1a", "shell-from-plugin-1b"))
            });
        });
        using var messageHandlerPlugin2 = new FakeHttpMessageHandler((_, _) =>
        {
            calledPlugins.Add("TestPlugin2");
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
        });
        using var httpClientPlugin1 = new HttpClient(messageHandlerPlugin1);
        httpClientPlugin1.BaseAddress = new Uri("https://testendpoint1.com");
        using var httpClientPlugin2 = new HttpClient(messageHandlerPlugin2);
        httpClientPlugin2.BaseAddress = new Uri("https://testendpoint2.com");
        _ = _httpClientFactory.CreateClient($"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1").Returns(httpClientPlugin1);
        _ = _httpClientFactory.CreateClient($"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2").Returns(httpClientPlugin2);
        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(_ => TestData.CreateShellDescriptorsTemplate());

        var assetType = "Type".EncodeBase64Url();
        var response = await _client.GetAsync(new Uri($"/shell-descriptors?limit=2&assetKind=Type&assetType={assetType}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["TestPlugin1"], calledPlugins);
        Assert.Equal([
            "https://example.com/ids/aas/shell-from-plugin-1a",
            "https://example.com/ids/aas/shell-from-plugin-1b"
        ], await ReadShellDescriptorIdsAsync(response));
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithInvalidAssetType_Returns400Async()
    {
        var response = await _client.GetAsync("/shell-descriptors?limit=2&cursor=bmV4dDEyMw==&assetKind=Instance&assetType=invalid value");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WhenOneDescriptorFails_ReturnsRemainingDescriptorsAsync()
    {
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

        const string HttpClientNamePlugin1 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin1).Returns(httpClientPlugin1);

        const string HttpClientNamePlugin2 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin2).Returns(httpClientPlugin2);

        var validTemplate = TestData.CreateShellDescriptorsTemplate();

        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new ResourceNotFoundException(),
                _ => validTemplate);

        var response = await _client.GetAsync("/shell-descriptors?limit=2&cursor=bmV4dDEyMw==");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);

        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        _ = Assert.Single(result);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithNagetiveLimit_Returns400Async()
    {
        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync("/shell-descriptors?limit=-1&cursor=bmV4dDEyMw==");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithInValidCursor_Returns400Async()
    {
        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync("/shell-descriptors?limit=4&cursor=invalid cursor");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithNotFound_Returns404Async()
    {
        using var messageHandlerPlugin1 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        }));
        using var messageHandlerPlugin2 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        }));
        using var httpClientPlugin1 = new HttpClient(messageHandlerPlugin1);
        httpClientPlugin1.BaseAddress = new Uri("https://testendpoint1.com");
        using var httpClientPlugin2 = new HttpClient(messageHandlerPlugin2);
        httpClientPlugin2.BaseAddress = new Uri("https://testendpoint2.com");
        const string HttpClientNamePlugin1 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin1).Returns(httpClientPlugin1);
        const string HttpClientNamePlugin2 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin2).Returns(httpClientPlugin2);

        var response = await _client.GetAsync("/shell-descriptors?limit=5&cursor=bmV4dDEyMw==");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllShellDescriptorsAsync_WithInternalServerError_Returns500Async()
    {
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
        const string HttpClientNamePlugin1 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin1).Returns(httpClientPlugin1);
        const string HttpClientNamePlugin2 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin2).Returns(httpClientPlugin2);
        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResponseParsingException());

        var response = await _client.GetAsync("/shell-descriptors?limit=5&cursor=bmV4dDEyMw==");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_ReturnsOkAsync()
    {
        const string AasId = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
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
        const string HttpClientName1 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName1).Returns(httpClient1);
        const string HttpClientName2 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2";
        _ = _httpClientFactory.CreateClient(HttpClientName2).Returns(httpClient2);
        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(_ => TestData.CreateShellDescriptorsTemplate());

        var response = await _client.GetAsync($"/shell-descriptors/{AasId}");

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

        using var messageHandler1 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePlugin1ResponseForShellDescriptor())
        }));

        using var messageHandler2 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        }));

        using var httpClient1 = new HttpClient(messageHandler1);
        httpClient1.BaseAddress = new Uri("https://testendpoint1.com");

        using var httpClient2 = new HttpClient(messageHandler2);
        httpClient2.BaseAddress = new Uri("https://testendpoint2.com");

        const string HttpClientName1 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName1).Returns(httpClient1);

        const string HttpClientName2 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2";
        _ = _httpClientFactory.CreateClient(HttpClientName2).Returns(httpClient2);

        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync($"/shell-descriptors/{AasId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetShellDescriptorByIdAsync_WithInternalServerError_Returns500Async()
    {
        const string AasId = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";

        using var messageHandler1 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePlugin1ResponseForShellDescriptor())
        }));

        using var messageHandler2 = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        }));

        using var httpClient1 = new HttpClient(messageHandler1);
        httpClient1.BaseAddress = new Uri("https://testendpoint1.com");

        using var httpClient2 = new HttpClient(messageHandler2);
        httpClient2.BaseAddress = new Uri("https://testendpoint2.com");

        const string HttpClientName1 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName1).Returns(httpClient1);

        const string HttpClientName2 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2";
        _ = _httpClientFactory.CreateClient(HttpClientName2).Returns(httpClient2);

        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResponseParsingException());

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

    #region Identifier Validation Tests

    [Theory]
    [InlineData("not-valid-base64!!!")]
    [InlineData("invalid!!base64")]
    public async Task GetShellDescriptorById_InvalidBase64_Returns400BadRequestAsync(string invalidBase64)
    {
        var response = await _client.GetAsync($"/shell-descriptors/{invalidBase64}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid User Input", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<img onerror=alert('xss')>")]
    [InlineData("<svg/onload=alert('xss')>")]
    public async Task GetShellDescriptorById_XssInDecodedId_Returns400BadRequestAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/shell-descriptors/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid User Input", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DROP TABLE shells--")]
    [InlineData("1 UNION SELECT * FROM descriptors")]
    [InlineData("admin'; DELETE FROM shells--")]
    public async Task GetShellDescriptorById_SqlInjectionInDecodedId_Returns400BadRequestAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/shell-descriptors/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid User Input", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("%2e%2e/config")]
    [InlineData("..%2fconfig")]
    public async Task GetShellDescriptorById_PathTraversalInDecodedId_Returns400BadRequestAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/shell-descriptors/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid User Input", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>")]
    [InlineData("vbscript:msgbox('xss')")]
    [InlineData("file:///etc/passwd")]
    public async Task GetShellDescriptorById_DangerousProtocolInDecodedId_Returns400BadRequestAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/shell-descriptors/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid User Input", content, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("test\0value")]
    public async Task GetShellDescriptorById_NullByteInDecodedId_Returns400BadRequestAsync(string contentWithNullByte)
    {
        var encoded = EncodeBase64Url(contentWithNullByte);

        var response = await _client.GetAsync($"/shell-descriptors/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid User Input", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetShellDescriptorById_IdentifierExceedsMaxLength_Returns400BadRequestAsync()
    {
        var longIdentifier = "https://example.com/" + new string('a', 2050);
        var encoded = EncodeBase64Url(longIdentifier);

        var response = await _client.GetAsync($"/shell-descriptors/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("https://example.com/ids/aas/1170_1160_3052_6568")]
    [InlineData("https://admin-shell.io/idta/aas/ContactInformation/1/0")]
    [InlineData("urn:uuid:123e4567-e89b-12d3-a456-426614174000")]
    [InlineData("https://mm-software.com/submodel/test/Nameplate")]
    public async Task GetShellDescriptorById_ValidAasIdentifiers_DoesNotReturn400Async(string validIdentifier)
    {
        var encoded = EncodeBase64Url(validIdentifier);
        _ = _mockTemplateProvider.GetShellDescriptorTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                                 .Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync($"/shell-descriptors/{encoded}");

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    [Fact]
    public async Task GetAllSubmodelDescriptorsByAasIdAsync_ReturnsOkAsync()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        const string SubmodelKey = "Nameplate";
        const string ProductId = "1170_1160_3052_6568";
        const string SubmodelTemplateId = "https://admin-shell.io/idta/SubmodelTemplate/DigitalNameplate/3/0";
        var submodelId = $"https://mm-software.com/submodel/{ProductId}/{SubmodelKey}";

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            [
                new Reference(
                    ReferenceTypes.ModelReference,
                    [new Key(KeyTypes.Submodel, SubmodelKey)],
                    null)
            ]);

        _ = _mockSubmodelDescriptorProvider
            .GetDataForSubmodelDescriptorByIdAsync(SubmodelTemplateId, Arg.Any<CancellationToken>())
            .Returns(new SubmodelDescriptor { Id = submodelId, Endpoints = [] });

        // Act
        var response = await _client.GetAsync($"/shell-descriptors/{AasIdentifier}/submodel-descriptors");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        _ = Assert.Single(result);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByAasIdAsync_ReturnsOkAsync()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        const string SubmodelKey = "Nameplate";
        const string ProductId = "1170_1160_3052_6568";
        const string SubmodelTemplateId = "https://admin-shell.io/idta/SubmodelTemplate/DigitalNameplate/3/0";
        var submodelId = $"https://mm-software.com/submodel/{ProductId}/{SubmodelKey}";
        var encodedSubmodelId = EncodeBase64Url(submodelId);

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            [
                new Reference(
                    ReferenceTypes.ModelReference,
                    [new Key(KeyTypes.Submodel, SubmodelKey)],
                    null)
            ]);

        _ = _mockSubmodelDescriptorProvider
            .GetDataForSubmodelDescriptorByIdAsync(SubmodelTemplateId, Arg.Any<CancellationToken>())
            .Returns(new SubmodelDescriptor { Id = submodelId, Endpoints = [] });

        // Act
        var response = await _client.GetAsync($"/shell-descriptors/{AasIdentifier}/submodel-descriptors/{encodedSubmodelId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByAasIdAsync_WhenSubmodelNotInAas_Returns404Async()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        const string ProductId = "1170_1160_3052_6568";
        var requestedSubmodelId = $"https://mm-software.com/submodel/{ProductId}/Missing";
        var encodedSubmodelId = EncodeBase64Url(requestedSubmodelId);

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            [
                new Reference(
                    ReferenceTypes.ModelReference,
                    [new Key(KeyTypes.Submodel, "Other")],
                    null)
            ]);

        // Act
        var response = await _client.GetAsync($"/shell-descriptors/{AasIdentifier}/submodel-descriptors/{encodedSubmodelId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByAasIdAsync_WithDifferentCaseInSubmodelId_ReturnsNotFoundAsync()
    {
        // Arrange
        const string aasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        const string submodelKey = "Nameplate";
        const string productId = "1170_1160_3052_6568";
        const string submodelTemplateId = "https://admin-shell.io/idta/SubmodelTemplate/DigitalNameplate/3/0";
        var requestedSubmodelId = $"https://mm-software.com/submodel/{productId}/nameplate";
        var encodedSubmodelId = EncodeBase64Url(requestedSubmodelId);

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            [
                new Reference(
                    ReferenceTypes.ModelReference,
                    [new Key(KeyTypes.Submodel, submodelKey)],
                    null)
            ]);

        _ = _mockSubmodelDescriptorProvider
            .GetDataForSubmodelDescriptorByIdAsync(submodelTemplateId, Arg.Any<CancellationToken>())
            .Returns(new SubmodelDescriptor { Id = requestedSubmodelId, Endpoints = [] });

        // Act
        var response = await _client.GetAsync($"/shell-descriptors/{aasIdentifier}/submodel-descriptors/{encodedSubmodelId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string EncodeBase64Url(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(plainText);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private void SetPluginManifests(IReadOnlyList<PluginManifest> pluginManifests)
        => _mockPluginManifestConflictHandler.Manifests.Returns(pluginManifests);

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
        {
            _ = clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private static async Task<string[]> ReadShellDescriptorIdsAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);

        return [.. result.Select(item => item!["id"]!.GetValue<string>())];
    }
}

public class ShellDescriptorControllerTestsV1Config() : ShellDescriptorControllerTests("v1-config");

public class ShellDescriptorControllerTestsV2Config() : ShellDescriptorControllerTests("v2-config");

public class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}

