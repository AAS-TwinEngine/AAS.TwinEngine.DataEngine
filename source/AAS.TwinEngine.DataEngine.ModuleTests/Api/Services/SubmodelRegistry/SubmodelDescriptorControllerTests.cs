using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.SubmodelRegistry;

public class SubmodelDescriptorControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ISubmodelDescriptorProvider _mockSubmodelDescriptorProvider;
    private readonly HttpClient _client;

    public SubmodelDescriptorControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockSubmodelDescriptorProvider = Substitute.For<ISubmodelDescriptorProvider>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();

        var factory1 = factory.WithWebHostBuilder(builder =>
        {
            _ = builder.ConfigureServices(services =>
            {
                _ = services.AddSingleton(mockPluginManifestProvider);
                _ = services.AddSingleton(_mockSubmodelDescriptorProvider);
            });
        });

        _client = factory1.CreateClient();
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_ReturnsOkAsync()
    {
        // Arrange
        const string SubmodelIdentifier = "Q29udGFjdEluZm9ybWF0aW9u";
        var mockTemplate = TestData.CreateSubmodelDescriptor();

        _ = _mockSubmodelDescriptorProvider.GetDataForSubmodelDescriptorByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(mockTemplate);

        // Act
        var response = await _client.GetAsync($"/submodel-descriptors/{SubmodelIdentifier}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(json);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_WithNotFound_Returns404Async()
    {
        const string SubmodelIdentifier = "Q29udGFjdEluZm9ybWF0aW9u";

        _ = _mockSubmodelDescriptorProvider.GetDataForSubmodelDescriptorByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync($"/submodel-descriptors/{SubmodelIdentifier}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_WithInternalServerError_Returns500Async()
    {
        const string SubmodelIdentifier = "Q29udGFjdEluZm9ybWF0aW9u";

        _ = _mockSubmodelDescriptorProvider.GetDataForSubmodelDescriptorByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Throws(new ResponseParsingException());

        var response = await _client.GetAsync($"/submodel-descriptors/{SubmodelIdentifier}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetSubmodelDescriptorByIdAsync_WhenIdentifierIsInValid_Returns400Async()
    {
        const string SubmodelIdentifier = "in valid";

        var response = await _client.GetAsync($"/submodel-descriptors/{SubmodelIdentifier}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
