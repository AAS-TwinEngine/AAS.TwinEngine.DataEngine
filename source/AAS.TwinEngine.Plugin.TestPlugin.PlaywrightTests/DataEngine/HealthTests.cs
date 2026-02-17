namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.DataEngine;

/// <summary>
/// Tests for Data Engine health endpoint
/// </summary>
public class HealthTests : ApiTestBase
{
    [Fact]
    public async Task GetHealth_ShouldReturnSuccess_EqualsHealthy()
    {
        // Arrange
        var url = "/healthz";

        // Act
        var response = await ApiContext.GetAsync(url);

        // Assert
        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.Equal("Healthy", content);
    }
}
