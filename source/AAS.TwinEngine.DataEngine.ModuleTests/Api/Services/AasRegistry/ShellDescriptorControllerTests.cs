using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.ModuleTests.Common;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.AasRegistry;

public abstract class ShellDescriptorControllerTests : IDisposable
{
    private readonly ConfigTestFactory _factory;
    private readonly ITemplateProvider _mockTemplateProvider;
    private readonly HttpClient _client;
    private readonly ICreateClient _httpClientFactory;

    protected ShellDescriptorControllerTests(string configDir)
    {
        _mockTemplateProvider = Substitute.For<ITemplateProvider>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();
        var mockPluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
        _httpClientFactory = Substitute.For<ICreateClient>();

        _factory = new ConfigTestFactory(configDir, services =>
        {
            _ = services.AddSingleton(_httpClientFactory);
            _ = services.AddSingleton(mockPluginManifestProvider);
            _ = services.AddSingleton(_mockTemplateProvider);
            _ = services.AddSingleton(mockPluginManifestConflictHandler);
        });

        _client = _factory.CreateClient();
        _ = mockPluginManifestConflictHandler.Manifests.Returns(TestData.CreatePluginManifests());
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

    private static string EncodeBase64Url(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(plainText);
        return WebEncoders.Base64UrlEncode(bytes);
    }
}

public class ShellDescriptorControllerTestsV1Config() : ShellDescriptorControllerTests("v1-config");

public class ShellDescriptorControllerTestsV2Config() : ShellDescriptorControllerTests("v2-config");

public class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}

