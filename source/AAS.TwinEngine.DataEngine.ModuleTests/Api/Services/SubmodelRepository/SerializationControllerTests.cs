using System.Net;
using System.Text;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.AasRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ModuleTests.Common;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.SubmodelRepository;

public abstract class SerializationControllerTests : IDisposable
{
    private readonly ConfigTestFactory _factory;
    private readonly IAasRepositoryService _mockAasRepositoryService;
    private readonly ISubmodelRepositoryService _mockSubmodelRepositoryService;
    private readonly IConceptDescriptionService _mockConceptDescriptionService;
    private readonly HttpClient _client;

    protected SerializationControllerTests(string configDir)
    {
        _mockAasRepositoryService = Substitute.For<IAasRepositoryService>();
        _mockSubmodelRepositoryService = Substitute.For<ISubmodelRepositoryService>();
        _mockConceptDescriptionService = Substitute.For<IConceptDescriptionService>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();

        _factory = new ConfigTestFactory(configDir, services =>
        {
            _ = services.AddSingleton(mockPluginManifestProvider);
            _ = services.AddSingleton(_mockAasRepositoryService);
            _ = services.AddSingleton(_mockSubmodelRepositoryService);
            _ = services.AddSingleton(_mockConceptDescriptionService);
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
    public async Task SerializeAasxAsync_ReturnsOkAsync_WhenConceptDescriptionsIsTrueAsync()
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

    [Fact]
    public async Task SerializeAasx_InvalidBase64InAasIds_Returns400BadRequestAsync()
    {
        var invalidAasId = "invalid!!base64";
        var validSubmodelId = EncodeBase64Url("https://example.com/submodels/test");

        var url = $"/serialization?aasIds={invalidAasId}&submodelIds={validSubmodelId}&includeConceptDescriptions=false";

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SerializeAasx_MaliciousPatternInDecodedAasId_Returns400BadRequestAsync()
    {
        var maliciousEncoded = EncodeBase64Url("javascript:alert('xss')");
        var validEncoded = EncodeBase64Url("https://example.com/submodels/test");

        var url = $"/serialization?aasIds={maliciousEncoded}&submodelIds={validEncoded}&includeConceptDescriptions=false";

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SerializeAasx_InvalidBase64InSubmodelIds_Returns400BadRequestAsync()
    {
        var validEncoded = EncodeBase64Url("https://example.com/aas/test");
        var invalidSubmodelId = "not!!valid!!base64";

        var url = $"/serialization?aasIds={validEncoded}&submodelIds={invalidSubmodelId}&includeConceptDescriptions=false";

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SerializeAasx_MaliciousPatternInDecodedSubmodelId_Returns400BadRequestAsync()
    {
        var validEncoded = EncodeBase64Url("https://example.com/aas/test");
        var maliciousEncoded = EncodeBase64Url("<script>alert('xss')</script>");

        var url = $"/serialization?aasIds={validEncoded}&submodelIds={maliciousEncoded}&includeConceptDescriptions=false";

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("../../../etc/passwd")]
    [InlineData("1 UNION SELECT *")]
    public async Task SerializeAasx_SqlInjectionOrPathTraversalInAasId_Returns400BadRequestAsync(string maliciousContent)
    {
        var maliciousEncoded = EncodeBase64Url(maliciousContent);
        var validEncoded = EncodeBase64Url("https://example.com/submodels/test");

        var url = $"/serialization?aasIds={maliciousEncoded}&submodelIds={validEncoded}&includeConceptDescriptions=false";

        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SerializeAasx_ValidIdentifiers_DoesNotReturn400Async()
    {
        var validAasId = EncodeBase64Url("https://example.com/aas/test123");
        var validSubmodelId = EncodeBase64Url("https://example.com/submodels/test456");

        var url = $"/serialization?aasIds={validAasId}&submodelIds={validSubmodelId}&includeConceptDescriptions=false";

        _ = _mockSubmodelRepositoryService.GetSubmodelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new SubmodelNotFoundException());

        var response = await _client.GetAsync(url);

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

public class SerializationControllerTestsV1Config() : SerializationControllerTests("v1-config");

public class SerializationControllerTestsV2Config() : SerializationControllerTests("v2-config");
