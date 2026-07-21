using System.Text.Json;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for Submodel endpoints
/// </summary>
public class SubmodelTests : ApiTestBase
{
    [Fact]
    public async Task GetAllSubmodels_WithoutParameters_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        var url = $"/submodels/";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetAllSubmodels_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodels_By_SemanticId_should_return_success_and_content_as_expected()
    {
        // Arrange
        var semanticId = EncodeBase64Url("https://admin-shell.io/zvei/nameplate/1/0/ContactInformations");
        var url = $"/submodels?semanticId={semanticId}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetAllSubmodels_By_SemanticId_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodels_ByIdShort()
    {
        // Arrange
        var idShort = "M%26M03";
        var url = $"/submodels?idShort={idShort}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "SubmodelRepository", "TestData", "GetAllSubmodels_ByIdShort_Expected.json"));
    }

    [Fact]
    public async Task GetAllSubmodels_ByIdShort_WithUnknownIdShort()
    {
        // Arrange
        var idShort = "UnknownProduct";
        var url = $"/submodels?idShort={idShort}";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);
        var result = json.RootElement.GetProperty("result");
        Assert.Equal(0, result.GetArrayLength());
    }

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

    private static string EncodeBase64Url(string plainText)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
