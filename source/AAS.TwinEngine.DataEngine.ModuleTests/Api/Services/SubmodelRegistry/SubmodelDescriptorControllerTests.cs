using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRegistry.Providers;
using AAS.TwinEngine.DataEngine.ModuleTests.Common;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.SubmodelRegistry;

public abstract class SubmodelDescriptorControllerTestsBase : IDisposable
{
    private readonly ConfigTestFactory _factory;
    private readonly ISubmodelDescriptorProvider _mockSubmodelDescriptorProvider;
    private readonly HttpClient _client;

    protected SubmodelDescriptorControllerTestsBase(string configDir)
    {
        _mockSubmodelDescriptorProvider = Substitute.For<ISubmodelDescriptorProvider>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();

        _factory = new ConfigTestFactory(configDir, services =>
        {
            _ = services.AddSingleton(mockPluginManifestProvider);
            _ = services.AddSingleton(_mockSubmodelDescriptorProvider);
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
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

    [Theory]
    [InlineData("invalid!!base64")]
    [InlineData("not-valid-base64!!!")]
    public async Task GetSubmodelDescriptorById_InvalidBase64_Returns400BadRequestAsync(string invalidBase64)
    {
        var response = await _client.GetAsync($"/submodel-descriptors/{invalidBase64}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("<iframe onload=alert('xss')>")]
    [InlineData("<svg/onload=alert('xss')>")]
    [InlineData("eval(alert('xss'))")]
    [InlineData("<script>alert(1)</script>")]
    public async Task GetSubmodelDescriptorById_XssInDecodedId_Returns400BadRequestAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/submodel-descriptors/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DROP TABLE descriptors--")]
    [InlineData("1 UNION SELECT * FROM submodels")]
    public async Task GetSubmodelDescriptorById_SqlInjectionInDecodedId_Returns400BadRequestAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/submodel-descriptors/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\windows\\system32")]
    [InlineData("%2e%2e/config")]
    public async Task GetSubmodelDescriptorById_PathTraversalInDecodedId_Returns400BadRequestAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/submodel-descriptors/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>")]
    [InlineData("vbscript:msgbox('xss')")]
    [InlineData("file:///etc/passwd")]
    public async Task GetSubmodelDescriptorById_DangerousProtocolInDecodedId_Returns400BadRequestAsync(string maliciousContent)
    {
        var encoded = EncodeBase64Url(maliciousContent);

        var response = await _client.GetAsync($"/submodel-descriptors/{encoded}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("https://example.com/submodels/test123")]
    [InlineData("urn:uuid:test-submodel-123")]
    [InlineData("https://admin-shell.io/submodels/ContactInformation")]
    public async Task GetSubmodelDescriptorById_ValidIdentifiers_DoesNotReturn400Async(string validIdentifier)
    {
        var encoded = EncodeBase64Url(validIdentifier);
        _ = _mockSubmodelDescriptorProvider.GetDataForSubmodelDescriptorByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException());

        var response = await _client.GetAsync($"/submodel-descriptors/{encoded}");

        Assert.NotEqual(HttpStatusCode.BadRequest, response.StatusCode);
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

public class SubmodelDescriptorControllerTests_V1Config : SubmodelDescriptorControllerTestsBase
{
    public SubmodelDescriptorControllerTests_V1Config() : base("v1-config") { }
}

public class SubmodelDescriptorControllerTests_V2Config : SubmodelDescriptorControllerTestsBase
{
    public SubmodelDescriptorControllerTests_V2Config() : base("v2-config") { }
}
