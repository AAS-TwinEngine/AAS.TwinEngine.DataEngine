using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.ContentModifiers;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Content-modifier tests for the Submodel Repository routes
/// </summary>
public class SubmodelRepositoryContentModifierTests : ContentModifierTestsBase
{
    private const string ListIndexElementPath = "Documents%255B0%255D";
    private const string NestedListIndexElementPath = "Documents%255B0%255D.DocumentIds%255B1%255D";
    private const string DeepListIndexElementPath = "Documents%255B0%255D.DocumentVersions%255B0%255D.Language%255B0%255D";

    protected override string SubmodelRoute => BuildSubmodelRoute(SubmodelIdentifierContact);

    protected override string DecodedSubmodelId => "https://mm-software.com/submodel/000-001/ContactInformation";

    protected override string CollectionIdShortPath => "ContactInformation1";

    protected override string LeafIdShortPath => "ContactInformation1.RoleOfContactPerson";

    protected override string MlpIdShortPath => "ContactInformation1.Company";

    protected override string BuildSubmodelRoute(string encodedSubmodelId) => $"/submodels/{encodedSubmodelId}";

    private string HandoverRoute => BuildSubmodelRoute(SubmodelIdentifierHandoverDocumentation);

    private const string DecodedHandoverId = "https://mm-software.com/submodel/000-001/HandoverDocumentation";

    // ---------- GET /submodels/$... ----------

    [Fact]
    public async Task GetAllSubmodels_Metadata_WithLimit1_ShouldReturnSubmodelsWithoutElements()
    {
        // Arrange
        var normal = GetPagedResult(await GetJsonAsync("/submodels?limit=1")).AsArray();
        var expected = ContentModifierExpectations.SubmodelMetadataList(normal);

        // Act
        var actual = await GetJsonAsync("/submodels/$metadata?limit=1");

        // Assert
        Assert.NotEmpty(normal);
        AssertJsonEqual(expected, GetPagedResult(actual));
    }

    [Fact]
    public async Task GetAllSubmodels_Reference_WithLimit1_ShouldReturnSubmodelReferences()
    {
        // Arrange
        var normal = GetPagedResult(await GetJsonAsync("/submodels?limit=1")).AsArray();
        var expected = ContentModifierExpectations.SubmodelReferenceList(normal);

        // Act
        var actual = await GetJsonAsync("/submodels/$reference?limit=1");

        // Assert
        Assert.NotEmpty(normal);
        AssertJsonEqual(expected, GetPagedResult(actual));
    }

    [Fact]
    public async Task GetAllSubmodels_Path_WithLimit1_ShouldReturnPathsOfAllElements()
    {
        // Arrange
        var normal = GetPagedResult(await GetJsonAsync("/submodels?limit=1")).AsArray();
        var expected = normal.SelectMany(s => ContentModifierExpectations.SubmodelPaths(s!)).ToList();

        // Act
        var actual = await GetJsonAsync("/submodels/$path?limit=1");

        // Assert
        Assert.NotEmpty(expected);
        AssertPaths(expected, GetPagedResult(actual));
    }

    [Fact]
    public async Task GetAllSubmodels_Value_WithLimit1_ShouldReturnValueOnlyObjects()
    {
        // Arrange
        var normal = GetPagedResult(await GetJsonAsync("/submodels?limit=1")).AsArray();

        // Act
        var actual = GetPagedResult(await GetJsonAsync("/submodels/$value?limit=1"));

        // Assert
        Assert.NotEmpty(normal);
        Assert.True(actual is JsonArray, "Expected result to be a JSON array.");
        var actualItems = actual.AsArray();
        Assert.Equal(normal.Count, actualItems.Count);
        for (var i = 0; i < normal.Count; i++)
        {
            ContentModifierExpectations.AssertSubmodelValue(actualItems[i], normal[i]!);
        }
    }

    [Theory]
    [InlineData("$metadata?limit=1&level=core")]
    [InlineData("$metadata?limit=1&extent=withBlobValue")]
    [InlineData("$reference?limit=1&level=deep")]
    public async Task GetAllSubmodels_WithUnsupportedModifierCombination_ShouldReturnBadRequest(string suffixAndQuery)
    {
        await AssertStatusAsync($"/submodels/{suffixAndQuery}", 400);
    }

    // ---------- List-index idShortPaths (HandoverDocumentation) ----------

    [Theory]
    [InlineData(ListIndexElementPath)]
    [InlineData(NestedListIndexElementPath)]
    [InlineData(DeepListIndexElementPath)]
    public async Task GetSubmodelElement_HandoverDocumentation_ListIndexPath_Reference_ShouldReturnKeysWithListIndex(string urlPath)
    {
        await AssertElementReferenceAsync(HandoverRoute, DecodedHandoverId, urlPath);
    }

    [Fact]
    public async Task GetSubmodelElement_HandoverDocumentation_ListIndexPath_Path_ShouldReturnIndexedPaths()
    {
        await AssertElementPathAsync(HandoverRoute, ListIndexElementPath);
    }

    [Fact]
    public async Task GetSubmodelElement_HandoverDocumentation_ListIndexPath_Metadata_ShouldReturnElementWithoutValue()
    {
        await AssertElementMetadataAsync(HandoverRoute, ListIndexElementPath);
    }

    [Fact]
    public async Task GetSubmodelElement_HandoverDocumentation_ListIndexPath_Value_ShouldReturnValueOnlyObject()
    {
        await AssertElementValueAsync(HandoverRoute, ListIndexElementPath);
    }

    [Fact]
    public async Task GetSubmodel_HandoverDocumentation_Path_ShouldReturnIndexedPaths()
    {
        // Arrange
        var normal = await GetJsonAsync(HandoverRoute);
        var expected = ContentModifierExpectations.SubmodelPaths(normal);

        // Act
        var actual = await GetJsonAsync($"{HandoverRoute}/$path");

        // Assert
        Assert.Contains("Documents[0]", expected);
        AssertPaths(expected, actual);
    }

    [Fact]
    public async Task GetSubmodel_HandoverDocumentation_Value_ShouldReturnValueOnlyObject()
    {
        // Arrange
        var normal = await GetJsonAsync(HandoverRoute);

        // Act
        var actual = await GetJsonAsync($"{HandoverRoute}/$value");

        // Assert
        ContentModifierExpectations.AssertSubmodelValue(actual, normal);
    }
}
