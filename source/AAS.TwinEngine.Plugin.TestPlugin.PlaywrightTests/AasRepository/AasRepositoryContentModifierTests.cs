using AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.ContentModifiers;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.AasRepository;

/// <summary>
/// Content-modifier tests for the AAS Repository routes
/// </summary>
public class AasRepositoryContentModifierTests : ContentModifierTestsBase
{
    private const string DecodedAasId = "https://mm-software.com/ids/aas/000-001";

    protected override string SubmodelRoute => BuildSubmodelRoute(SubmodelIdentifierContact);

    protected override string DecodedSubmodelId => "https://mm-software.com/submodel/000-001/ContactInformation";

    protected override string CollectionIdShortPath => "ContactInformation1";

    protected override string LeafIdShortPath => "ContactInformation1.RoleOfContactPerson";

    protected override string MlpIdShortPath => "ContactInformation1.Company";

    protected override string BuildSubmodelRoute(string encodedSubmodelId) => $"/shells/{AasIdentifier}/submodels/{encodedSubmodelId}";

    // ---------- GET /shells/$reference ----------

    [Fact]
    public async Task GetAllShells_Reference_WithLimit1_ShouldReturnShellReferences()
    {
        // Arrange
        var normal = GetPagedResult(await GetJsonAsync("/shells?limit=1")).AsArray();
        var expected = ContentModifierExpectations.ShellReferenceList(normal);

        // Act
        var actual = await GetJsonAsync("/shells/$reference?limit=1");

        // Assert
        Assert.NotEmpty(normal);
        AssertJsonEqual(expected, GetPagedResult(actual));
    }

    [Fact]
    public async Task GetShell_Reference_ShouldReturnShellModelReference()
    {
        // Arrange
        var normal = await GetJsonAsync($"/shells/{AasIdentifier}");
        Assert.Equal(DecodedAasId, normal["id"]!.GetValue<string>());
        var expected = ContentModifierExpectations.ShellReference(DecodedAasId);

        // Act
        var actual = await GetJsonAsync($"/shells/{AasIdentifier}/$reference");

        // Assert
        AssertJsonEqual(expected, actual);
    }

    // ---------- Submodel that does not belong to the shell ----------

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$reference")]
    [InlineData("$path")]
    [InlineData("$value")]
    public async Task GetSubmodel_WithModifier_ForSubmodelOfAnotherShell_ShouldReturnClientError(string suffix)
    {
        // Arrange
        var foreignSubmodel = Base64EncodeUrl("https://mm-software.com/submodel/000-002/ContactInformation");

        // Act
        var response = await ApiContext.GetAsync($"/shells/{AasIdentifier}/submodels/{foreignSubmodel}/{suffix}");

        // Assert
        Assert.InRange(response.Status, 400, 499);
    }
}
