using System.Text.Json.Nodes;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.ContentModifiers;

/// <summary>
/// Shared content-modifier tests ($metadata, $reference, $path, $value) for a submodel route.
/// Every expectation is derived from the normal representation served by the same API.
/// </summary>
public abstract class ContentModifierTestsBase : ApiTestBase
{
    /// <summary>Route of the submodel under test, e.g. /submodels/{id}.</summary>
    protected abstract string SubmodelRoute { get; }

    /// <summary>Builds the route of any submodel (by Base64 encoded id) on the same repository.</summary>
    protected abstract string BuildSubmodelRoute(string encodedSubmodelId);

    protected abstract string DecodedSubmodelId { get; }

    /// <summary>URL-encoded idShortPath of a collection element.</summary>
    protected abstract string CollectionIdShortPath { get; }

    /// <summary>URL-encoded idShortPath of a Property below the collection.</summary>
    protected abstract string LeafIdShortPath { get; }

    /// <summary>URL-encoded idShortPath of a MultiLanguageProperty below the collection.</summary>
    protected abstract string MlpIdShortPath { get; }

    private string ElementsRoute => $"{SubmodelRoute}/submodel-elements";

    // ---------- Submodel ----------

    [Fact]
    public async Task GetSubmodel_Metadata_ShouldReturnSubmodelWithoutElements()
    {
        // Arrange
        var normal = await GetJsonAsync(SubmodelRoute);
        var expected = ContentModifierExpectations.SubmodelMetadata(normal);

        // Act
        var actual = await GetJsonAsync($"{SubmodelRoute}/$metadata");

        // Assert
        Assert.Null(actual["submodelElements"]);
        AssertJsonEqual(expected, actual);
    }

    [Theory]
    [InlineData("core")]
    [InlineData("deep")]
    public async Task GetSubmodel_Metadata_WithLevel_ShouldReturnBadRequest(string level)
    {
        await AssertStatusAsync($"{SubmodelRoute}/$metadata?level={level}", 400);
    }

