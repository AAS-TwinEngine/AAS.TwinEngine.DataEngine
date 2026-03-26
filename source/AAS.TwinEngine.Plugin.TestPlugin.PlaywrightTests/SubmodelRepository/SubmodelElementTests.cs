using System.Text.Json;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for Submodel Element endpoints
/// </summary>
public class SubmodelElementTests : ApiTestBase
{
    [Fact]
    public async Task GetSubmodelElement_ContactInfo_ContactInformation_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierContact}/submodel-elements/ContactInformation1";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodelElement_ContactInfo_ContactInformation_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelElement_HandoverDocumentation_Documents0_DocumentVersions0_Language0_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierHandoverDocumentation}/submodel-elements/Documents%255B0%255D.DocumentVersions%255B0%255D.Language%255B0%255D";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodelElement_HandoverDocumentation_Documents0_DocumentVersions0_Language0.json"));
    }

    [Fact]
    public async Task GetSubmodelElement_CustomSubmodel_OperatingConditionsOfReliabilityCharacteristics_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierCustomSubmodel}/submodel-elements/OperatingConditionsOfReliabilityCharacteristics";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodelElement_CustomSubmodel_OperatingConditionsOfReliabilityCharacteristics.json"));
    }
}
