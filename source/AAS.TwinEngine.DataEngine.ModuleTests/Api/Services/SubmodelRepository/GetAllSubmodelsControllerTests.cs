using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ModuleTests.Common;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.SubmodelRepository;

public abstract class GetAllSubmodelsControllerTests : IDisposable
{
    private readonly ConfigTestFactory _factory;
    private readonly ISubmodelRepositoryService _mockSubmodelRepositoryService;
    private readonly HttpClient _client;

    protected GetAllSubmodelsControllerTests(string configDir)
    {
        _mockSubmodelRepositoryService = Substitute.For<ISubmodelRepositoryService>();
        var mockPluginManifestProvider = Substitute.For<IPluginManifestProvider>();

        _factory = new ConfigTestFactory(configDir, services =>
        {
            _ = services.AddSingleton(_mockSubmodelRepositoryService);
            _ = services.AddSingleton(mockPluginManifestProvider);
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
    public async Task GetAllSubmodelsAsync_WithNoQueryParams_ReturnsOkWithEmptyResultAsync()
    {
        // Arrange
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(submodelList);

        // Act
        var response = await _client.GetAsync("/submodels");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("result"));
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WithSubmodels_ReturnsPopulatedResultAsync()
    {
        // Arrange
        var submodel = TestData.CreateSubmodel();
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = [submodel]
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(submodelList);

        // Act
        var response = await _client.GetAsync("/submodels");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(body);
        var resultArray = body["result"]?.AsArray();
        Assert.NotNull(resultArray);
        _ = Assert.Single(resultArray);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WithPagingCursorInResponse_ReturnsCursorInBodyAsync()
    {
        // Arrange
        const string ExpectedCursor = "dGVzdEN1cnNvcg==";
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = ExpectedCursor },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(submodelList);

        // Act
        var response = await _client.GetAsync("/submodels?limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(body);
        var pagingMetadata = body["paging_metadata"]?.AsObject();
        Assert.NotNull(pagingMetadata);
        Assert.Equal(ExpectedCursor, pagingMetadata["cursor"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WithSemanticIdQueryParam_ReturnsOkAsync()
    {
        // Arrange
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(submodelList);

        // Act
        var response = await _client.GetAsync("/submodels?semanticId=aHR0cDovL2V4YW1wbGUuY29t");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WithIdShortQueryParam_ReturnsOkAsync()
    {
        // Arrange
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(submodelList);

        // Act
        var response = await _client.GetAsync("/submodels?idShort=DigitalNameplate");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WithValidLimitAndCursor_ReturnsOkAsync()
    {
        // Arrange
        var cursor = EncodeBase64Url("next-page-token");
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), 5, cursor, Arg.Any<CancellationToken>())
            .Returns(submodelList);

        // Act
        var response = await _client.GetAsync($"/submodels?limit=5&cursor={cursor}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("deep")]
    [InlineData("core")]
    public async Task GetAllSubmodelsAsync_WithLevelQueryParam_ReturnsOkAsync(string level)
    {
        // Arrange
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(submodelList);

        // Act
        var response = await _client.GetAsync($"/submodels?level={level}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("withBlobValue")]
    [InlineData("withoutBlobValue")]
    public async Task GetAllSubmodelsAsync_WithExtentQueryParam_ReturnsOkAsync(string extent)
    {
        // Arrange
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(submodelList);

        // Act
        var response = await _client.GetAsync($"/submodels?extent={extent}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetAllSubmodelsAsync_WithInvalidLimit_Returns400Async(int invalidLimit)
    {
        // Act
        var response = await _client.GetAsync($"/submodels?limit={invalidLimit}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WithInvalidCursorEncoding_Returns400Async()
    {
        // Arrange – a cursor that is not valid Base64Url
        const string InvalidCursor = "not!!valid!!base64";

        // Act
        var response = await _client.GetAsync($"/submodels?cursor={InvalidCursor}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("notALevel")]
    [InlineData("123")]
    public async Task GetAllSubmodelsAsync_WithInvalidLevelEnum_Returns400Async(string invalidLevel)
    {
        // Act
        var response = await _client.GetAsync($"/submodels?level={invalidLevel}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("noBlobValue")]
    public async Task GetAllSubmodelsAsync_WithInvalidExtentEnum_Returns400Async(string invalidExtent)
    {
        // Act
        var response = await _client.GetAsync($"/submodels?extent={invalidExtent}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenServiceThrowsSubmodelNotFoundException_Returns404Async()
    {
        // Arrange
        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new SubmodelNotFoundException());

        // Act
        var response = await _client.GetAsync("/submodels");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenServiceThrowsInternalDataProcessingException_Returns500Async()
    {
        // Arrange
        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new InternalDataProcessingException());

        // Act
        var response = await _client.GetAsync("/submodels");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_WhenServiceThrowsUnexpectedException_Returns500Async()
    {
        // Arrange
        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Unexpected failure"));

        // Act
        var response = await _client.GetAsync("/submodels");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelsAsync_ResponseBodyContainsResultAndPagingMetadata_ReturnsOkAsync()
    {
        // Arrange
        var submodelList = new SubmodelList
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelsAsync(Arg.Any<SubmodelSearchFilter?>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(submodelList);

        // Act
        var response = await _client.GetAsync("/submodels");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("result"), "Response body must contain a 'result' field.");
        Assert.True(body.ContainsKey("paging_metadata"), "Response body must contain a 'paging_metadata' field.");
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

public class GetAllSubmodelsControllerTestsV1Config() : GetAllSubmodelsControllerTests("v1-config");

public class GetAllSubmodelsControllerTestsV2Config() : GetAllSubmodelsControllerTests("v2-config");
