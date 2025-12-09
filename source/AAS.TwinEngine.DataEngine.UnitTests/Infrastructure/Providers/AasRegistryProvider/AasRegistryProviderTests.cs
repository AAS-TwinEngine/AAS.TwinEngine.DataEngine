using System.Net;
using System.Text;
using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.Infrastructure.Http.Clients;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using AasRegistryProviderRepo = AAS.TwinEngine.DataEngine.Infrastructure.Providers.AasRegistryProvider.Services;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.AasRegistryProvider;

public class AasRegistryProviderTests
{
    private readonly ILogger<AasRegistryProviderRepo.AasRegistryProvider> _logger = Substitute.For<ILogger<AasRegistryProviderRepo.AasRegistryProvider>>();
    private readonly ICreateClient _clientFactory = Substitute.For<ICreateClient>();
    private readonly IOptions<AasEnvironmentConfig> _options = Substitute.For<IOptions<AasEnvironmentConfig>>();
    private readonly AasRegistryProviderRepo.AasRegistryProvider _sut;
    private const string AasRegistryPath = "shell-descriptors";

    private const string JsonResponseForShells = """
                                        {
                                            "paging_metadata": {
                                            "cursor": null
                                            },
                                            "result": [
                                            {
                                            "globalAssetId": "ContactInformation",
                                            "idShort": "ContactInformationAAS",
                                             "id": "ContactInformation"
                                            }
                                            ]
                                        }
                                        """;

