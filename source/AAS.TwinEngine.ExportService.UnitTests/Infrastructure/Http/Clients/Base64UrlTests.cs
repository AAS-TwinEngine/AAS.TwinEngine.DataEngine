using AAS.TwinEngine.ExportService.Infrastructure.Http.Clients;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Http.Clients;

public class Base64UrlTests
{
    [Theory]
    [InlineData("test", "dGVzdA")]
    [InlineData("urn:example:aas:123", "dXJuOmV4YW1wbGU6YWFzOjEyMw")]
    [InlineData("https://example.com/ids/sm/456", "aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvc20vNDU2")]
    [InlineData("a+b/c=d", "YStiL2M9ZA")]
    [InlineData(">>>???///+++===", "Pj4-Pz8_Ly8vKysrPT09")]
    [InlineData("", "")]
    public void Encode_EncodesStringToUrlSafeBase64WithoutPadding(string input, string expected)
    {
        // Act
        var result = Base64Url.Encode(input);

        // Assert
        Assert.Equal(expected, result);
        Assert.DoesNotContain('+', result);
        Assert.DoesNotContain('/', result);
        Assert.DoesNotContain('=', result);
    }
}
