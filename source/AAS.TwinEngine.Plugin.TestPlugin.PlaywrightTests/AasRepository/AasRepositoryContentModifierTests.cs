namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.AasRepository;

/// <summary>
/// Content-modifier tests ($metadata, $value, $reference, $path) for the AAS Repository routes
/// </summary>
public class AasRepositoryContentModifierTests : ApiTestBase
{
    private const string Area = "AasRepository";
    private const string ListIndexPath = "Documents%255B0%255D";
    private const string NestedListIndexPath = "Documents%255B0%255D.DocumentIds%255B1%255D";

    private string ContactInfoRoute => $"/shells/{AasIdentifier}/submodels/{SubmodelIdentifierContact}";

    private string HandoverDocumentationRoute => $"/shells/{AasIdentifier}/submodels/{SubmodelIdentifierHandoverDocumentation}";

    [Fact]
    public async Task GetAllShells_Reference_WithLimit1_ShouldReturnSuccess_ContentAsExpected()
    {
        await AssertContentAsExpectedAsync("/shells/$reference?limit=1", Area, "GetAllShells_Reference_WithLimit1_Expected.json");
    }

    [Fact]
    public async Task GetShell_Reference_ShouldReturnSuccess_ContentAsExpected()
    {
        await AssertContentAsExpectedAsync($"/shells/{AasIdentifier}/$reference", Area, "GetShell_Reference_Expected.json");
    }

