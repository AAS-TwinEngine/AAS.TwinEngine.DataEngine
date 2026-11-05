using System.Text.Json;

using Microsoft.Playwright;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for appropriate serialization endpoints
/// </summary>
public class SerializationTests : ApiTestBase
{
    private string SerializationUrl =>
        $"/serialization" +
        $"?aasIds={AasIdentifier}" +
        $"&submodelIds={SubmodelIdentifierContact}" +
        $"&submodelIds={SubmodelIdentifierHandoverDocumentation}" +
        $"&submodelIds={SubmodelIdentifierCustomSubmodel}" +
        $"&includeConceptDescriptions=false";

    [Fact]
    public async Task GetAppropriateSerialization_WithMultipleSubmodels_ShouldReturnSuccess()
    {
        // Arrange & Act
        var response = await GetSerializationAsync("application/asset-administration-shell-package+xml");
        var content = await response.TextAsync();

        // Assert
        Assert.Equal("application/asset-administration-shell-package+xml", GetContentType(response));
        Assert.False(string.IsNullOrEmpty(content));

        Assert.Contains("https://mm-software.com/submodel/000-003/HandoverDocumentation", content, StringComparison.Ordinal);
        Assert.Contains("https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation", content, StringComparison.Ordinal);
        Assert.Contains("http://schemas.openxmlformats.org/package/2006/relationships", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAppropriateSerialization_WithJsonAcceptHeader_ShouldReturnJsonEnvironment()
    {
        // Arrange & Act
        var response = await GetSerializationAsync("application/json");
        var content = await response.TextAsync();

        // Assert
        Assert.Equal("application/json", GetContentType(response));

        using var environment = JsonDocument.Parse(content);
        Assert.True(environment.RootElement.TryGetProperty("assetAdministrationShells", out _));
        Assert.True(environment.RootElement.TryGetProperty("submodels", out _));

        Assert.Contains("https://mm-software.com/submodel/000-003/HandoverDocumentation", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAppropriateSerialization_WithXmlAcceptHeader_ShouldReturnXmlEnvironment()
    {
        // Arrange & Act
        var response = await GetSerializationAsync("application/xml");
        var content = await response.TextAsync();

        // Assert
        Assert.Equal("application/xml", GetContentType(response));
        Assert.StartsWith("<?xml", content, StringComparison.Ordinal);
        Assert.Contains("environment", content, StringComparison.Ordinal);
        Assert.Contains("https://mm-software.com/submodel/000-003/HandoverDocumentation", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAppropriateSerialization_WithUnsupportedAcceptHeader_ShouldReturnBadRequest()
    {
        // Arrange & Act
        var response = await GetSerializationAsync("text/csv");

        // Assert
        Assert.Equal(400, response.Status);
    }

    private Task<IAPIResponse> GetSerializationAsync(string acceptHeader)
        => ApiContext.GetAsync(SerializationUrl,
                               new APIRequestContextOptions
                               {
                                   Headers = new Dictionary<string, string> { { "Accept", acceptHeader } }
                               });

    private static string? GetContentType(IAPIResponse response)
        => response.Headers.TryGetValue("content-type", out var contentType)
               ? contentType.Split(';')[0].Trim()
               : null;
}
