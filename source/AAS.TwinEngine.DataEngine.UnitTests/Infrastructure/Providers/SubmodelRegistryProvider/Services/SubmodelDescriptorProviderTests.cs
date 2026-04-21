using System.Net;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.SubmodelRegistryProvider.Services;
using AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Services;

using Microsoft.Extensions.Logging;

using NSubstitute;

using UnauthorizedAccessException = AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure.UnauthorizedAccessException;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.SubmodelRegistryProvider.Services;

public class SubmodelDescriptorProviderTests
{
    private readonly ILogger<SubmodelDescriptorProvider> _logger = Substitute.For<ILogger<SubmodelDescriptorProvider>>();
    private readonly ICreateClient _clientFactory = Substitute.For<ICreateClient>();
    private readonly SubmodelDescriptorProvider _sut;

    public SubmodelDescriptorProviderTests() => _sut = new SubmodelDescriptorProvider(_logger, _clientFactory);

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ReturnsSubmodelDesciptor_WhenResponseIsSuccessful()
    {
        const string Id = "ContactInformation";
        var expectedContent = new StringContent("{ \"id\": \"ContactInformation\" }");
        var expectedDescriptor = new SubmodelDescriptor { Id = "ContactInformation" };
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = expectedContent
        }));
        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://mm-software/fakeUrl");
        _clientFactory.CreateClient(HttpClientNames.SubmodelRegistry)
                      .Returns(httpClient);

        var result = await _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None);

        Assert.Equal(expectedDescriptor.Id, result.Id);
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsResponseParsingException_WhenDeserializationFails()
    {
        const string Id = "ContactInformation";
        var invalidJson = new StringContent("This is not valid JSON");
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = invalidJson
        }));
        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://mm-software/fakeUrl");
        _clientFactory.CreateClient(HttpClientNames.SubmodelRegistry)
                      .Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsResponseParsingException_WhenDeserializedObjectIsNull()
    {
        const string Id = "null-object-id";
        var emptyJson = new StringContent("null");
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = emptyJson
        }));
        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://mm-software/fakeUrl");
        _clientFactory.CreateClient(HttpClientNames.SubmodelRegistry)
                      .Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsResourceNotFoundException_WhenResponseIsNotSuccessful()
    {
        const string Id = "test-id";
        var errorContent = new StringContent("Not found");
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound,
            Content = errorContent
        }));
        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://mm-software/fakeUrl");
        _clientFactory.CreateClient(HttpClientNames.SubmodelRegistry)
                      .Returns(httpClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                                                                _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsServiceAuthorizationException_WhenUnauthorized()
    {
        const string Id = "auth-fail-id";
        var errorContent = new StringContent("Unauthorized");
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Unauthorized,
            Content = errorContent
        }));
        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://mm-software/fakeUrl");
        _clientFactory.CreateClient(HttpClientNames.SubmodelRegistry)
                      .Returns(httpClient);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsServiceAuthorizationException_WhenForbidden()
    {
        const string Id = "forbidden-id";
        var errorContent = new StringContent("Forbidden");
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Forbidden,
            Content = errorContent
        }));
        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://mm-software/fakeUrl");
        _clientFactory.CreateClient(HttpClientNames.SubmodelRegistry)
                      .Returns(httpClient);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsRequestTimeoutException_WhenTimeout()
    {
        const string Id = "timeout-id";
        var errorContent = new StringContent("Request timed out");
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.RequestTimeout,
            Content = errorContent
        }));
        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://mm-software/fakeUrl");
        _clientFactory.CreateClient(HttpClientNames.SubmodelRegistry)
                      .Returns(httpClient);

        await Assert.ThrowsAsync<RequestTimeoutException>(() =>
            _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForSubmodelDescriptorByIdAsync_ThrowsValidationFailedException_WhenOtherError()
    {
        const string Id = "badrequest-id";
        var errorContent = new StringContent("Bad request");
        using var messageHandler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = errorContent
        }));
        using var httpClient = new HttpClient(messageHandler);
        httpClient.BaseAddress = new Uri("https://mm-software/fakeUrl");
        _clientFactory.CreateClient(HttpClientNames.SubmodelRegistry)
                      .Returns(httpClient);

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            _sut.GetDataForSubmodelDescriptorByIdAsync(Id, CancellationToken.None));
    }
}
