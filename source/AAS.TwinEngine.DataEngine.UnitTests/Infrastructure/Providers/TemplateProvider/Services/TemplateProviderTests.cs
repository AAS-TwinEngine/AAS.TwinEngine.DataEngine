using System.Net;
using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Template = AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services.TemplateProvider;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.TemplateProvider.Services;

public class TemplateProviderTests
{
    private readonly ICreateClient _httpClientFactory;
    private readonly Template _sut;
    private const string TemplateId = "Nameplate";

    public TemplateProviderTests()
    {
        var logger = Substitute.For<ILogger<Template>>();
        _httpClientFactory = Substitute.For<ICreateClient>();
        var subModelRegistryUrl = Substitute.For<IOptions<AasEnvironmentConfig>>();
        subModelRegistryUrl.Value.Returns(new AasEnvironmentConfig { AasEnvironmentRepositoryBaseUrl = new Uri("https://www.mm-software.com/fakeurl"), AasRegistryBaseUrl = new Uri("https://www.mm-software.com/fakeurl") });
        _sut = new Template(logger, _httpClientFactory, subModelRegistryUrl);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ReturnsSubmodel_WhenValidResponse()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ProviderTestData.ValidateSubmodelResponse) };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var result = await _sut.GetSubmodelTemplateAsync(TemplateId, CancellationToken.None);

        Assert.Equal("TestId", result.Id);
        Assert.Equal("Test", result.IdShort);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsResponseParsingException_WhenInvalidJsonResponse()
    {
        using var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        mockHttpResponse.Content = new StringContent("{ invalid json }");
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetSubmodelTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsResourceNotFoundException_WhenNotFoundResponse()
    {
        using var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        mockHttpResponse.Content = new StringContent("Not found");
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetSubmodelTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsException_WhenHttpClientFails()
    {
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(new HttpRequestException("Network error"));
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _sut.GetSubmodelTemplateAsync(TemplateId, CancellationToken.None));
        Assert.Equal("Network error", exception.Message);
    }

    [Fact]
    public async Task GetShellDescriptorsTemplateAsync_ReturnsShellDescriptor_WhenValidResponse()
    {
        using var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        mockHttpResponse.Content = new StringContent(ProviderTestData.ValidateShellDescriptorResponse);
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHttpClientName).Returns(httpClient);

        var result = await _sut.GetShellDescriptorsTemplateAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("https://admin-shell.io/idta/aas/ContactInformation/1/0", result.Id);
        Assert.Equal("ContactInformationAAS", result.IdShort);
        Assert.Equal("https://admin-shell.io/idta/asset/ContactInformation/1/0", result.GlobalAssetId);
    }

    [Fact]
    public async Task GetShellDescriptorsTemplateAsync_ThrowsResponseParsingException_WhenInvalidJsonResponse()
    {
        using var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        mockHttpResponse.Content = new StringContent("{ invalid json }");
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetShellDescriptorsTemplateAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorsTemplateAsync_ThrowsResourceNotFoundException_WhenResultArrayIsMissing()
    {
        const string jsonResponse = "{}";

        using var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler)
        {
            BaseAddress = new Uri("https://www.mm-software.com/fakeurl")
        };
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _sut.GetShellDescriptorsTemplateAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorsTemplateAsync_ReturnsDefaultShellDescriptor_WhenResultArrayIsEmpty()
    {
        const string jsonResponse = "{ \"result\": [] }";

        using var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler)
        {
            BaseAddress = new Uri("https://www.mm-software.com/fakeurl")
        };
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHttpClientName).Returns(httpClient);

        var result = await _sut.GetShellDescriptorsTemplateAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ShellDescriptor.CreateDefault().Id, result.Id);
        Assert.Equal(ShellDescriptor.CreateDefault().IdShort, result.IdShort);
        Assert.Equal(ShellDescriptor.CreateDefault().GlobalAssetId, result.GlobalAssetId);
    }

    [Fact]
    public async Task GetShellDescriptorsTemplateAsync_ThrowsResponseParsingException_WhenDeserializationFails()
    {
        const string JsonWithInvalidDescriptor = "{ \"result\": [ null ] }";
        using var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        mockHttpResponse.Content = new StringContent(JsonWithInvalidDescriptor);
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetShellDescriptorsTemplateAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorsTemplateAsync_ThrowsResourceNotFoundException_WhenNotFoundResponse()
    {
        using var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        mockHttpResponse.Content = new StringContent("Not found");
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetShellDescriptorsTemplateAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetShellDescriptorsTemplateAsync_ThrowsException_WhenHttpClientFails()
    {
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(new HttpRequestException("Network error"));
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasRegistryHttpClientName).Returns(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _sut.GetShellDescriptorsTemplateAsync(CancellationToken.None));
        Assert.Equal("Network error", exception.Message);
    }

    [Fact]
    public async Task GetShellTemplateAsync_ReturnsShell_WhenValidResponse()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ProviderTestData.ValidateShellResponse) };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var result = await _sut.GetShellTemplateAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("https://admin-shell.io/idta/aas/ContactInformation/1/0", result.Id);
        Assert.Equal("ContactInformationAAS", result.IdShort);
    }

    [Fact]
    public async Task GetShellTemplateAsync_ThrowsResponseParsingException_WhenInvalidJsonResponse()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid json }")
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetShellTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellTemplateAsync_ThrowsResourceNotFoundException_WhenNotFoundResponse()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not found")
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                                                                                _sut.GetShellTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetShellTemplateAsync_ThrowsException_WhenHttpClientFails()
    {
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(new HttpRequestException("Network error"));
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                                                                           _sut.GetShellTemplateAsync(TemplateId, CancellationToken.None));
        Assert.Equal("Network error", exception.Message);
    }

    [Fact]
    public async Task GetAssetInformationTemplateAsync_ReturnsAssetInformation_WhenValidResponse()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ProviderTestData.ValidateAssetInformationResponse) };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var result = await _sut.GetAssetInformationTemplateAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("https://sew-eurodrive.de/shell/1", result.GlobalAssetId);
    }

    [Fact]
    public async Task GetAssetInformationTemplateAsync_ThrowsResponseParsingException_WhenInvalidJsonResponse()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid json }")
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetAssetInformationTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetInformationTemplateAsync_ThrowsResourceNotFoundException_WhenNotFoundResponse()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not found")
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                                                                                _sut.GetAssetInformationTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetAssetInformationTemplateAsync_ThrowsException_WhenHttpClientFails()
    {
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(new HttpRequestException("Network error"));
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                                                                           _sut.GetAssetInformationTemplateAsync(TemplateId, CancellationToken.None));
        Assert.Equal("Network error", exception.Message);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ReturnsSubmodelRefs_WhenValidResponse()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ProviderTestData.ValidateSubmodelRefResponse)
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var result = await _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("urn:uuid:submodel-123", result[0].Keys![0].Value);
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsResourceNotFoundException_WhenResultArrayMissing()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ \"unexpected\": [] }")
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsResourceNotFoundException_WhenResultArrayIsEmpty()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ \"result\": [] }")
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsResponseParsingException_WhenInvalidJson()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid json }")
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelRefByIdAsync_ThrowsHttpRequestException_WhenHttpFails()
    {
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(new HttpRequestException("Network error"));
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                                                                           _sut.GetSubmodelRefByIdAsync(TemplateId, CancellationToken.None));
        Assert.Equal("Network error", exception.Message);
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsServiceAuthorizationException_WhenUnauthorized()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Unauthorized")
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ServiceAuthorizationException>(() =>
            _sut.GetSubmodelTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsRequestTimeoutException_WhenTimeout()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
        {
            Content = new StringContent("Request timed out")
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<RequestTimeoutException>(() =>
            _sut.GetSubmodelTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelTemplateAsync_ThrowsValidationFailedException_WhenUnexpectedStatusCode()
    {
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error")
        };
        using var mockHttpMessageHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHttpMessageHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName).Returns(httpClient);

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            _sut.GetSubmodelTemplateAsync(TemplateId, CancellationToken.None));
    }

    [Fact]
    public async Task GetConceptDescriptionByIdAsync_ReturnsConceptDescription_WhenResponseIsValid()
    {
        const string CdIdentifier = "test-id";
        var mockHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ProviderTestData.ValidConceptDescription)
        };
        using var mockHandler = new FakeHttpMessageHandler(mockHttpResponse);
        using var httpClient = new HttpClient(mockHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName)
                          .Returns(httpClient);

        var result = await _sut.GetConceptDescriptionByIdAsync(CdIdentifier, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(typeof(JsonException))]
    [InlineData(typeof(RequestTimeoutException))]
    [InlineData(typeof(ValidationFailedException))]
    [InlineData(typeof(ResourceNotFoundException))]
    [InlineData(typeof(ServiceAuthorizationException))]
    public async Task GetConceptDescriptionByIdAsync_ReturnsNull_OnHandledExceptions(Type exceptionType)
    {
        const string CdIdentifier = "test-id";
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        using var mockHandler = new FakeHttpMessageHandler(exception);
        using var httpClient = new HttpClient(mockHandler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _httpClientFactory.CreateClient(AasEnvironmentConfig.AasEnvironmentRepoHttpClientName)
                          .Returns(httpClient);

        var result = await _sut.GetConceptDescriptionByIdAsync(CdIdentifier, CancellationToken.None);

        Assert.Null(result);
    }
}

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response = null!;
    private readonly Exception? _exceptionToThrow;
    public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;
    public FakeHttpMessageHandler(Exception exception) => _exceptionToThrow = exception;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => _exceptionToThrow != null ? throw _exceptionToThrow : Task.FromResult(_response);
}
