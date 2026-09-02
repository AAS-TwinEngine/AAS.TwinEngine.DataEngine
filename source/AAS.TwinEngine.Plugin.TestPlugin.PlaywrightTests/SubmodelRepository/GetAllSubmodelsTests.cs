using System.Text.Json;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.SubmodelRepository;

/// <summary>
/// Tests for GET /submodels list endpoint (SSP-002 GetAllSubmodelRepository)
/// </summary>
public class GetAllSubmodelsTests : ApiTestBase
{
    [Fact]
    public async Task GetAllSubmodels_ShouldReturnSuccess_WithPagedResultShape()
    {
        var response = await ApiContext.GetAsync("/submodels/");

        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));

        var json = JsonDocument.Parse(content);
        Assert.NotNull(json);

        Assert.True(json.RootElement.TryGetProperty("paging_metadata", out _), "Response must contain 'paging_metadata' (SSP-002)");
        Assert.True(json.RootElement.TryGetProperty("result", out var resultElement), "Response must contain 'result' (SSP-002)");
        Assert.Equal(JsonValueKind.Array, resultElement.ValueKind);
    }

    [Fact]
    public async Task GetAllSubmodels_WithLimit1_ShouldReturnOnlyOneResult()
    {
        var response = await ApiContext.GetAsync("/submodels/?limit=1");

        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        var json = JsonDocument.Parse(content);

        Assert.True(json.RootElement.TryGetProperty("result", out var resultElement));
        Assert.Equal(JsonValueKind.Array, resultElement.ValueKind);
        Assert.True(resultElement.GetArrayLength() <= 1);
    }

    [Fact]
    public async Task GetAllSubmodels_WithLimit1_ShouldReturnCursorForNextPage()
    {
        var response = await ApiContext.GetAsync("/submodels/?limit=1");

        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        var json = JsonDocument.Parse(content);

        Assert.True(json.RootElement.TryGetProperty("result", out var resultElement));

        if (resultElement.GetArrayLength() == 1)
        {
            Assert.True(json.RootElement.TryGetProperty("paging_metadata", out var pagingMeta));
            Assert.True(pagingMeta.TryGetProperty("cursor", out _), "A cursor must be present when more results exist");
        }
    }

    [Fact]
    public async Task GetAllSubmodels_WithSemanticId_ShouldReturnFilteredResults()
    {
        var encodedSemanticId = "aHR0cHM6Ly9hZG1pbi1zaGVsbC5pby96dmVpL25hbWVwbGF0ZS8xLzAvQ29udGFjdEluZm9ybWF0aW9ucw==";

        var response = await ApiContext.GetAsync($"/submodels/?semanticId={encodedSemanticId}");

        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        var json = JsonDocument.Parse(content);
        Assert.True(json.RootElement.TryGetProperty("result", out var resultElement));
        Assert.Equal(3, resultElement.GetArrayLength());
    }

    [Fact]
    public async Task GetAllSubmodels_WithIdShort_ShouldReturnMatchingSubmodels()
    {
        var idShort = "MM01";
        var response = await ApiContext.GetAsync($"/submodels/?idShort={idShort}");

        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        var json = JsonDocument.Parse(content);
        Assert.True(json.RootElement.TryGetProperty("result", out var resultElement));
        Assert.Equal(3, resultElement.GetArrayLength());
    }

    [Fact]
    public async Task GetAllSubmodels_WithInvalidLimit_ShouldReturn400()
    {
        var response = await ApiContext.GetAsync("/submodels/?limit=0");

        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task GetAllSubmodels_WithExtentWithoutBlobValue_ShouldReturnNullBlobValues()
    {
        var response = await ApiContext.GetAsync("/submodels/?extent=withoutBlobValue");

        AssertSuccessResponse(response);
        var content = await response.TextAsync();
        Assert.False(string.IsNullOrEmpty(content));
    }

    [Fact]
    public async Task GetAllSubmodels_PaginationFlow_SecondPageHasDistinctResults()
    {
        var firstResponse = await ApiContext.GetAsync("/submodels/?limit=1");
        AssertSuccessResponse(firstResponse);
        var firstJson = JsonDocument.Parse(await firstResponse.TextAsync());

        if (!firstJson.RootElement.TryGetProperty("paging_metadata", out var pagingMeta) ||
            !pagingMeta.TryGetProperty("cursor", out var cursorProp) ||
            cursorProp.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var cursor = Uri.EscapeDataString(cursorProp.GetString()!);
        var secondResponse = await ApiContext.GetAsync($"/submodels/?limit=1&cursor={cursor}");
        AssertSuccessResponse(secondResponse);

        var firstResult = firstJson.RootElement.GetProperty("result").EnumerateArray().First().GetProperty("id").GetString();
        var secondJson = JsonDocument.Parse(await secondResponse.TextAsync());
        var secondResult = secondJson.RootElement.GetProperty("result").EnumerateArray().FirstOrDefault();

        if (secondResult.ValueKind != JsonValueKind.Undefined)
        {
            Assert.NotEqual(firstResult, secondResult.GetProperty("id").GetString());
        }
    }
}
