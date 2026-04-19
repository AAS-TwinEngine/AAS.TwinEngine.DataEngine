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

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.SubmodelRepository;

public abstract class SubmodelRepositoryControllerTestsBase : IDisposable
{
    private readonly ConfigTestFactory _factory;
    private readonly ITemplateProvider _mockTemplateProvider;
    private readonly HttpClient _client;
    private readonly ICreateClient _httpClientFactory;

    protected SubmodelRepositoryControllerTestsBase(string configDir)
    {
        _mockTemplateProvider = Substitute.For<ITemplateProvider>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();
        var mockPluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
        _httpClientFactory = Substitute.For<ICreateClient>();

        _factory = new ConfigTestFactory(configDir, services =>
        {
            _ = services.AddSingleton(_httpClientFactory);
            _ = services.AddSingleton(_mockTemplateProvider);
            _ = services.AddSingleton(mockPluginManifestProvider);
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

        const string HttpClientNamePlugin1 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin1).Returns(httpClientPlugin1);

        const string HttpClientNamePlugin2 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin2";
        _ = _httpClientFactory.CreateClient(HttpClientNamePlugin2).Returns(httpClientPlugin2);

        const string SubmodelId = "Q29udGFjdEluZm9ybWF0aW9u";
        var mockSubmodel = TestData.CreateSubmodel();

        _ = _mockTemplateProvider.GetSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockSubmodel);

        // Act
        var response = await _client.GetAsync($"/submodels/{SubmodelId}");

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
        _ = TestData.CreatePluginResponseForSubmodelElement();

        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePluginResponseForSubmodelElement())
        }));

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://testendpoint.com");

        const string HttpClientName = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName).Returns(httpClient);

        _ = _mockTemplateProvider.GetSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockSubmodel);

        // Act
        var response = await _client.GetAsync(CreateSubmodelElementPath(SubmodelId, IdShortPath));

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

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData(@"..\..\windows\system32")]
    [InlineData("element/../otherElement")]
    [InlineData("%2e%2e/config")]
    public async Task GetSubmodelElement_PathTraversalInIdShortPath_Returns400BadRequestAsync(string maliciousIdShortPath)
    {
        var validSubmodelId = EncodeBase64Url("https://example.com/submodels/test");

        var response = await _client.GetAsync(CreateSubmodelElementPath(validSubmodelId, maliciousIdShortPath));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<img onerror=alert('xss')>")]
    [InlineData("element<script>alert(1)</script>")]
    [InlineData("<svg/onload=alert('xss')>")]
    public async Task GetSubmodelElement_XssInIdShortPath_Returns400BadRequestAsync(string maliciousIdShortPath)
    {
        var validSubmodelId = EncodeBase64Url("https://example.com/submodels/test");

        var response = await _client.GetAsync(CreateSubmodelElementPath(validSubmodelId, maliciousIdShortPath));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("element'; DROP TABLE--")]
    [InlineData("1 UNION SELECT *")]
    [InlineData("admin'--")]
    public async Task GetSubmodelElement_SqlInjectionInIdShortPath_Returns400BadRequestAsync(string maliciousIdShortPath)
    {
        var validSubmodelId = EncodeBase64Url("https://example.com/submodels/test");

        var response = await _client.GetAsync(CreateSubmodelElementPath(validSubmodelId, maliciousIdShortPath));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("data:text/html,<script>")]
    [InlineData("file:///etc/passwd")]
    [InlineData("vbscript:msgbox('xss')")]
    public async Task GetSubmodelElement_DangerousProtocolInIdShortPath_Returns400BadRequestAsync(string maliciousIdShortPath)
    {
        var validSubmodelId = EncodeBase64Url("https://example.com/submodels/test");

        var response = await _client.GetAsync(CreateSubmodelElementPath(validSubmodelId, maliciousIdShortPath));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("element with spaces")]
    [InlineData("element/slash")]
    [InlineData("element\\backslash")]
    [InlineData("element|pipe")]
    [InlineData("element;semicolon")]
    public async Task GetSubmodelElement_InvalidCharactersInIdShortPath_Returns400BadRequestAsync(string invalidIdShortPath)
    {
        var validSubmodelId = EncodeBase64Url("https://example.com/submodels/test");

        var response = await _client.GetAsync(CreateSubmodelElementPath(validSubmodelId, invalidIdShortPath));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("ContactInformation1")]
    [InlineData("ManufacturerName")]
    [InlineData("element.subelement.property")]
    [InlineData("list[0]")]
    [InlineData("element[3].property")]
    [InlineData("collection_item-name")]
    public async Task GetSubmodelElement_ValidIdShortPath_DoesNotReturn400Async(string validIdShortPath)
    {
        var validSubmodelId = EncodeBase64Url("https://example.com/submodels/test");
        _ = _mockTemplateProvider.GetSubmodelTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync(CreateSubmodelElementPath(validSubmodelId, validIdShortPath));

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("not!!valid")]
    [InlineData("invalid base64")]
    public async Task GetSubmodel_InvalidBase64_Returns400BadRequestAsync(string invalidBase64)
    {
        var response = await _client.GetAsync($"/submodels/{invalidBase64}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("<svg/onload=alert('xss')>")]
    [InlineData("1 UNION SELECT * FROM submodels")]
    [InlineData("javascript:alert(1)")]
    public async Task GetSubmodel_MaliciousPattern_Returns400BadRequestAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/submodels/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private static string CreateSubmodelElementPath(string submodelIdentifier, string idShortPath)
        => $"/submodels/{submodelIdentifier}/submodel-elements/{Uri.EscapeDataString(idShortPath)}";
}

public class SubmodelRepositoryControllerTests_V1Config : SubmodelRepositoryControllerTestsBase
{
    public SubmodelRepositoryControllerTests_V1Config() : base("v1-config") { }
}

public class SubmodelRepositoryControllerTests_V2Config : SubmodelRepositoryControllerTestsBase
{
    public SubmodelRepositoryControllerTests_V2Config() : base("v2-config") { }
}

public class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
