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
        var idShort = "M&M03";
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
        var content = await response.TextAsync();
        var json = JsonDocument.Parse(content);

        // Assert
        Assert.Equal(400, response.Status);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_WithInvalidLimit_Expected.json"));
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
        var content = await response.TextAsync();
        var json = JsonDocument.Parse(content);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_WithInvalidCursorEncoding_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_WithUnknownCursorValue_ShouldReturnFirstPage()
    {
        // Arrange
        const string firstPageUrl = "/shells?limit=1";
        var unknownCursor = EncodeBase64Url("https://mm-software.com/ids/aas/000-004");

        // Act
        var firstPageResponse = await ApiContext.GetAsync(firstPageUrl);
        var unknownCursorResponse = await ApiContext.GetAsync($"/shells?limit=1&cursor={unknownCursor}");

        // Assert
        AssertSuccessResponse(firstPageResponse);
        AssertSuccessResponse(unknownCursorResponse);

        var firstRoot = await ParseResponseRootAsync(firstPageResponse);
        var unknownRoot = await ParseResponseRootAsync(unknownCursorResponse);

        var firstId = firstRoot.GetProperty("result")[0].GetProperty("id").GetString();
        var unknownCursorId = unknownRoot.GetProperty("result")[0].GetProperty("id").GetString();

        Assert.Equal(firstId, unknownCursorId);
        var json = JsonDocument.Parse(unknownRoot.GetRawText());
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRepository", "TestData", "GetAllShells_WithUnknownCursorValue_Expected.json"));
    }

    [Fact]
    public async Task GetAllShells_ByMultipleAssetIds_ShouldApplyAndFilter()
    {
        // Arrange
        var serialFilter = EncodeBase64Url("{\"name\":\"SerialNumber\",\"value\":\"SN-1111\"}");
        var batchFilter = EncodeBase64Url("{\"name\":\"BatchId\",\"value\":\"B-2026-08\"}");
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
            AssertShellContainsSpecificAssetId(shell, "SerialNumber", "SN-1111");
            AssertShellContainsSpecificAssetId(shell, "BatchId", "B-2026-08");
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

        // Verify it's valid JSON
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
