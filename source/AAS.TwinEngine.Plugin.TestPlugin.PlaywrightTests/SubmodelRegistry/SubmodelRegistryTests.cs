using System.Text.Json;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.SubmodelRegistry;

/// <summary>
/// Tests for Submodel Registry endpoints
/// </summary>
public class SubmodelRegistryTests : ApiTestBase
{
    [Fact]
    public async Task GetSubmodelDescriptorById_Contact_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodel-descriptors/{SubmodelIdentifierContact}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRegistry", "TestData", "GetSubmodelDescriptorById_Contact_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelDescriptorById_HandoverDocumentation_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodel-descriptors/{SubmodelIdentifierHandoverDocumentation}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRegistry", "TestData", "GetSubmodelDescriptorById_HandoverDocumentation_Expected.json"));
    }

    [Fact]
    public async Task GetSubmodelDescriptorById_CustomSubmodel_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodel-descriptors/{SubmodelIdentifierCustomSubmodel}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRegistry", "TestData", "GetSubmodelDescriptorById_CustomSubmodel_Expected.json"));
    }
}
