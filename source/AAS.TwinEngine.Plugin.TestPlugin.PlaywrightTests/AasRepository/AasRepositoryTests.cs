using System.Text.Json;
using System.Text;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.AasRepository;

/// <summary>
/// Tests for AAS Repository endpoints
/// </summary>
public class AasRepositoryTests : ApiTestBase
{
    [Fact]
    public async Task GetAllShells_WithLimit_ShouldReturnExpectedPageSize()
    {
        // Arrange
        const string url = "/shells?limit=1";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));
        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_WithLimit_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_ByAssetIds()
    {
        // Arrange
        var assetId = EncodeBase64Url("{\"name\":\"SerialNumber\",\"value\":\"SN-1111\"}");
        var url = $"/shells?assetIds={assetId}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var root = await ParseResponseRootAsync(response);
        var result = root.GetProperty("result");
        Assert.NotEqual(0, result.GetArrayLength());

        var json = JsonDocument.Parse(root.GetRawText());
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_ByAssetIds_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_ByIdShort()
    {
        // Arrange
        var idShort = "M%26M03";
        var url = $"/shells?idShort={idShort}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var root = await ParseResponseRootAsync(response);
        var result = root.GetProperty("result");
        Assert.NotEqual(0, result.GetArrayLength());

        var json = JsonDocument.Parse(root.GetRawText());
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_ByIdShort_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_ByAssetId_And_ByIdShort()
    {
        // Arrange
        var assetId = EncodeBase64Url("{\"name\":\"SerialNumber\",\"value\":\"SN-1111\"}");
        var idShort = "M%26M03";
        var url = $"/shells?assetIds={assetId}&idShort={idShort}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var root = await ParseResponseRootAsync(response);
        var result = root.GetProperty("result");
        Assert.NotEqual(0, result.GetArrayLength());

        var json = JsonDocument.Parse(root.GetRawText());
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_ByAssetId_And_ByIdShort_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_WithCursor_ShouldReturnNextPage()
    {
        // Arrange
        const string firstPageUrl = "/shells?limit=1";

        // Act
        var firstPageResponse = await ApiContext.GetAsync(firstPageUrl);

        // Assert first page
        AssertSuccessResponse(firstPageResponse);

        var firstRoot = await ParseResponseRootAsync(firstPageResponse);
        var firstResult = firstRoot.GetProperty("result");
        Assert.Equal(1, firstResult.GetArrayLength());

        var cursor = firstRoot.GetProperty("paging_metadata").GetProperty("cursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(cursor));

        var firstPageId = firstResult[0].GetProperty("id").GetString();

        var secondPageResponse = await ApiContext.GetAsync($"/shells?limit=1&cursor={cursor}");
        AssertSuccessResponse(secondPageResponse);

        var secondRoot = await ParseResponseRootAsync(secondPageResponse);
        var secondResult = secondRoot.GetProperty("result");
        Assert.Equal(1, secondResult.GetArrayLength());

        var secondPageId = secondResult[0].GetProperty("id").GetString();
        Assert.NotEqual(firstPageId, secondPageId);
        var json = JsonDocument.Parse(secondRoot.GetRawText());
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_WithCursor_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_WithInvalidLimit_ShouldReturnBadRequest()
    {
        // Arrange
        const string url = "/shells?limit=0";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task GetAllShells_WithInvalidCursorEncoding_ShouldReturnBadRequest()
    {
        // Arrange
        const string url = "/shells?cursor=https://mm-software.com/ids/aas/000-001";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task GetAllShells_WithUnknownCursorValue_ShouldReturnInternalServerError()
    {
        // Arrange
        const string firstPageUrl = "/shells?limit=1";
        var unknownCursor = EncodeBase64Url("https://mm-software.com/ids/aas/000-004");

        // Act
        var firstPageResponse = await ApiContext.GetAsync(firstPageUrl);
        var unknownCursorResponse = await ApiContext.GetAsync($"/shells?limit=1&cursor={unknownCursor}");

        // Assert
        AssertSuccessResponse(firstPageResponse);
        Assert.Equal(500, unknownCursorResponse.Status);
    }

    [Fact]
    public async Task GetAllShells_ByMultipleAssetIds_ShouldApplyAndFilter()
    {
        // Arrange
        var serialFilter = EncodeBase64Url("{\"name\":\"SerialNumber\",\"value\":\"SN-9999\"}");
        var batchFilter = EncodeBase64Url("{\"name\":\"BatchId\",\"value\":\"B-2026-03\"}");
        var url = $"/shells?assetIds={serialFilter}&assetIds={batchFilter}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var root = await ParseResponseRootAsync(response);
        var result = root.GetProperty("result");
        Assert.NotEqual(0, result.GetArrayLength());

        foreach (var shell in result.EnumerateArray())
        {
            AssertShellContainsSpecificAssetId(shell, "SerialNumber", "SN-9999");
            AssertShellContainsSpecificAssetId(shell, "BatchId", "B-2026-03");
        }

        var json = JsonDocument.Parse(root.GetRawText());
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_ByMultipleAssetIds_Expected.json"));
    }

    [Fact]
    public async Task GetShellById_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetShellById_Expected.json"));
    }

    [Fact]
    public async Task GetAssetInformationById_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier}/asset-information";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAssetInformationById_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelRefById_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier}/submodel-refs";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetSubmodelRefById_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelByAasId_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier}/submodels/{SubmodelIdentifierCustomSubmodel}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetSubmodelByAasId_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelByAasId_WithLevelAndExtent_ShouldReturnSuccess()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier}/submodels/{SubmodelIdentifierCustomSubmodel}" + "?level=deep&extent=withoutBlobValue";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetSubmodelByAasId_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelByAasId_WithInvalidSubmodelId_ShouldReturnNotFound()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier}/submodels/invalid//submodel";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        Assert.Equal(404, response.Status);
    }

    [Fact]
    public async Task GetAllSubmodelElementsByAasId_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shells/{AasIdentifier}/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllSubmodelElementsByAasId_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodelElementsByAasId_WithPagination()
    {
        // Arrange
        var urlLimit2 = $"/shells/{AasIdentifier}/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements?limit=1";

        var urlLimit3 = $"/shells/{AasIdentifier}/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements?limit=2";

        // Act
        var responseLimit2 = await ApiContext.GetAsync(urlLimit2);
        var responseLimit3 = await ApiContext.GetAsync(urlLimit3);

        // Assert
        AssertSuccessResponse(responseLimit2);
        AssertSuccessResponse(responseLimit3);

        var jsonLimit2 = JsonDocument.Parse(await responseLimit2.TextAsync());
        var jsonLimit3 = JsonDocument.Parse(await responseLimit3.TextAsync());

        var resultLimit2 = jsonLimit2.RootElement.GetProperty("result");
        var resultLimit3 = jsonLimit3.RootElement.GetProperty("result");

        Assert.Equal(resultLimit2.GetArrayLength() + 1, resultLimit3.GetArrayLength());
    }

    [Fact]
    public async Task GetAllSubmodelElementsByAasId_WithCursor_ShouldReturnNextPage()
    {
        // Arrange
        var firstPageUrl = $"/shells/{AasIdentifier}/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements?limit=1";

        // Act
        var firstResponse = await ApiContext.GetAsync(firstPageUrl);

        // Assert first page
        AssertSuccessResponse(firstResponse);

        var firstRoot = await ParseResponseRootAsync(firstResponse);
        var firstResult = firstRoot.GetProperty("result");

        Assert.Single(firstResult.EnumerateArray());

        var cursor = firstRoot.GetProperty("paging_metadata").GetProperty("cursor").GetString();

        Assert.False(string.IsNullOrWhiteSpace(cursor));

        var firstIdShort = firstResult[0].GetProperty("idShort").GetString();

        // Act second page
        var secondResponse = await ApiContext.GetAsync($"/shells/{AasIdentifier}/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements?limit=1&cursor={cursor}");

        // Assert second page
        AssertSuccessResponse(secondResponse);

        var secondRoot = await ParseResponseRootAsync(secondResponse);
        var secondResult = secondRoot.GetProperty("result");

        Assert.Single(secondResult.EnumerateArray());

        var secondIdShort = secondResult[0].GetProperty("idShort").GetString();

        Assert.NotEqual(firstIdShort, secondIdShort);

        var json = JsonDocument.Parse(secondRoot.GetRawText());

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllSubmodelElementsByAasId_WithCursor_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodelElementsByAasId_WithInvalidLimit_ShouldReturnBadRequest()
    {
        // Arrange
        var url =
            $"/shells/{AasIdentifier}/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements?limit=0";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        Assert.Equal(400, response.Status);
    }

    private static async Task<JsonElement> ParseResponseRootAsync(Microsoft.Playwright.IAPIResponse response)
    {
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        return json.RootElement.Clone();
    }

    private static void AssertShellContainsSpecificAssetId(JsonElement shell, string expectedName, string expectedValue)
    {
        Assert.True(shell.TryGetProperty("assetInformation", out var assetInformation));
        Assert.True(assetInformation.TryGetProperty("specificAssetIds", out var specificAssetIds));
        Assert.Equal(JsonValueKind.Array, specificAssetIds.ValueKind);

        var hasExpectedAssetId = specificAssetIds.EnumerateArray().Any(assetId =>
            assetId.TryGetProperty("name", out var name) &&
            assetId.TryGetProperty("value", out var value) &&
            string.Equals(name.GetString(), expectedName, StringComparison.Ordinal) &&
            string.Equals(value.GetString(), expectedValue, StringComparison.Ordinal));

        Assert.True(hasExpectedAssetId, $"Expected shell to contain specificAssetId '{{name: {expectedName}, value: {expectedValue}}}'.");
    }

    private static string EncodeBase64Url(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
