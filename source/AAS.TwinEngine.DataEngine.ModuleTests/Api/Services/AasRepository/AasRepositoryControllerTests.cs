using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasEnvironment.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Shared.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.ModuleTests.Common;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.AasRepository;

public abstract class AasRepositoryControllerTests : IDisposable
{
    private readonly ConfigTestFactory _factory;
    private readonly ITemplateProvider _mockTemplateProvider;
    private readonly IFileContentProvider _fileContentProvider;
    private readonly ISubmodelRepositoryService _mockSubmodelRepositoryService;
    private readonly HttpClient _client;
    private readonly ICreateClient _httpClientFactory;

    protected AasRepositoryControllerTests(string configDir)
    {
        _mockTemplateProvider = Substitute.For<ITemplateProvider>();
        _fileContentProvider = Substitute.For<IFileContentProvider>();
        _mockSubmodelRepositoryService = Substitute.For<ISubmodelRepositoryService>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();
        var mockPluginManifestConflictHandler = Substitute.For<IPluginManifestConflictHandler>();
        _httpClientFactory = Substitute.For<ICreateClient>();

        _factory = new ConfigTestFactory(configDir, services =>
        {
            _ = services.AddSingleton(mockPluginManifestProvider);
            _ = services.AddSingleton(mockPluginManifestConflictHandler);
            _ = services.AddSingleton(_httpClientFactory);
            _ = services.AddSingleton(_mockTemplateProvider);
            _ = services.AddSingleton(_fileContentProvider);
            _ = services.AddSingleton(_mockSubmodelRepositoryService);
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
    public async Task GetShellByIdAsync_ReturnsOkAsync()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1NjgvdGVzdC9hYXM=";
        var mockShellTemplate = TestData.CreateShellTemplate();
        var mockAssetInformationTemplate = TestData.CreateAssetInformationTemplate();
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePluginResponseForAssetinformation())
        }));

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://testendpoint.com");

        const string HttpClientName = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName).Returns(httpClient);

        _ = _mockTemplateProvider.GetShellTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockShellTemplate);

        _ = _mockTemplateProvider.GetAssetInformationTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockAssetInformationTemplate);

        // Act
        var response = await _client.GetAsync($"/shells/{AasIdentifier}");

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
        var expectedProductId = TestData.GetProductIdFromRule(AasIdentifier.DecodeBase64Url(), 6);
        Assert.Equal(productId, expectedProductId);
    }

    [Fact]
    public async Task GetShellByIdAsync_ReturnsInternalServerErrorAsync_WhenErrorWhileExtractionOfProductIdAsync()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFz";
        var mockShellTemplate = TestData.CreateShellTemplate();
        var mockAssetInformationTemplate = TestData.CreateAssetInformationTemplate();
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(TestData.CreatePluginResponseForAssetinformation())
        }));

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://testendpoint.com");

        var httpClientName = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(httpClientName).Returns(httpClient);

        _ = _mockTemplateProvider.GetShellTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockShellTemplate);

        _ = _mockTemplateProvider.GetAssetInformationTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockAssetInformationTemplate);

        // Act
        var response = await _client.GetAsync($"/shells/{AasIdentifier}");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
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

        const string HttpClientName = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
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
        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodel-refs?limit=5");

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

        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodel-refs?limit=5&cursor=bmV4dDEyMw==");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_WhenIdentifierIsInValid_Returns400Async()
    {
        const string AasIdentifier = "in valid";

        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodel-refs?limit=-1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #region Identifier Validation Tests

    [Theory]
    [InlineData("not-valid-base64!!!")]
    [InlineData("invalid!!base64")]
    public async Task GetShellById_InvalidBase64_Returns400BadRequestAsync(string invalidBase64)
    {
        var response = await _client.GetAsync($"/shells/{invalidBase64}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("<img onerror=alert('xss')>")]
    [InlineData("'; DROP TABLE shells--")]
    public async Task GetShellById_MaliciousPattern_Returns400BadRequestAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/shells/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("vbscript:msgbox('xss')")]
    [InlineData("file:///etc/passwd")]
    public async Task GetAssetInformation_MaliciousPattern_Returns400BadRequesAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/shells/{encoded}/asset-information");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("invalid!!")]
    public async Task GetSubmodelRefs_InvalidBase64_Returns400BadRequestAsync(string invalidBase64)
    {
        var response = await _client.GetAsync($"/shells/{invalidBase64}/submodel-refs");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("https://example.com/shells/shell123")]
    [InlineData("urn:uuid:test-123")]
    public async Task GetShellById_ValidIdentifier_DoesNotReturn400Async(string validId)
    {
        var encoded = EncodeBase64Url(validId);
        _ = _mockTemplateProvider.GetShellTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync($"/shells/{encoded}");

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GET /shells (GetShellsByAssetId)

    [Fact]
    public async Task GetShellsAsync_WithNoAssetIds_ReturnsOkWithAllShellsAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForShellDescriptors());
        SetupTemplateProvider();

        var response = await _client.GetAsync("/shells");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetShellsAsync_WithValidAssetId_ReturnsOkWithMatchingShellsAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForShellDescriptors());
        SetupTemplateProvider();

        var specificAssetId = """{"name":"SerialNumber","value":"SN-4711"}""";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(specificAssetId));

        var response = await _client.GetAsync($"/shells?assetIds={encoded}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public async Task GetShellsAsync_WithMultipleAssetIds_ReturnsOkAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForShellDescriptors());
        SetupTemplateProvider();

        var id1 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("""{"name":"SerialNumber","value":"SN-4711"}"""));
        var id2 = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("""{"name":"BatchId","value":"B-001"}"""));

        var response = await _client.GetAsync($"/shells?assetIds={id1}&assetIds={id2}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        Assert.NotNull(json["result"]);
    }

    [Fact]
    public async Task GetShellsAsync_WithPagination_LimitsResultsAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForShellDescriptorsFilterByIdShort());
        SetupTemplateProvider();

        var specificAssetId = """{"name":"SerialNumber","value":"SN-4711"}""";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(specificAssetId));

        var response = await _client.GetAsync($"/shells?assetIds={encoded}&limit=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetShellsAsync_WithNoMatchingResults_ReturnsEmptyResultAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForShellDescriptorsEmpty());
        SetupTemplateProvider();

        var specificAssetId = """{"name":"SerialNumber","value":"non-existent"}""";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(specificAssetId));

        var response = await _client.GetAsync($"/shells?assetIds={encoded}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetShellsAsync_WithInvalidBase64AssetId_Returns400Async()
    {
        var response = await _client.GetAsync("/shells?assetIds=not-valid-base64!!!");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetShellsAsync_WithInvalidJsonAssetId_Returns400Async()
    {
        var invalidJson = "not json at all";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(invalidJson));

        var response = await _client.GetAsync($"/shells?assetIds={encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetShellsAsync_WithMissingAssetIdName_Returns400Async()
    {
        var json = """{"value":"SN-4711"}""";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(json));

        var response = await _client.GetAsync($"/shells?assetIds={encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetShellsAsync_WithMissingAssetIdValue_Returns400Async()
    {
        var json = """{"name":"SerialNumber"}""";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(json));

        var response = await _client.GetAsync($"/shells?assetIds={encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetShellsAsync_WithNegativeLimit_Returns400Async()
    {
        var response = await _client.GetAsync("/shells?limit=-1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetShellsAsync_WithIdShort_ReturnsOnlyMatchingShellAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForShellDescriptorsFilterByIdShort());
        SetupTemplateProvider();

        var response = await _client.GetAsync("/shells?idShort=Product1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        _ = Assert.Single(result);
    }

    [Fact]
    public async Task GetShellsAsync_WithIdShortNoMatch_ReturnsEmptyResultAsync()
    {
        SetupPluginHttpClient(TestData.CreatePluginResponseForShellDescriptorsEmpty());
        SetupTemplateProvider();

        var response = await _client.GetAsync("/shells?idShort=NonExistentShell");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        var result = json["result"]?.AsArray();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    private void SetupPluginHttpClient(string pluginResponse)
    {
        const string HttpClientName1 = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";

        _httpClientFactory.CreateClient(HttpClientName1)
            .Returns(_ => CreateHttpClient(pluginResponse));
    }

    private static HttpClient CreateHttpClient(string pluginResponse)
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(pluginResponse)
            }));

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://testendpoint.com")
        };
    }

    private void SetupTemplateProvider()
    {
        _ = _mockTemplateProvider
            .GetShellTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var assetInformation = new AssetInformation(AssetKind.Instance)
                {
                    SpecificAssetIds =
                    [
                        new SpecificAssetId("SerialNumber", "Test"),
                        new SpecificAssetId("manufacturer", "ABC")
                    ]
                };

                return new AssetAdministrationShell(
                    callInfo.ArgAt<string>(0),
                    assetInformation)
                {
                    Submodels = []
                };
            });
    }

    [Fact]
    public async Task GetThumbnailAsync_ShouldReturn200OKWithStream_WhenThumbnailExists()
    {
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";

        var messageHandler = new FakeHttpMessageHandler((request, token) =>
        {
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(TestData.CreatePluginResponseForAssetinformation())
            };
            return Task.FromResult(httpResponse);
        });

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://testendpoint.com");

        const string HttpClientName = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName).Returns(httpClient);

        var expectedAssetInformation = TestData.CreateAssetInformationTemplate();
        _ = _mockTemplateProvider.GetAssetInformationTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expectedAssetInformation);

        var stream = new MemoryStream("test-bytes"u8.ToArray());
        var fileContentResponse = new FileContentResponse(stream);
        _ = _fileContentProvider.GetFileContentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(fileContentResponse);

        var response = await _client.GetAsync($"/shells/{AasIdentifier}/asset-information/thumbnail");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedAssetInformation.DefaultThumbnail.ContentType, response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("test-bytes"u8.ToArray(), bytes);
    }

    [Fact]
    public async Task GetThumbnailAsync_ShouldReturn404_WhenThumbnailIsMissing()
    {
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";

        var mockAssetInformationTemplate = new AssetInformation(
            AssetKind.Instance,
            "https://example.com/ids/asset/123",
            [],
            defaultThumbnail: null
        );

        var messageHandler = new FakeHttpMessageHandler((request, token) =>
        {
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "assetKind": "Type",
                      "globalAssetId": "https://example.com/ids/asset/123",
                      "specificAssetIds": [],
                      "defaultThumbnail": null
                    }
                    """)
            };
            return Task.FromResult(httpResponse);
        });

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://testendpoint.com");

        const string HttpClientName = $"{HttpClientNames.PluginDataProviderPrefix}TestPlugin1";
        _ = _httpClientFactory.CreateClient(HttpClientName).Returns(httpClient);

        _ = _mockTemplateProvider.GetAssetInformationTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockAssetInformationTemplate);

        var response = await _client.GetAsync($"/shells/{AasIdentifier}/asset-information/thumbnail");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelByAasIdAsync_ReturnsOkAsync()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        const string submodelKey = "Nameplate";
        const string productId = "1170_1160_3052_6568";
        var requestedSubmodelId = $"https://mm-software.com/submodel/{productId}/{submodelKey}";
        var encodedSubmodelId = EncodeBase64Url(requestedSubmodelId);

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, submodelKey)], null)]);

        _ = _mockSubmodelRepositoryService
            .GetSubmodelAsync(requestedSubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new Submodel(id: requestedSubmodelId));

        // Act
        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodels/{encodedSubmodelId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
    }

    [Fact]
    public async Task GetSubmodelByAasIdAsync_WhenSubmodelNotInAas_Returns404Async()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        const string productId = "1170_1160_3052_6568";
        var requestedSubmodelId = $"https://mm-software.com/submodel/{productId}/Missing";
        var encodedSubmodelId = EncodeBase64Url(requestedSubmodelId);

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, "Other")], null)]);

        // Act
        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodels/{encodedSubmodelId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelElementsByAasIdAsync_ReturnsOkAsync()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        const string submodelKey = "Nameplate";
        const string productId = "1170_1160_3052_6568";
        var requestedSubmodelId = $"https://mm-software.com/submodel/{productId}/{submodelKey}";
        var encodedSubmodelId = EncodeBase64Url(requestedSubmodelId);

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, submodelKey)], null)]);

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(requestedSubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new SubmodelElementsPage { PagingMetaData = new PagingMetaData { Cursor = null }, Result = [] });

        // Act
        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodels/{encodedSubmodelId}/submodel-elements");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
        Assert.True(json.ContainsKey("result"));
    }

    [Fact]
    public async Task GetSubmodelElementByAasIdAsync_ReturnsOkAsync()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        const string submodelKey = "Nameplate";
        const string productId = "1170_1160_3052_6568";
        var requestedSubmodelId = $"https://mm-software.com/submodel/{productId}/{submodelKey}";
        const string IdShortPath = "ManufacturerName";
        var encodedSubmodelId = EncodeBase64Url(requestedSubmodelId);

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, submodelKey)], null)]);

        _ = _mockSubmodelRepositoryService
            .GetSubmodelElementAsync(requestedSubmodelId, IdShortPath, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new Property(DataTypeDefXsd.String) { IdShort = IdShortPath });

        // Act
        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodels/{encodedSubmodelId}/submodel-elements/{IdShortPath}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
    }

    [Fact]
    public async Task GetFileByPathByAasIdAsync_ReturnsAttachmentStreamAsync()
    {
        // Arrange
        const string AasIdentifier = "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg=";
        const string submodelKey = "Nameplate";
        const string productId = "1170_1160_3052_6568";
        var requestedSubmodelId = $"https://mm-software.com/submodel/{productId}/{submodelKey}";
        const string idShortPath = "Thumbnail";
        var encodedSubmodelId = EncodeBase64Url(requestedSubmodelId);
        var fileBytes = Encoding.UTF8.GetBytes("fake-image-bytes");

        _ = _mockTemplateProvider.GetSubmodelRefByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, submodelKey)], null)]);

        _ = _mockSubmodelRepositoryService
            .GetFileAttachmentAsync(requestedSubmodelId, idShortPath, Arg.Any<CancellationToken>())
            .Returns(new FileAttachmentResult(new MemoryStream(fileBytes), "image/png", "logo.png", 100 * 1024 * 1024));

        // Act
        var response = await _client.GetAsync($"/shells/{AasIdentifier}/submodels/{encodedSubmodelId}/submodel-elements/{idShortPath}/attachment");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(fileBytes, body);
        Assert.Contains("logo.png", response.Content.Headers.ContentDisposition?.ToString(), StringComparison.Ordinal);
        await _mockSubmodelRepositoryService.Received(1).GetFileAttachmentAsync(requestedSubmodelId, idShortPath, Arg.Any<CancellationToken>());
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
}

public class AasRepositoryControllerTestsV1Config() : AasRepositoryControllerTests("v1-config");

public class AasRepositoryControllerTestsV2Config() : AasRepositoryControllerTests("v2-config");

public class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
