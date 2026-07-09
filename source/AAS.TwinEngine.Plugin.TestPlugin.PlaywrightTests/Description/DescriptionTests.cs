using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.Description;

/// <summary>
/// Tests for Description endpoints
/// </summary>
public class DescriptionTests : ApiTestBase
{
    [Fact]
    public async Task GetDescription_ShouldReturnSuccess_ContentAsExpected()
    {
        // Arrange
        const string url = "/description";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);

        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        await CompareJsonAsync(json, Path.Combine(Directory.GetCurrentDirectory(), "Description", "TestData", "GetDescription_Expected.json"));
    }
}
