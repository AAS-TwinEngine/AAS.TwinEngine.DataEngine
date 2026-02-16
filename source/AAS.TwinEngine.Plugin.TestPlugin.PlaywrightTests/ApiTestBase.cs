using Microsoft.Playwright;

using System.Text;
using System.Text.Json;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests;

/// <summary>
/// Base class for API tests providing common functionality and configuration
/// </summary>
public abstract class ApiTestBase : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = false };

    protected IAPIRequestContext ApiContext { get; private set; } = null!;
    protected string BaseUrl { get; private set; } = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:8085";

    // Base64 encoded identifiers
    protected string AasIdentifier { get; private set; } = null!;
    protected string SubmodelIdentifierContact { get; private set; } = null!;
    protected string SubmodelIdentifierNameplate { get; private set; } = null!;
    protected string SubmodelIdentifierReliability { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Initialize Playwright
        var playwright = await Playwright.CreateAsync();

        // Create API request context
        ApiContext = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = BaseUrl,
            IgnoreHTTPSErrors = true,
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                { "Accept", "application/json" }
            }
        });

        // Initialize base64 encoded identifiers
        AasIdentifier = Base64EncodeUrl("https://mm-software.com/ids/aas/000-001");
        SubmodelIdentifierContact = Base64EncodeUrl("https://mm-software.com/submodel/000-001/ContactInformation");
        SubmodelIdentifierNameplate = Base64EncodeUrl("https://mm-software.com/submodel/000-001/Nameplate");
        SubmodelIdentifierReliability = Base64EncodeUrl("https://mm-software.com/submodel/000-001/Reliability");
    }

    public async Task DisposeAsync() => await ApiContext.DisposeAsync();

    /// <summary>
    /// Base64 URL encodes a string
    /// </summary>
    public static string Base64EncodeUrl(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Asserts that an API response is successful
    /// </summary>
    protected static void AssertSuccessResponse(IAPIResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        Assert.True(response.Ok, $"Expected successful response but got {response.Status}: {response.StatusText}");
    }

    /// <summary>
    /// Asserts that an JsonDocument is equals to the expected JSON content from a file
    /// </summary>
    protected static async Task CompareJsonAsync(JsonDocument actualDoc, string fullPath)
    {
        // Load expected test data and compare
        var expectedJson = await File.ReadAllTextAsync(fullPath);

        var expectedDoc = JsonDocument.Parse(expectedJson);
        Assert.NotNull(expectedDoc);


        // Compare JSON content (normalize formatting for comparison)
        var expectedNormalized = JsonSerializer.Serialize(expectedDoc, JsonSerializerOptions);
        var actualNormalized = JsonSerializer.Serialize(actualDoc, JsonSerializerOptions);
        Assert.Equal(expectedNormalized, actualNormalized);
    }
}
