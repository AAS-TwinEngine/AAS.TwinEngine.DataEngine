using System.Text.Json;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.Discovery;

/// <summary>
/// Tests for Discovery endpoints
/// </summary>
public class DiscoveryTests : ApiTestBase
{
    [Fact]
    public async Task SearchShellsByAssetLink_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        const string url = "/lookup/shellsByAssetLink";
        var assetLinks = new[]
        {
            new
            {
                name = "SerialNumber",
                value = "SN-4711"
            }
        };

        // Act
        var response = await ApiContext.PostAsync(url, new()
        {
            DataObject = assetLinks
        });

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "Discovery", "TestData", "SearchShellByAssetLink_Expected.json"));
    }

    [Fact]
    public async Task SearchShellsByAssetLink_WithMultipleFilters_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        const string url = "/lookup/shellsByAssetLink";
        var assetLinks = new[]
        {
            new
            {
                name = "SerialNumber",
                value = "SN-4711"
            },
            new
            {
                name = "BatchId",
                value = "B-2026-03"
            }
        };

        // Act
        var response = await ApiContext.PostAsync(url, new()
        {
            DataObject = assetLinks
        });

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "Discovery", "TestData", "SearchShellByAssetLinksMultipleFilters_Expected.json"));
    }
}
