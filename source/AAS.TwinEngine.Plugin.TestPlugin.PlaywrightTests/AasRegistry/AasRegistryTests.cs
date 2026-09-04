using System.Text.Json;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.AasRegistry;

/// <summary>
/// Tests for AAS Registry endpoints
/// </summary>
public class AasRegistryTests : ApiTestBase
{
    [Fact]
    public async Task GetAllShellDescriptors_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = "/shell-descriptors";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "AasRegistry", "TestData", "GetAllShellDescriptors_Expected.json"));
    }

    [Fact]
    public async Task GetAllShellDescriptors_WithPagination()
    {
        // Arrange
        var urlLimit2 = "/shell-descriptors?limit=2";
        var urlLimit3 = "/shell-descriptors?limit=3";

        // Act

        var responseLimit2 = await ApiContext.GetAsync(urlLimit2);
        var responseLimit3 = await ApiContext.GetAsync(urlLimit3);

        // Assert
        AssertSuccessResponse(responseLimit2);
        AssertSuccessResponse(responseLimit3);

        var contentLimit2 = await responseLimit2.TextAsync();
        var contentLimit3 = await responseLimit3.TextAsync();

        Assert.False(string.IsNullOrEmpty(contentLimit2));
        Assert.False(string.IsNullOrEmpty(contentLimit3));

        var jsonLimit2 = JsonDocument.Parse(contentLimit2);
        var jsonLimit3 = JsonDocument.Parse(contentLimit3);

        Assert.NotNull(jsonLimit2);
        Assert.NotNull(jsonLimit3);

        // Verify that limit 3 contains one more element than limit 2
        var resultLimit2 = jsonLimit2.RootElement.GetProperty("result");
        var resultLimit3 = jsonLimit3.RootElement.GetProperty("result");

        var countLimit2 = resultLimit2.GetArrayLength();
        var countLimit3 = resultLimit3.GetArrayLength();

        Assert.Equal(countLimit2 + 1, countLimit3);
    }

    [Fact]
    public async Task GetAllShellDescriptors_WithLimitAndCursor()
    {
        // Arrange
        var urlLimit2 = "/shell-descriptors?limit=2";

        // Act
        var responseLimit2 = await ApiContext.GetAsync(urlLimit2);

        // Assert
        AssertSuccessResponse(responseLimit2);

        var contentLimit2 = await responseLimit2.TextAsync();
        Assert.False(string.IsNullOrEmpty(contentLimit2));

        var jsonLimit2 = JsonDocument.Parse(contentLimit2);
        var resultLimit2 = jsonLimit2.RootElement.GetProperty("result");
        Assert.Equal(2, resultLimit2.GetArrayLength());

        var cursor = jsonLimit2.RootElement.GetProperty("paging_metadata").GetProperty("cursor").GetString();
        Assert.False(string.IsNullOrEmpty(cursor));

        // Act - request next page using the cursor returned above
        var urlWithCursor = $"/shell-descriptors?limit=2&cursor={cursor}";
        var responseWithCursor = await ApiContext.GetAsync(urlWithCursor);

        // Assert
        AssertSuccessResponse(responseWithCursor);

        var contentWithCursor = await responseWithCursor.TextAsync();
        Assert.False(string.IsNullOrEmpty(contentWithCursor));

        var jsonWithCursor = JsonDocument.Parse(contentWithCursor);
        var resultWithCursor = jsonWithCursor.RootElement.GetProperty("result");

        // Ensure the second page does not repeat the first element of the first page
        var firstIdPage1 = resultLimit2[0].GetProperty("id").GetString();
        var firstIdPage2 = resultWithCursor[0].GetProperty("id").GetString();
        Assert.NotEqual(firstIdPage1, firstIdPage2);
    }

    [Fact]
    public async Task GetShellDescriptorById_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shell-descriptors/{AasIdentifier}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var actualDoc = JsonDocument.Parse(content);
        Assert.NotNull(actualDoc);

        await CompareJsonAsync(actualDoc, Path.Combine(Directory.GetCurrentDirectory(), "AasRegistry", "TestData", "GetShellDescriptorById_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodelDescriptorsByAasId_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/shell-descriptors/{AasIdentifier}/submodel-descriptors";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var actualDoc = JsonDocument.Parse(content);
        Assert.NotNull(actualDoc);

        await CompareJsonAsync(actualDoc, Path.Combine(Directory.GetCurrentDirectory(), "AasRegistry", "TestData", "GetAllSubmodelDescriptorsByAasId_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodelDescriptorsByAasId_WithPagination()
    {
        // Arrange
        var urlLimit2 = $"/shell-descriptors/{AasIdentifier}/submodel-descriptors?limit=2";
        var urlLimit3 = $"/shell-descriptors/{AasIdentifier}/submodel-descriptors?limit=3";

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
    public async Task GetSubmodelDescriptorByAasId_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url =
            $"/shell-descriptors/{AasIdentifier}/submodel-descriptors/{SubmodelIdentifierHandoverDocumentation}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var actualDoc = JsonDocument.Parse(content);
        Assert.NotNull(actualDoc);

        await CompareJsonAsync(actualDoc, Path.Combine(Directory.GetCurrentDirectory(), "AasRegistry", "TestData", "GetSubmodelDescriptorByAasId_Expected.json"));
    }
}
