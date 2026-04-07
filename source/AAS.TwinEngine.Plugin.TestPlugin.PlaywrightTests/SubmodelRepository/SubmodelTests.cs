using System.Text.Json;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for Submodel endpoints
/// </summary>
public class SubmodelTests : ApiTestBase
{
    [Fact]
    public async Task GetSubmodel_ContactInfo_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierContact}/";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodel_ContactInfo_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodel_HandoverDocumentation_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierHandoverDocumentation}/";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodel_HandoverDocumentation_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodel_CustomSubmodel_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/{SubmodelIdentifierCustomSubmodel}/";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetSubmodel_CustomSubmodel_Expected.json"));
    }
}
