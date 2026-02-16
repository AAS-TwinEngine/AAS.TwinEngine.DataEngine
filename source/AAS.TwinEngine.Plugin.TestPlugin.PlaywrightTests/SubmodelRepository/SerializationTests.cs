namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for appropriate serialization endpoints
/// </summary>
public class SerializationTests : ApiTestBase
{
    [Fact]
    public async Task GetAppropriateSerialization_WithMultipleSubmodels_ShouldReturnSuccess()
    {
        // Arrange
        var url = $"/serialization" +
                  $"?aasIds={AasIdentifier}" +
                  $"&submodelIds={SubmodelIdentifierContact}" +
                  $"&submodelIds={SubmodelIdentifierNameplate}" +
                  $"&submodelIds={SubmodelIdentifierReliability}" +
                  $"&includeConceptDescriptions=false";

        // Act
        var response = await ApiContext.GetAsync(url);
        var content = await response.TextAsync();

        Assert.False(string.IsNullOrEmpty(content));

        Assert.Contains("https://mm-software.com/submodel/000-001/Nameplate", content, System.StringComparison.Ordinal);
        Assert.Contains("https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation", content, System.StringComparison.Ordinal);
        Assert.Contains("http://schemas.openxmlformats.org/package/2006/relationships", content, System.StringComparison.Ordinal);
    }
}
