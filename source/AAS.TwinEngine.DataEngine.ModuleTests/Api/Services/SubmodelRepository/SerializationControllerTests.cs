using System.Net;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.SubmodelRepository;

public class SerializationControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IAasRepositoryService _mockAasRepositoryService;
    private readonly ISubmodelRepositoryService _mockSubmodelRepositoryService;
    private readonly IConceptDescriptionService _mockConceptDescriptionService;
    private readonly HttpClient _client;

    public SerializationControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockAasRepositoryService = Substitute.For<IAasRepositoryService>();
        _mockSubmodelRepositoryService = Substitute.For<ISubmodelRepositoryService>();
        _mockConceptDescriptionService = Substitute.For<IConceptDescriptionService>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();

        var factory1 = factory.WithWebHostBuilder(builder =>
        {
            _ = builder.ConfigureServices(services =>
            {
                _ = services.AddSingleton(mockPluginManifestProvider);
                _ = services.AddSingleton(_mockAasRepositoryService);
                _ = services.AddSingleton(_mockSubmodelRepositoryService);
                _ = services.AddSingleton(_mockConceptDescriptionService);
            });
        });

        _client = factory1.CreateClient();
    }

    [Fact]
    public async Task SerializeAasxAsync_ReturnsOkAsync()
    {
        // Arrange
        var aasIds = new[] { "aas-123" };
        var submodelIds = new[] { "submodel-456" };

        var url = $"/serialization?aasIds={string.Join("&aasIds=", aasIds)}&submodelIds={string.Join("&submodelIds=", submodelIds)}&includeConceptDescriptions=false";
        var mockSubmodel = TestData.CreateSubmodel();
        var mockResponse = TestData.CreateShellTemplate();

        _ = _mockSubmodelRepositoryService.GetSubmodelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockSubmodel);

        _ = _mockAasRepositoryService.GetShellByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockResponse);

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SerializeAasxAsync_ReturnsOkAsync_WhenConceptDescriptionsIsTrue()
    {
        // Arrange
        var aasIds = new[] { "aas-123" };
        var submodelIds = new[] { "submodel-456" };

        var url = $"/serialization?aasIds={string.Join("&aasIds=", aasIds)}&submodelIds={string.Join("&submodelIds=", submodelIds)}&includeConceptDescriptions=true";
        var mockSubmodel = TestData.CreateSubmodel();
        var mockResponse = TestData.CreateShellTemplate();

        _ = _mockSubmodelRepositoryService.GetSubmodelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockSubmodel);

        _ = _mockAasRepositoryService.GetShellByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockResponse);

        _ = _mockConceptDescriptionService.GetConceptDescriptionById(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(TestData.CreateConceptDescription());

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SerializeAasxAsync_WithNotFound_Returns404Async()
    {
        // Arrange
        var aasIds = new[] { "aas-123" };
        var submodelIds = new[] { "submodel-456" };

        var url = $"/serialization?aasIds={string.Join("&aasIds=", aasIds)}&submodelIds={string.Join("&submodelIds=", submodelIds)}&includeConceptDescriptions=false";

        _ = _mockSubmodelRepositoryService.GetSubmodelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new SubmodelNotFoundException());

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SerializeAasxAsync_WithInternalServerError_Returns500Async()
    {
        // Arrange
        var aasIds = new[] { "aas-123" };
        var submodelIds = new[] { "submodel-456" };

        var url = $"/serialization?aasIds={string.Join("&aasIds=", aasIds)}&submodelIds={string.Join("&submodelIds=", submodelIds)}&includeConceptDescriptions=false";

        _ = _mockSubmodelRepositoryService.GetSubmodelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new InternalDataProcessingException());

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task SerializeAasxAsync_WhenIdentifierIsInValid_Returns400Async()
    {
        // Arrange
        var aasIds = new[] { "aas-123" };
        var submodelIds = new[] { "in valid" };

        var url = $"/serialization?aasIds={string.Join("&aasIds=", aasIds)}&submodelIds={string.Join("&submodelIds=", submodelIds)}&includeConceptDescriptions=false";

        // Act
        var response = await _client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