    public AasRegistryProviderTests()
    {
        _options.Value.Returns(new AasEnvironmentConfig { AasRegistryPath = AasRegistryPath });
        _sut = new AasRegistryProviderRepo.AasRegistryProvider(_logger, _clientFactory, _options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsShellDescriptors_WhenSuccessful()
    {
        var expectedResponse = new List<ShellDescriptor>()
        {
            new()
            {
                GlobalAssetId = "ContactInformation",
                Id = "ContactInformation",
                IdShort = "ContactInformationAAS"
            }
        };
        using var handler = new FakeHttpMessageHandler(_ =>
                                                     new HttpResponseMessage(HttpStatusCode.OK)
                                                     {
                                                         Content = new StringContent(JsonResponseForShells, Encoding.UTF8, "application/json")
                                                     });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var result = await _sut.GetAllAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(JsonSerializer.Serialize(expectedResponse), JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task GetAllAsync_ThrowsResourceResourceNotFoundException_WhenHttpFails()
    {
        using var handler = new FakeHttpMessageHandler(_ =>
                                                          new HttpResponseMessage(HttpStatusCode.NotFound)
                                                          {
                                                              Content = new StringContent("Not Found", Encoding.UTF8, "application/json")
                                                          });

        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetAllAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAllAsync_ThrowsResponseParsingException_WhenJsonIsInvalid()
    {
        const string InvalidJson = " {\r\n     \"paging_metadata\": {\r\n     \"cursor\": null\r\n     }}";
        using var handler = new FakeHttpMessageHandler(_ =>
                                                     new HttpResponseMessage(HttpStatusCode.OK)
                                                     {
                                                         Content = new StringContent(InvalidJson, Encoding.UTF8, "application/json")
                                                     });

        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetAllAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenResultIsNullInJson()
    {
        const string NullResultJson = """
                                      {
                                        "paging_metadata": { "cursor": null },
                                        "result": null
                                      }
                                      """;
        using var handler = new FakeHttpMessageHandler(_ =>
                                                           new HttpResponseMessage(HttpStatusCode.OK)
                                                           {
                                                               Content = new StringContent(NullResultJson, Encoding.UTF8, "application/json")
                                                           });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var list = await _sut.GetAllAsync(CancellationToken.None);

        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsResponseParsingException_WhenDeserializedObjectIsNull()
    {
        const string NullContent = "null";
        using var handler = new FakeHttpMessageHandler(_ =>
                                                           new HttpResponseMessage(HttpStatusCode.OK)
                                                           {
                                                               Content = new StringContent(NullContent, Encoding.UTF8, "application/json")
                                                           });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetByIdAsync("anything", CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsShellDescriptor_WhenSuccessful()
    {
        var expectedDescriptor = new ShellDescriptor
        {
            Id = "ContactInformation",
            IdShort = "ContactInformationAAS",
            GlobalAssetId = "ContactInformation"
        };
        var jsonResponse = JsonSerializer.Serialize(expectedDescriptor);
        using var handler = new FakeHttpMessageHandler(_ =>
                                                     new HttpResponseMessage(HttpStatusCode.OK)
                                                     {
                                                         Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                                                     });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var result = await _sut.GetByIdAsync("ContactInformation", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(jsonResponse, JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsResourceNotFoundException_WhenHttpFails()
    {
        using var handler = new FakeHttpMessageHandler(_ =>
                                                     new HttpResponseMessage(HttpStatusCode.NotFound)
                                                     {
                                                         Content = new StringContent("Not Found", Encoding.UTF8, "application/json")
                                                     });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.GetByIdAsync("InvalidId", CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsResponseParsingException_WhenJsonIsInvalid()
    {
        const string InvalidJson = "{ invalid json }";
        using var handler = new FakeHttpMessageHandler(_ =>
                                                     new HttpResponseMessage(HttpStatusCode.OK)
                                                     {
                                                         Content = new StringContent(InvalidJson, Encoding.UTF8, "application/json")
                                                     });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        await Assert.ThrowsAsync<ResponseParsingException>(() => _sut.GetByIdAsync("ContactInformation", CancellationToken.None));
    }

    [Fact]
    public async Task PutAsync_Succeeds_WhenRequestIsValid()
    {
        var shellDescriptor = new ShellDescriptor
        {
            Id = "ContactInformation",
            IdShort = "ContactInformationAAS",
            GlobalAssetId = "ContactInformation"
        };
        var encodedId = "ContactInformation".EncodeBase64Url();
        using var handler = new FakeHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Contains(encodedId, request!.RequestUri!.ToString(), StringComparison.OrdinalIgnoreCase);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var exception = await Record.ExceptionAsync(() => _sut.PutAsync("ContactInformation", shellDescriptor, CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task PutAsync_ThrowsRequestTimeoutException_WhenRequestFails()
    {
        var shellDescriptor = new ShellDescriptor
        {
            Id = "ContactInformation",
            IdShort = "ContactInformationAAS",
            GlobalAssetId = "ContactInformation"
        };
        using var handler = new FakeHttpMessageHandler(_ =>
                                                     new HttpResponseMessage(HttpStatusCode.InternalServerError)
                                                     {
                                                         Content = new StringContent("Internal Server Error")
                                                     });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        await Assert.ThrowsAsync<RequestTimeoutException>(() => _sut.PutAsync("ContactInformation", shellDescriptor, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteByIdAsync_Succeeds_WhenRequestIsValid()
    {
        var encodedId = "ContactInformation".EncodeBase64Url();
        using var handler = new FakeHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Contains(encodedId, request!.RequestUri!.ToString(), StringComparison.OrdinalIgnoreCase);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var exception = await Record.ExceptionAsync(() => _sut.DeleteByIdAsync("ContactInformation", CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteByIdAsync_ThrowsRequestTimeoutException_WhenRequestFails()
    {
        using var handler = new FakeHttpMessageHandler(_ =>
                                                     new HttpResponseMessage(HttpStatusCode.InternalServerError)
                                                     {
                                                         Content = new StringContent("Server error")
                                                     });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        await Assert.ThrowsAsync<RequestTimeoutException>(() => _sut.DeleteByIdAsync("ContactInformation", CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenRequestIsValid()
    {
        var shellDescriptor = new ShellDescriptor
        {
            Id = "ContactInformation",
            IdShort = "ContactInformationAAS",
            GlobalAssetId = "ContactInformation"
        };
        using var handler = new FakeHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://www.mm-software.com/shell-descriptors", request!.RequestUri!.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        var exception = await Record.ExceptionAsync(() => _sut.CreateAsync(shellDescriptor, CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationFailedException_WhenRequestFails()
    {
        var shellDescriptor = new ShellDescriptor
        {
            Id = "ContactInformation",
            IdShort = "ContactInformationAAS",
            GlobalAssetId = "ContactInformation"
        };
        using var handler = new FakeHttpMessageHandler(_ =>
                                                     new HttpResponseMessage(HttpStatusCode.BadRequest)
                                                     {
                                                         Content = new StringContent("Bad request")
                                                     });
        using var httpClient = new HttpClient(handler);
        httpClient.BaseAddress = new Uri("https://www.mm-software.com/fakeurl");
        _clientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        await Assert.ThrowsAsync<ValidationFailedException>(() => _sut.CreateAsync(shellDescriptor, CancellationToken.None));
    }

    public class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseGenerator) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = responseGenerator(request);
            return Task.FromResult(response);
        }
    }
}