    [Fact]
    public async Task GetSubmodel_Metadata_WithExtentWithBlobValue_ShouldReturnBadRequest()
    {
        await AssertStatusAsync($"{SubmodelRoute}/$metadata?extent=withBlobValue", 400);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?level=core")]
    public async Task GetSubmodel_Reference_ShouldReturnSubmodelModelReference(string query)
    {
        // Arrange
        var expected = ContentModifierExpectations.SubmodelReference(DecodedSubmodelId);

        // Act
        var actual = await GetJsonAsync($"{SubmodelRoute}/$reference{query}");

        // Assert
        AssertJsonEqual(expected, actual);
    }

    [Fact]
    public async Task GetSubmodel_Reference_WithLevelDeep_ShouldReturnBadRequest()
    {
        await AssertStatusAsync($"{SubmodelRoute}/$reference?level=deep", 400);
    }

    [Fact]
    public async Task GetSubmodel_Path_ShouldReturnPathsOfAllElements()
    {
        // Arrange
        var normal = await GetJsonAsync(SubmodelRoute);
        var expected = ContentModifierExpectations.SubmodelPaths(normal);

        // Act
        var actual = await GetJsonAsync($"{SubmodelRoute}/$path");

        // Assert
        Assert.NotEmpty(expected);
        AssertPaths(expected, actual);
    }

    [Fact]
    public async Task GetSubmodel_Value_ShouldReturnValueOnlyObject()
    {
        // Arrange
        var normal = await GetJsonAsync(SubmodelRoute);

        // Act
        var actual = await GetJsonAsync($"{SubmodelRoute}/$value");

        // Assert
        ContentModifierExpectations.AssertSubmodelValue(actual, normal);
    }

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$reference")]
    [InlineData("$path")]
    [InlineData("$value")]
    public async Task GetUnknownSubmodel_WithModifier_ShouldReturnNotFound(string suffix)
    {
        // Arrange
        var unknownRoute = BuildSubmodelRoute(Base64EncodeUrl("https://mm-software.com/submodel/000-001/DoesNotExist"));

        // Act + Assert
        await AssertStatusAsync($"{unknownRoute}/{suffix}", 404);
        await AssertStatusAsync($"{unknownRoute}/submodel-elements/{suffix}", 404);
        await AssertStatusAsync($"{unknownRoute}/submodel-elements/{CollectionIdShortPath}/{suffix}", 404);
    }

    // ---------- Submodel elements (list) ----------

    [Fact]
    public async Task GetSubmodelElements_Metadata_ShouldReturnElementsWithoutValues()
    {
        // Arrange
        var normal = await GetNormalElementsAsync();
        var expected = ContentModifierExpectations.ElementMetadataList(normal);

        // Act
        var actual = await GetJsonAsync($"{ElementsRoute}/$metadata?limit=100");

        // Assert
        AssertJsonEqual(expected, GetPagedResult(actual));
    }

    [Theory]
    [InlineData("core")]
    [InlineData("deep")]
    public async Task GetSubmodelElements_Metadata_WithLevel_ShouldReturnBadRequest(string level)
    {
        await AssertStatusAsync($"{ElementsRoute}/$metadata?level={level}", 400);
    }

    [Fact]
    public async Task GetSubmodelElements_Metadata_WithExtentWithBlobValue_ShouldReturnBadRequest()
    {
        await AssertStatusAsync($"{ElementsRoute}/$metadata?extent=withBlobValue", 400);
    }

    [Theory]
    [InlineData("")]
    [InlineData("&level=core")]
    public async Task GetSubmodelElements_Reference_ShouldReturnReferencesOfElements(string extraQuery)
    {
        // Arrange
        var submodel = await GetJsonAsync(SubmodelRoute);
        var normal = await GetNormalElementsAsync();
        var expected = ContentModifierExpectations.ElementReferenceList(submodel, DecodedSubmodelId, normal);

        // Act
        var actual = await GetJsonAsync($"{ElementsRoute}/$reference?limit=100{extraQuery}");

        // Assert
        AssertJsonEqual(expected, GetPagedResult(actual));
    }

    [Fact]
    public async Task GetSubmodelElements_Reference_WithLevelDeep_ShouldReturnBadRequest()
    {
        await AssertStatusAsync($"{ElementsRoute}/$reference?level=deep", 400);
    }

    [Fact]
    public async Task GetSubmodelElements_Path_ShouldReturnPathsOfAllElements()
    {
        // Arrange
        var normal = await GetNormalElementsAsync();
        var expected = ContentModifierExpectations.ElementListPaths(normal);

        // Act
        var actual = await GetJsonAsync($"{ElementsRoute}/$path?limit=100");

        // Assert
        Assert.NotEmpty(expected);
        AssertPaths(expected, GetPagedResult(actual));
    }

    [Fact]
    public async Task GetSubmodelElements_Value_ShouldReturnValueOnlyObject()
    {
        // Arrange
        var normal = await GetNormalElementsAsync();

        // Act
        var actual = await GetJsonAsync($"{ElementsRoute}/$value?limit=100");

        // Assert
        ContentModifierExpectations.AssertElementsValue(GetPagedResult(actual), normal);
    }

    // ---------- Single submodel element ----------

    [Fact]
    public async Task GetSubmodelElement_Metadata_Collection_ShouldReturnElementWithoutValue()
    {
        await AssertElementMetadataAsync(SubmodelRoute, CollectionIdShortPath);
    }

    [Fact]
    public async Task GetSubmodelElement_Metadata_Property_ShouldReturnElementWithoutValue()
    {
        await AssertElementMetadataAsync(SubmodelRoute, LeafIdShortPath);
    }

    [Fact]
    public async Task GetSubmodelElement_Metadata_MultiLanguageProperty_ShouldReturnElementWithoutValue()
    {
        await AssertElementMetadataAsync(SubmodelRoute, MlpIdShortPath);
    }

    [Theory]
    [InlineData("core")]
    [InlineData("deep")]
    public async Task GetSubmodelElement_Metadata_WithLevel_ShouldReturnBadRequest(string level)
    {
        await AssertStatusAsync($"{ElementsRoute}/{CollectionIdShortPath}/$metadata?level={level}", 400);
    }

    [Fact]
    public async Task GetSubmodelElement_Metadata_WithExtentWithBlobValue_ShouldReturnBadRequest()
    {
        await AssertStatusAsync($"{ElementsRoute}/{CollectionIdShortPath}/$metadata?extent=withBlobValue", 400);
    }

    [Fact]
    public async Task GetSubmodelElement_Reference_Collection_ShouldReturnModelReference()
    {
        await AssertElementReferenceAsync(SubmodelRoute, DecodedSubmodelId, CollectionIdShortPath);
    }

    [Fact]
    public async Task GetSubmodelElement_Reference_Property_ShouldReturnModelReference()
    {
        await AssertElementReferenceAsync(SubmodelRoute, DecodedSubmodelId, LeafIdShortPath);
    }

    [Fact]
    public async Task GetSubmodelElement_Reference_WithLevelCore_ShouldReturnModelReference()
    {
        await AssertElementReferenceAsync(SubmodelRoute, DecodedSubmodelId, CollectionIdShortPath, "?level=core");
    }

    [Fact]
    public async Task GetSubmodelElement_Reference_WithLevelDeep_ShouldReturnBadRequest()
    {
        await AssertStatusAsync($"{ElementsRoute}/{CollectionIdShortPath}/$reference?level=deep", 400);
    }

    [Fact]
    public async Task GetSubmodelElement_Path_Collection_ShouldReturnPathsOfElementAndDescendants()
    {
        await AssertElementPathAsync(SubmodelRoute, CollectionIdShortPath);
    }

    [Fact]
    public async Task GetSubmodelElement_Path_Property_ShouldReturnBadRequest()
    {
        await AssertStatusAsync($"{ElementsRoute}/{LeafIdShortPath}/$path", 400);
    }

    [Fact]
    public async Task GetSubmodelElement_Path_MultiLanguageProperty_ShouldReturnBadRequest()
    {
        await AssertStatusAsync($"{ElementsRoute}/{MlpIdShortPath}/$path", 400);
    }

    [Fact]
    public async Task GetSubmodelElement_Value_Collection_ShouldReturnValueOnlyObject()
    {
        await AssertElementValueAsync(SubmodelRoute, CollectionIdShortPath);
    }

    [Fact]
    public async Task GetSubmodelElement_Value_Property_ShouldReturnScalar()
    {
        await AssertElementValueAsync(SubmodelRoute, LeafIdShortPath);
    }

    [Fact]
    public async Task GetSubmodelElement_Value_MultiLanguageProperty_ShouldReturnLanguageObjects()
    {
        await AssertElementValueAsync(SubmodelRoute, MlpIdShortPath);
    }

    // ---------- Shared assertions ----------

    protected async Task AssertElementMetadataAsync(string submodelRoute, string urlPath)
    {
        // Arrange
        var normal = await GetJsonAsync($"{submodelRoute}/submodel-elements/{urlPath}");
        var expected = ContentModifierExpectations.ElementMetadata(normal);

        // Act
        var actual = await GetJsonAsync($"{submodelRoute}/submodel-elements/{urlPath}/$metadata");

        // Assert
        AssertJsonEqual(expected, actual);
    }

    protected async Task AssertElementReferenceAsync(string submodelRoute, string decodedSubmodelId, string urlPath, string query = "")
    {
        // Arrange
        var submodel = await GetJsonAsync(submodelRoute);
        var expected = ContentModifierExpectations.ElementReference(submodel, decodedSubmodelId, ContentModifierExpectations.DecodeIdShortPath(urlPath));

        // Act
        var actual = await GetJsonAsync($"{submodelRoute}/submodel-elements/{urlPath}/$reference{query}");

        // Assert
        AssertJsonEqual(expected, actual);
    }

    protected async Task AssertElementPathAsync(string submodelRoute, string urlPath)
    {
        // Arrange
        var decodedPath = ContentModifierExpectations.DecodeIdShortPath(urlPath);
        var normal = await GetJsonAsync($"{submodelRoute}/submodel-elements/{urlPath}");
        var expected = ContentModifierExpectations.ElementPaths(normal, decodedPath);

        // Act
        var actual = await GetJsonAsync($"{submodelRoute}/submodel-elements/{urlPath}/$path");

        // Assert
        Assert.Contains(decodedPath, expected);
        AssertPaths(expected, actual);
    }

    protected async Task AssertElementValueAsync(string submodelRoute, string urlPath)
    {
        // Arrange
        var decodedPath = ContentModifierExpectations.DecodeIdShortPath(urlPath);
        var normal = await GetJsonAsync($"{submodelRoute}/submodel-elements/{urlPath}");

        // Act
        var actual = await GetJsonAsync($"{submodelRoute}/submodel-elements/{urlPath}/$value");

        // Assert
        ContentModifierExpectations.AssertElementValue(actual, normal, decodedPath);
    }

    protected async Task<JsonNode> GetJsonAsync(string url)
    {
        var response = await ApiContext.GetAsync(url);
        Assert.Equal(200, response.Status);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonNode.Parse(content);
        Assert.NotNull(json);
        return json;
    }

    protected async Task AssertStatusAsync(string url, int expectedStatus)
    {
        var response = await ApiContext.GetAsync(url);
        Assert.Equal(expectedStatus, response.Status);
    }

    /// <summary>Returns the `result` of a paged response after checking the paging envelope.</summary>
    protected static JsonNode GetPagedResult(JsonNode root)
    {
        var envelope = root.AsObject();
        Assert.True(envelope.ContainsKey("paging_metadata"), "Response has no paging_metadata.");
        Assert.True(envelope.ContainsKey("result"), "Response has no result.");
        return envelope["result"]!;
    }

    protected static void AssertJsonEqual(JsonNode expected, JsonNode? actual)
    {
        if (!JsonNode.DeepEquals(expected, actual))
        {
            Assert.Equal(expected.ToJsonString(), actual?.ToJsonString());
            Assert.Fail("JSON documents differ.");
        }
    }

    protected static void AssertPaths(IReadOnlyList<string> expected, JsonNode? actual)
    {
        Assert.True(actual is JsonArray, "Expected a JSON array of idShortPaths.");
        var actualPaths = actual!.AsArray().Select(p => p!.GetValue<string>()).ToList();
        Assert.Equal(expected, actualPaths);
    }

    protected async Task<JsonArray> GetNormalElementsAsync()
    {
        var normal = await GetJsonAsync($"{ElementsRoute}?limit=100");
        return GetPagedResult(normal).AsArray();
    }
}
