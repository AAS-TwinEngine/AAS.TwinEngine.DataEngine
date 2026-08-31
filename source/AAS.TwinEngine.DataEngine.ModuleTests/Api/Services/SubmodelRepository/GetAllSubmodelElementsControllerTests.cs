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

using AasCore.Aas3_1;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.SubmodelRepository;

public abstract class GetAllSubmodelElementsControllerTests : IDisposable
{
    private readonly ConfigTestFactory _factory;
    private readonly ISubmodelRepositoryService _mockSubmodelRepositoryService;
    private readonly HttpClient _client;

    private const string SubmodelId = "ContactInformation";

    protected GetAllSubmodelElementsControllerTests(string configDir)
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

    private static string GetUrl(string? submodelId = null, int? limit = null, string? cursor = null)
    {
        var encodedId = submodelId is null
            ? EncodeBase64Url(SubmodelId)
            : EncodeBase64Url(submodelId);
        var url = $"/submodels/{encodedId}/submodel-elements";
        var queryParams = new Dictionary<string, string?>();
        if (limit.HasValue)
        {
            queryParams["limit"] = limit.Value.ToString();
        }

        if (cursor is not null)
        {
            queryParams["cursor"] = cursor;
        }

        return queryParams.Count > 0 ? QueryHelpers.AddQueryString(url, queryParams) : url;
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WithNoQueryParams_ReturnsOkWithEmptyResultAsync()
    {
        // Arrange
        var elementList = new SubmodelElementsPage
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(elementList);

        // Act
        var response = await _client.GetAsync(GetUrl());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("result"));
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WithElements_ReturnsPopulatedResultAsync()
    {
        // Arrange
        var element = TestData.CreateManufacturerName();
        var elementList = new SubmodelElementsPage
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = [element]
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(elementList);

        // Act
        var response = await _client.GetAsync(GetUrl());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(body);
        var resultArray = body["result"]?.AsArray();
        Assert.NotNull(resultArray);
        _ = Assert.Single(resultArray);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WithPagingCursorInResponse_ReturnsCursorInBodyAsync()
    {
        // Arrange
        const string ExpectedCursor = "dGVzdEN1cnNvcg==";
        var elementList = new SubmodelElementsPage
        {
            PagingMetaData = new PagingMetaData { Cursor = ExpectedCursor },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(elementList);

        // Act
        var response = await _client.GetAsync(GetUrl(limit: 10));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(body);
        var pagingMetadata = body["paging_metadata"]?.AsObject();
        Assert.NotNull(pagingMetadata);
        Assert.Equal(ExpectedCursor, pagingMetadata["cursor"]?.GetValue<string>());
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WithValidLimitAndCursor_ReturnsOkAsync()
    {
        // Arrange
        var cursor = EncodeBase64Url("next-page-token");
        var elementList = new SubmodelElementsPage
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), 5, cursor, Arg.Any<CancellationToken>())
            .Returns(elementList);

        // Act
        var response = await _client.GetAsync(GetUrl(limit: 5, cursor: cursor));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("deep")]
    [InlineData("core")]
    public async Task GetAllSubmodelElementsAsync_WithLevelQueryParam_ReturnsOkAsync(string level)
    {
        // Arrange
        var elementList = new SubmodelElementsPage
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(elementList);

        // Act
        var response = await _client.GetAsync(GetUrl() + $"?level={level}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("withBlobValue")]
    [InlineData("withoutBlobValue")]
    public async Task GetAllSubmodelElementsAsync_WithExtentQueryParam_ReturnsOkAsync(string extent)
    {
        // Arrange
        var elementList = new SubmodelElementsPage
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(SubmodelId, Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(elementList);

        // Act
        var response = await _client.GetAsync(GetUrl() + $"?extent={extent}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_ResponseBodyContainsResultAndPagingMetadata_Async()
    {
        // Arrange
        var elementList = new SubmodelElementsPage
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            Result = []
        };

        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(elementList);

        // Act
        var response = await _client.GetAsync(GetUrl());

        // Assert
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("result"), "Response body must contain a 'result' field.");
        Assert.True(body.ContainsKey("paging_metadata"), "Response body must contain a 'paging_metadata' field.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetAllSubmodelElementsAsync_WithInvalidLimit_Returns400Async(int invalidLimit)
    {
        // Act
        var response = await _client.GetAsync(GetUrl(limit: invalidLimit));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WithInvalidBase64SubmodelId_Returns400Async()
    {
        // Act
        var response = await _client.GetAsync("/submodels/not!!valid%%base64/submodel-elements");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WhenSubmodelNotFound_Returns404Async()
    {
        // Arrange
        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new SubmodelNotFoundException());

        // Act
        var response = await _client.GetAsync(GetUrl());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WhenServiceThrowsInternalDataProcessingException_Returns500Async()
    {
        // Arrange
        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new InternalDataProcessingException());

        // Act
        var response = await _client.GetAsync(GetUrl());

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GetAllSubmodelElementsAsync_WhenServiceThrowsUnexpectedException_Returns500Async()
    {
        // Arrange
        _ = _mockSubmodelRepositoryService
            .GetAllSubmodelElementsAsync(Arg.Any<string>(), Arg.Any<SubmodelQueryOptions?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Unexpected failure"));

        // Act
        var response = await _client.GetAsync(GetUrl());

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
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

public class GetAllSubmodelElementsControllerTestsV1Config() : GetAllSubmodelElementsControllerTests("v1-config");

public class GetAllSubmodelElementsControllerTestsV2Config() : GetAllSubmodelElementsControllerTests("v2-config");
