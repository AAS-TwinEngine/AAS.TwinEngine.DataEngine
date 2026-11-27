namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Content-modifier tests ($metadata, $value, $reference, $path) for the Submodel Repository routes
/// </summary>
public class SubmodelRepositoryContentModifierTests : ApiTestBase
{
    private const string Area = "SubmodelRepository";
    private const string ListIndexPath = "Documents%255B0%255D";
    private const string NestedListIndexPath = "Documents%255B0%255D.DocumentIds%255B1%255D";

    [Theory]
    [InlineData("$metadata", "GetAllSubmodels_Metadata_WithLimit1")]
    [InlineData("$value", "GetAllSubmodels_Value_WithLimit1")]
    [InlineData("$reference", "GetAllSubmodels_Reference_WithLimit1")]
    [InlineData("$path", "GetAllSubmodels_Path_WithLimit1")]
    public async Task GetAllSubmodels_WithContentModifierAndLimit1_ShouldReturnSuccess_ContentAsExpected(string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"/submodels/{modifier}?limit=1", Area, $"{expectedFile}_Expected.json");
    }

    [Theory]
    [InlineData("$metadata", "GetSubmodel_ContactInfo_Metadata")]
    [InlineData("$value", "GetSubmodel_ContactInfo_Value")]
    [InlineData("$value?level=core", "GetSubmodel_ContactInfo_Value_LevelCore")]
    [InlineData("$value?extent=withBlobValue", "GetSubmodel_ContactInfo_Value_ExtentWithBlobValue")]
    [InlineData("$reference", "GetSubmodel_ContactInfo_Reference")]
    [InlineData("$reference?level=core", "GetSubmodel_ContactInfo_Reference_LevelCore")]
    [InlineData("$path", "GetSubmodel_ContactInfo_Path")]
    [InlineData("$path?level=core", "GetSubmodel_ContactInfo_Path_LevelCore")]
    public async Task GetSubmodel_ContactInfo_WithContentModifier_ShouldReturnSuccess_ContentAsExpected(string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"/submodels/{SubmodelIdentifierContact}/{modifier}", Area, $"{expectedFile}_Expected.json");
    }

    [Theory]
    [InlineData("$metadata", "GetSubmodel_HandoverDocumentation_Metadata")]
    [InlineData("$value", "GetSubmodel_HandoverDocumentation_Value")]
    [InlineData("$reference", "GetSubmodel_HandoverDocumentation_Reference")]
    [InlineData("$path", "GetSubmodel_HandoverDocumentation_Path")]
    public async Task GetSubmodel_HandoverDocumentation_WithContentModifier_ShouldReturnSuccess_ContentAsExpected(string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"/submodels/{SubmodelIdentifierHandoverDocumentation}/{modifier}", Area, $"{expectedFile}_Expected.json");
    }

    [Theory]
    [InlineData("$metadata", "GetAllSubmodelElements_ContactInfo_Metadata")]
    [InlineData("$value", "GetAllSubmodelElements_ContactInfo_Value")]
    [InlineData("$reference", "GetAllSubmodelElements_ContactInfo_Reference")]
    [InlineData("$path", "GetAllSubmodelElements_ContactInfo_Path")]
    [InlineData("$metadata?limit=1", "GetAllSubmodelElements_ContactInfo_Metadata_WithLimit1")]
    [InlineData("$value?limit=1", "GetAllSubmodelElements_ContactInfo_Value_WithLimit1")]
    [InlineData("$reference?limit=1", "GetAllSubmodelElements_ContactInfo_Reference_WithLimit1")]
    [InlineData("$path?limit=1", "GetAllSubmodelElements_ContactInfo_Path_WithLimit1")]
    public async Task GetAllSubmodelElements_ContactInfo_WithContentModifier_ShouldReturnSuccess_ContentAsExpected(string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"/submodels/{SubmodelIdentifierContact}/submodel-elements/{modifier}", Area, $"{expectedFile}_Expected.json");
    }

    [Theory]
    [InlineData("ContactInformation1", "$metadata", "GetSubmodelElement_ContactInfo_ContactInformation1_Metadata")]
    [InlineData("ContactInformation1", "$value", "GetSubmodelElement_ContactInfo_ContactInformation1_Value")]
    [InlineData("ContactInformation1", "$value?level=core", "GetSubmodelElement_ContactInfo_ContactInformation1_Value_LevelCore")]
    [InlineData("ContactInformation1", "$reference", "GetSubmodelElement_ContactInfo_ContactInformation1_Reference")]
    [InlineData("ContactInformation1", "$reference?level=core", "GetSubmodelElement_ContactInfo_ContactInformation1_Reference_LevelCore")]
    [InlineData("ContactInformation1", "$path", "GetSubmodelElement_ContactInfo_ContactInformation1_Path")]
    [InlineData("ContactInformation1.RoleOfContactPerson", "$metadata", "GetSubmodelElement_ContactInfo_RoleOfContactPerson_Metadata")]
    [InlineData("ContactInformation1.RoleOfContactPerson", "$value", "GetSubmodelElement_ContactInfo_RoleOfContactPerson_Value")]
    [InlineData("ContactInformation1.RoleOfContactPerson", "$reference", "GetSubmodelElement_ContactInfo_RoleOfContactPerson_Reference")]
    [InlineData("ContactInformation1.Company", "$metadata", "GetSubmodelElement_ContactInfo_Company_Metadata")]
    [InlineData("ContactInformation1.Company", "$value", "GetSubmodelElement_ContactInfo_Company_Value")]
    [InlineData("ContactInformation1.Company", "$reference", "GetSubmodelElement_ContactInfo_Company_Reference")]
    public async Task GetSubmodelElement_ContactInfo_WithContentModifier_ShouldReturnSuccess_ContentAsExpected(string idShortPath, string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"/submodels/{SubmodelIdentifierContact}/submodel-elements/{idShortPath}/{modifier}", Area, $"{expectedFile}_Expected.json");
    }

    [Theory]
    [InlineData(ListIndexPath, "$metadata", "GetSubmodelElement_HandoverDocumentation_Documents0_Metadata")]
    [InlineData(ListIndexPath, "$value", "GetSubmodelElement_HandoverDocumentation_Documents0_Value")]
    [InlineData(ListIndexPath, "$reference", "GetSubmodelElement_HandoverDocumentation_Documents0_Reference")]
    [InlineData(ListIndexPath, "$path", "GetSubmodelElement_HandoverDocumentation_Documents0_Path")]
    [InlineData(NestedListIndexPath, "$value", "GetSubmodelElement_HandoverDocumentation_Documents0_DocumentIds1_Value")]
    [InlineData(NestedListIndexPath, "$reference", "GetSubmodelElement_HandoverDocumentation_Documents0_DocumentIds1_Reference")]
    public async Task GetSubmodelElement_HandoverDocumentation_ListIndexPath_WithContentModifier_ShouldReturnSuccess_ContentAsExpected(string idShortPath, string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements/{idShortPath}/{modifier}", Area, $"{expectedFile}_Expected.json");
    }

    [Theory]
    [InlineData("$metadata?level=core")]
    [InlineData("$metadata?level=deep")]
    [InlineData("$metadata?extent=withBlobValue")]
    [InlineData("$reference?level=deep")]
    [InlineData("submodel-elements/$metadata?level=core")]
    [InlineData("submodel-elements/$metadata?extent=withBlobValue")]
    [InlineData("submodel-elements/$reference?level=deep")]
    [InlineData("submodel-elements/ContactInformation1/$metadata?level=deep")]
    [InlineData("submodel-elements/ContactInformation1/$metadata?extent=withBlobValue")]
    [InlineData("submodel-elements/ContactInformation1/$reference?level=deep")]
    [InlineData("submodel-elements/ContactInformation1.RoleOfContactPerson/$path")]
    [InlineData("submodel-elements/ContactInformation1.Company/$path")]
    public async Task GetSubmodel_ContactInfo_WithUnsupportedModifierCombination_ShouldReturnBadRequest(string relativeUrl)
    {
        await AssertStatusAsync($"/submodels/{SubmodelIdentifierContact}/{relativeUrl}", 400);
    }

    [Theory]
    [InlineData("$metadata?limit=1&level=core")]
    [InlineData("$metadata?limit=1&extent=withBlobValue")]
    [InlineData("$reference?limit=1&level=deep")]
    public async Task GetAllSubmodels_WithUnsupportedModifierCombination_ShouldReturnBadRequest(string modifierAndQuery)
    {
        await AssertStatusAsync($"/submodels/{modifierAndQuery}", 400);
    }

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodel_UnknownSubmodel_WithContentModifier_ShouldReturnNotFound(string modifier)
    {
        var unknownSubmodel = Base64EncodeUrl("https://example.com/submodel/does-not-exist");

        await AssertStatusAsync($"/submodels/{unknownSubmodel}/{modifier}", 404);
    }

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodel_InvalidBase64Identifier_WithContentModifier_ShouldReturnBadRequest(string modifier)
    {
        await AssertStatusAsync($"/submodels/invalid!!base64/{modifier}", 400);
    }

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodelElement_UnknownIdShortPath_WithContentModifier_ShouldReturnNotFound(string modifier)
    {
        await AssertStatusAsync($"/submodels/{SubmodelIdentifierContact}/submodel-elements/DoesNotExist/{modifier}", 404);
    }

    [Fact]
    public async Task GetSubmodelElement_OutOfRangeListIndex_Reference_ShouldReturnNotFound()
    {
        await AssertStatusAsync($"/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements/Documents%255B99%255D/$reference", 404);
    }

    [Theory(Skip = "Known defect (PR #282 review F2/F4): invalid or out-of-range list index paths return 500 instead of 404.")]
    [InlineData("DoesNotExist%255B0%255D")]
    [InlineData("Documents%255B99%255D")]
    [InlineData("Documents%255B99999999999%255D")]
    public async Task GetSubmodelElement_InvalidListIndexPath_Value_ShouldReturnNotFound(string idShortPath)
    {
        await AssertStatusAsync($"/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements/{idShortPath}/$value", 404);
    }
}
