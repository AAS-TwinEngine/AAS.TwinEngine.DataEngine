using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;

using PluginDataProviderRepo = AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginDataProviderTests
{
    private readonly PluginDataProviderRepo.PluginDataProvider _sut;
    private readonly ICreateClient _httpClientFactory;

    private const string SimpleRequestSchema = """
                                               {
                                                 "$schema": "http://json-schema.org/draft-07/schema#",
                                                 "type":"object",
                                                 "properties": { "leaf": { "type":"string" }}
                                               }
                                               """;

    private const string SimpleResponse = """{ "leaf":"value" }""";

    public PluginDataProviderTests()
    {
        var logger = Substitute.For<ILogger<PluginDataProviderRepo.PluginDataProvider>>();
        _httpClientFactory = Substitute.For<ICreateClient>();
        _sut = new PluginDataProviderRepo.PluginDataProvider(logger, _httpClientFactory);
    }

    [Theory]
    [InlineData("https://testendpoint.com")]
    [InlineData("https://testendpoint.com/")]
    public async Task GetDataForSemanticIdsAsync_ReturnsValuesAsync(string endpoint)
    {
        HttpRequestMessage capturedRequest = null!;

        using var messageHandler = new FakeHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(SimpleResponse)
            });
        });

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri(endpoint);

        var httpClientName = $"{PluginConfig.HttpClientNamePrefix}PluginName";
        _httpClientFactory.CreateClient(httpClientName).Returns(httpClient);

        using var simpleRequestSchema = ConvertToJsonContent(SimpleRequestSchema);
        var pluginRequest = new PluginRequestSubmodel(httpClientName, simpleRequestSchema);
        var pluginRequests = new List<PluginRequestSubmodel> { pluginRequest };

        var contentList = await _sut.GetDataForSemanticIdsAsync(pluginRequests, "asdf", CancellationToken.None);

        Assert.NotNull(contentList);
        Assert.Single(contentList);

        var text = await contentList[0].ReadAsStringAsync();
        Assert.Equal(SimpleResponse, text);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
    }

    [Fact]
    public async Task GetDataForSemanticIdsAsync_404Response_ThrowsResourceNotFoundExceptionAsync()
    {
        using var simpleRequestSchema = ConvertToJsonContent(SimpleRequestSchema);
        var pluginRequest = new PluginRequestSubmodel($"{PluginConfig.HttpClientNamePrefix}PluginName", simpleRequestSchema);
        var pluginRequests = new List<PluginRequestSubmodel> { pluginRequest };

        using var messageHandler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not found")
            }));

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://example.com");
        _httpClientFactory.CreateClient(pluginRequest.HttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.GetDataForSemanticIdsAsync(pluginRequests, "asdf", CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForAllShellDescriptorsAsync_ShouldReturnRawContent()
    {
        HttpRequestMessage? captured = null;

        using var messageHandler = new FakeHttpMessageHandler((req, _) =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(ProviderTestData.ShellDescriptors, Encoding.UTF8, "application/json")
            });
        });

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://example.com");
        _httpClientFactory.CreateClient(PluginConfig.MetaData).Returns(httpClient);

        var metadata = new List<PluginRequestMetaData>
        {
            new(PluginConfig.MetaData, "")
        };

        var result = await _sut.GetDataForAllShellDescriptorsAsync(null, null, metadata, CancellationToken.None);

        Assert.NotNull(result);
        var json = await result[0].ReadAsStringAsync();
        Assert.Equal(ProviderTestData.ShellDescriptors.Trim(), json.Trim());

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal("https://example.com/metadata/shells", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetDataForAllShellDescriptorsAsync_ShouldThrowNotFound_WhenAllRequestsFailWithNotFound()
    {
        var callCount = 0;
        using var messageHandler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(callCount % 2 == 0 ? HttpStatusCode.NotFound : HttpStatusCode.NotFound));
        });

        _httpClientFactory.CreateClient(PluginConfig.MetaData).Returns(_ => new HttpClient(messageHandler) { BaseAddress = new Uri("https://example.com") });

        var metadata = new List<PluginRequestMetaData>
        {
            new(PluginConfig.MetaData, "plugin1"),
            new(PluginConfig.MetaData, "plugin2"),
        };

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetDataForAllShellDescriptorsAsync(null, null, metadata, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForAllShellDescriptorsAsync_ShouldThrowUnauthorized_WhenAllRequestsFailWithUnauthorized()
    {
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://example.com");
        _httpClientFactory.CreateClient(PluginConfig.MetaData).Returns(httpClient);

        var metadata = new List<PluginRequestMetaData>
        {
            new(PluginConfig.MetaData, "")
        };

        await Assert.ThrowsAsync<ServiceAuthorizationException>(() => _sut.GetDataForAllShellDescriptorsAsync(null, null, metadata, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForAllShellDescriptorsAsync_ShouldThrowResponseParsingException_WhenRequestsFailWithUnExpectedExceptions()
    {
        var callCount = 0;
        using var messageHandler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(callCount % 2 == 0 ? HttpStatusCode.Conflict : HttpStatusCode.Conflict));
        });

        _httpClientFactory.CreateClient(PluginConfig.MetaData).Returns(_ => new HttpClient(messageHandler) { BaseAddress = new Uri("https://example.com") });

        var metadata = new List<PluginRequestMetaData>
        {
            new(PluginConfig.MetaData, "plugin1"),
            new(PluginConfig.MetaData, "plugin2"),
        };

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetDataForAllShellDescriptorsAsync(null, null, metadata, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForAllShellDescriptorsAsync_ShouldThrowBadRequest_WhenRequestsFailWithDifferentExceptions()
    {
        var callCount = 0;
        using var messageHandler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(callCount % 2 == 0 ? HttpStatusCode.Unauthorized : HttpStatusCode.NotFound));
        });

        _httpClientFactory.CreateClient(PluginConfig.MetaData).Returns(_ => new HttpClient(messageHandler) { BaseAddress = new Uri("https://example.com") });

        var metadata = new List<PluginRequestMetaData>
        {
            new(PluginConfig.MetaData, "plugin1"),
            new(PluginConfig.MetaData, "plugin2"),
        };

        await Assert.ThrowsAsync<PluginMetaDataInvalidRequestException>(() => _sut.GetDataForAllShellDescriptorsAsync(null, null, metadata, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForShellDescriptorByIdAsync_ShouldReturnRawContent()
    {
        HttpRequestMessage? captured = null;

        using var messageHandler = new FakeHttpMessageHandler((req, _) =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(ProviderTestData.ShellDescriptor, Encoding.UTF8, "application/json")
            });
        });

        using var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("https://testendpoint.com") };
        _httpClientFactory.CreateClient(PluginConfig.MetaData).Returns(httpClient);

        var testId = GetTestShellDescriptorDataList().First().Id!;
        var expectedEncoded = testId.EncodeBase64Url();

        var metadata = new List<PluginRequestMetaData>
        {
            new(PluginConfig.MetaData, testId)
        };

        var result = await _sut.GetDataForShellDescriptorByIdAsync(metadata, CancellationToken.None);

        Assert.NotNull(result);
        var json = await result[0].ReadAsStringAsync();
        Assert.Equal(ProviderTestData.ShellDescriptor.Trim(), json.Trim());

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal(
            $"https://testendpoint.com/metadata/shells/{expectedEncoded}",
            captured.RequestUri!.ToString()
        );
    }

    [Fact]
    public async Task GetDataForAssetInformationByIdAsync_ShouldReturnRawContent()
    {
        HttpRequestMessage? captured = null;

        using var messageHandler = new FakeHttpMessageHandler((req, _) =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(ProviderTestData.AssetInformation, Encoding.UTF8, "application/json")
            });
        });

        using var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("https://testendpoint.com") };
        _httpClientFactory.CreateClient(PluginConfig.MetaData).Returns(httpClient);

        var testId = CreateAssetInformation().GlobalAssetId!;
        var expectedEncoded = testId.EncodeBase64Url();

        var metadata = new List<PluginRequestMetaData>
        {
            new(PluginConfig.MetaData, testId)
        };

        var result = await _sut.GetDataForAssetInformationByIdAsync(metadata, CancellationToken.None);

        Assert.NotNull(result);
        var json = await result[0].ReadAsStringAsync();
        Assert.Equal(ProviderTestData.AssetInformation.Trim(), json.Trim());
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal(
            $"https://testendpoint.com/metadata/assets/{expectedEncoded}",
            captured.RequestUri!.ToString()
        );
    }

    [Fact]
    public async Task GetDataForSemanticIdsAsync_WhenPluginRequestIsNull_ThrowsValidationFailedException()
    {
        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            _sut.GetDataForSemanticIdsAsync(null!, "asdf", CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSemanticIdsAsync_WhenTaskCanceled_ThrowsRequestTimeoutException()
    {
        using var simpleRequestSchema = ConvertToJsonContent(SimpleRequestSchema);
        var pluginRequest = new PluginRequestSubmodel($"{PluginConfig.HttpClientNamePrefix}PluginName", simpleRequestSchema);
        var pluginRequests = new List<PluginRequestSubmodel> { pluginRequest };

        using var messageHandler = new FakeHttpMessageHandler((_, _) => throw new TaskCanceledException("timeout"));
        using var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("https://example.com") };
        _httpClientFactory.CreateClient(pluginRequest.HttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<RequestTimeoutException>(() =>
            _sut.GetDataForSemanticIdsAsync(pluginRequests, "asdf", CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetDataForSemanticIdsAsync_WhenUnauthorizedOrForbidden_ThrowsServiceAuthorizationException(HttpStatusCode statusCode)
    {
        using var simpleRequestSchema = ConvertToJsonContent(SimpleRequestSchema);
        var pluginRequest = new PluginRequestSubmodel($"{PluginConfig.HttpClientNamePrefix}PluginName", simpleRequestSchema);
        var pluginRequests = new List<PluginRequestSubmodel> { pluginRequest };

        using var messageHandler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("Unauthorized")
            }));

        using var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("https://example.com") };
        _httpClientFactory.CreateClient(pluginRequest.HttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ServiceAuthorizationException>(() =>
            _sut.GetDataForSemanticIdsAsync(pluginRequests, "asdf", CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSemanticIdsAsync_WhenUnexpectedStatusCode_ThrowsResponseParsingException()
    {
        using var simpleRequestSchema = ConvertToJsonContent(SimpleRequestSchema);
        var pluginRequest = new PluginRequestSubmodel($"{PluginConfig.HttpClientNamePrefix}PluginName", simpleRequestSchema);
        var pluginRequests = new List<PluginRequestSubmodel> { pluginRequest };

        using var messageHandler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server error")
            }));

        using var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("https://example.com") };
        _httpClientFactory.CreateClient(pluginRequest.HttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() =>
            _sut.GetDataForSemanticIdsAsync(pluginRequests, "asdf", CancellationToken.None));
    }

    private static List<ShellDescriptorMetaData> GetTestShellDescriptorDataList()
    => [
        new ShellDescriptorMetaData
        {
            GlobalAssetId = "https://example.com/ids/F/5350_5407_2522_6562",
            IdShort = "SensorWeatherStationExample",
            Id = "https://example.com/ids/aas/1170_1160_3052_6568",
            SpecificAssetIds = []
        }
    ];

    private static AssetInformation CreateAssetInformation()
    {
        var thumbnail = Substitute.For<IResource>();
        thumbnail.Path = "AAS_Logo.svg";
        thumbnail.ContentType = "image/svg+xml";

        return new AssetInformation(
            assetKind: AssetKind.Type,
            globalAssetId: "https://admin-shell.io/idta/asset/ContactInformation/1/0",
            specificAssetIds: [],
            defaultThumbnail: thumbnail
        );
    }

    public static JsonContent ConvertToJsonContent(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonContent.Create(doc.RootElement.Clone(), options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }
}

public class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        send(request, cancellationToken);
}