    [Theory]
    [InlineData("$metadata", "Shell_GetSubmodel_ContactInfo_Metadata")]
    [InlineData("$value", "Shell_GetSubmodel_ContactInfo_Value")]
    [InlineData("$value?level=core", "Shell_GetSubmodel_ContactInfo_Value_LevelCore")]
    [InlineData("$value?extent=withBlobValue", "Shell_GetSubmodel_ContactInfo_Value_ExtentWithBlobValue")]
    [InlineData("$reference", "Shell_GetSubmodel_ContactInfo_Reference")]
    [InlineData("$reference?level=core", "Shell_GetSubmodel_ContactInfo_Reference_LevelCore")]
    [InlineData("$path", "Shell_GetSubmodel_ContactInfo_Path")]
    [InlineData("$path?level=core", "Shell_GetSubmodel_ContactInfo_Path_LevelCore")]
    public async Task GetSubmodel_ContactInfo_WithContentModifier_ShouldReturnSuccess_ContentAsExpected(string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"{ContactInfoRoute}/{modifier}", Area, $"{expectedFile}_Expected.json");
    }

    [Theory]
    [InlineData("$metadata", "Shell_GetSubmodel_HandoverDocumentation_Metadata")]
    [InlineData("$value", "Shell_GetSubmodel_HandoverDocumentation_Value")]
    [InlineData("$reference", "Shell_GetSubmodel_HandoverDocumentation_Reference")]
    [InlineData("$path", "Shell_GetSubmodel_HandoverDocumentation_Path")]
    public async Task GetSubmodel_HandoverDocumentation_WithContentModifier_ShouldReturnSuccess_ContentAsExpected(string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"{HandoverDocumentationRoute}/{modifier}", Area, $"{expectedFile}_Expected.json");
    }

    [Theory]
    [InlineData("$metadata", "Shell_GetAllSubmodelElements_ContactInfo_Metadata")]
    [InlineData("$value", "Shell_GetAllSubmodelElements_ContactInfo_Value")]
    [InlineData("$reference", "Shell_GetAllSubmodelElements_ContactInfo_Reference")]
    [InlineData("$path", "Shell_GetAllSubmodelElements_ContactInfo_Path")]
    [InlineData("$metadata?limit=1", "Shell_GetAllSubmodelElements_ContactInfo_Metadata_WithLimit1")]
    [InlineData("$value?limit=1", "Shell_GetAllSubmodelElements_ContactInfo_Value_WithLimit1")]
    [InlineData("$reference?limit=1", "Shell_GetAllSubmodelElements_ContactInfo_Reference_WithLimit1")]
    [InlineData("$path?limit=1", "Shell_GetAllSubmodelElements_ContactInfo_Path_WithLimit1")]
    public async Task GetAllSubmodelElements_ContactInfo_WithContentModifier_ShouldReturnSuccess_ContentAsExpected(string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"{ContactInfoRoute}/submodel-elements/{modifier}", Area, $"{expectedFile}_Expected.json");
    }

    [Theory]
    [InlineData("ContactInformation1", "$metadata", "Shell_GetSubmodelElement_ContactInfo_ContactInformation1_Metadata")]
    [InlineData("ContactInformation1", "$value", "Shell_GetSubmodelElement_ContactInfo_ContactInformation1_Value")]
    [InlineData("ContactInformation1", "$value?level=core", "Shell_GetSubmodelElement_ContactInfo_ContactInformation1_Value_LevelCore")]
    [InlineData("ContactInformation1", "$reference", "Shell_GetSubmodelElement_ContactInfo_ContactInformation1_Reference")]
    [InlineData("ContactInformation1", "$reference?level=core", "Shell_GetSubmodelElement_ContactInfo_ContactInformation1_Reference_LevelCore")]
    [InlineData("ContactInformation1", "$path", "Shell_GetSubmodelElement_ContactInfo_ContactInformation1_Path")]
    [InlineData("ContactInformation1.RoleOfContactPerson", "$metadata", "Shell_GetSubmodelElement_ContactInfo_RoleOfContactPerson_Metadata")]
    [InlineData("ContactInformation1.RoleOfContactPerson", "$value", "Shell_GetSubmodelElement_ContactInfo_RoleOfContactPerson_Value")]
    [InlineData("ContactInformation1.RoleOfContactPerson", "$reference", "Shell_GetSubmodelElement_ContactInfo_RoleOfContactPerson_Reference")]
    [InlineData("ContactInformation1.Company", "$metadata", "Shell_GetSubmodelElement_ContactInfo_Company_Metadata")]
    [InlineData("ContactInformation1.Company", "$value", "Shell_GetSubmodelElement_ContactInfo_Company_Value")]
    [InlineData("ContactInformation1.Company", "$reference", "Shell_GetSubmodelElement_ContactInfo_Company_Reference")]
    public async Task GetSubmodelElement_ContactInfo_WithContentModifier_ShouldReturnSuccess_ContentAsExpected(string idShortPath, string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"{ContactInfoRoute}/submodel-elements/{idShortPath}/{modifier}", Area, $"{expectedFile}_Expected.json");
    }

    [Theory]
    [InlineData(ListIndexPath, "$metadata", "Shell_GetSubmodelElement_HandoverDocumentation_Documents0_Metadata")]
    [InlineData(ListIndexPath, "$value", "Shell_GetSubmodelElement_HandoverDocumentation_Documents0_Value")]
    [InlineData(ListIndexPath, "$reference", "Shell_GetSubmodelElement_HandoverDocumentation_Documents0_Reference")]
    [InlineData(ListIndexPath, "$path", "Shell_GetSubmodelElement_HandoverDocumentation_Documents0_Path")]
    [InlineData(NestedListIndexPath, "$value", "Shell_GetSubmodelElement_HandoverDocumentation_Documents0_DocumentIds1_Value")]
    [InlineData(NestedListIndexPath, "$reference", "Shell_GetSubmodelElement_HandoverDocumentation_Documents0_DocumentIds1_Reference")]
    public async Task GetSubmodelElement_HandoverDocumentation_ListIndexPath_WithContentModifier_ShouldReturnSuccess_ContentAsExpected(string idShortPath, string modifier, string expectedFile)
    {
        await AssertContentAsExpectedAsync($"{HandoverDocumentationRoute}/submodel-elements/{idShortPath}/{modifier}", Area, $"{expectedFile}_Expected.json");
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
        await AssertStatusAsync($"{ContactInfoRoute}/{relativeUrl}", 400);
    }

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodel_ForSubmodelOfAnotherShell_WithContentModifier_ShouldReturnNotFound(string modifier)
    {
        var foreignSubmodel = Base64EncodeUrl("https://mm-software.com/submodel/000-002/ContactInformation");

        await AssertStatusAsync($"/shells/{AasIdentifier}/submodels/{foreignSubmodel}/{modifier}", 404);
    }

    [Theory]
    [InlineData("$metadata")]
    [InlineData("$value")]
    [InlineData("$reference")]
    [InlineData("$path")]
    public async Task GetSubmodelElement_UnknownIdShortPath_WithContentModifier_ShouldReturnNotFound(string modifier)
    {
        await AssertStatusAsync($"{ContactInfoRoute}/submodel-elements/DoesNotExist/{modifier}", 404);
    }

    [Theory(Skip = "Known defect (PR #282 review F2/F4): invalid or out-of-range list index paths return 500 instead of 404.")]
    [InlineData("DoesNotExist%255B0%255D")]
    [InlineData("Documents%255B99%255D")]
    [InlineData("Documents%255B99999999999%255D")]
    public async Task GetSubmodelElement_InvalidListIndexPath_Value_ShouldReturnNotFound(string idShortPath)
    {
        await AssertStatusAsync($"{HandoverDocumentationRoute}/submodel-elements/{idShortPath}/$value", 404);
    }
}
