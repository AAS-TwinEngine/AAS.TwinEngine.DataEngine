using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Extensions;

public class Base64UrlExtensionsTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public void DecodeBase64Url_WhenNullInput_ThrowsInvalidUserInputException()
    {
        string? encoded = null;

        var exception = Assert.Throws<InvalidUserInputException>(() =>
        encoded!.DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public void DecodeBase64Url_WhenEmptyInput_ThrowsInvalidUserInputException()
    {
        var exception = Assert.Throws<InvalidUserInputException>(() =>
        string.Empty.DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public void DecodeBase64Url_WhenWhitespaceInput_ThrowsInvalidUserInputException()
    {
        var exception = Assert.Throws<InvalidUserInputException>(() =>
            " ".DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("aGVsbG8", "hello")]
    [InlineData("dGVzdA", "test")]
    [InlineData("aHR0cHM6Ly9leGFtcGxlLmNvbQ", "https://example.com")]
    public void DecodeBase64Url_WhenValidInput_ReturnsDecodedString(string encoded, string expected)
    {
        var result = encoded.DecodeBase64Url(_logger);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DecodeBase64Url_WhenInvalidBase64_ThrowsInvalidUserInputException()
    {
        var exception = Assert.Throws<InvalidUserInputException>(() =>
            "not-valid-base64!!!".DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("aHR0cHM6Ly9leGFtcGxlLmNvbS9pZHMvYWFzLzExNzBfMTE2MF8zMDUyXzY1Njg")]
    [InlineData("aHR0cHM6Ly9hZG1pbi1zaGVsbC5pby9pZHRhL2Fhcy9Db250YWN0SW5mb3JtYXRpb24vMS8w")]
    [InlineData("dXJuOnV1aWQ6MTIzZTQ1NjctZTg5Yi0xMmQzLWE0NTYtNDI2NjE0MTc0MDAw")]
    public void DecodeBase64Url_WhenValidAasIdentifier_ReturnsDecodedString(string encoded)
    {
        var result = encoded.DecodeBase64Url(_logger);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData("amF2YXNjcmlwdDphbGVydCgnuHNzJyk")]
    [InlineData("PHNjcmlwdD5hbGVydCgnuHNzJyk8L3NjcmlwdD4")]
    [InlineData("JyBPUiAnMSc9JzE")]
    public void DecodeBase64Url_WhenDecodedContainsMaliciousPattern_ThrowsInvalidUserInputException(string encoded)
    {
        var exception = Assert.Throws<InvalidUserInputException>(() =>
        encoded.DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public void DecodeBase64Url_WhenDecodedExceedsMaxLength_ThrowsInvalidUserInputException()
    {
        var longString = new string('a', 2049);
        var bytes = System.Text.Encoding.UTF8.GetBytes(longString);
        var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(bytes);

        var exception = Assert.Throws<InvalidUserInputException>(() =>
        encoded.DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public void DecodeBase64Url_WhenDecodedAtMaxLength_Succeeds()
    {
        var longString = new string('a', 2048);
        var bytes = System.Text.Encoding.UTF8.GetBytes(longString);
        var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(bytes);

        var result = encoded.DecodeBase64Url(_logger);

        Assert.Equal(longString, result);
        Assert.Equal(2048, result.Length);
    }

    [Theory]
    [InlineData("aHR0cHM6Ly9leGFtcGxlLmNvbS9wYXRoP3BhcmFtPXZhbHVl")]
    [InlineData("aHR0cHM6Ly9leGFtcGxlLmNvbTo4MDgwL3BhdGg")]
    [InlineData("dXJuOnV1aWQ6ZjQ3YWMxMGItNThjYy00MzcyLWE1NjctMGUwMmIyYzNkNDc5")]
    public void DecodeBase64Url_WhenValidUrlFormats_ReturnsDecodedString(string encoded)
    {
        var result = encoded.DecodeBase64Url(_logger);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData("Li4vLi4vLi4vZXRjL3Bhc3N3ZA")]
    [InlineData("Li5cLi5cLi5cd2luZG93c1xzeXN0ZW0zMg")]
    [InlineData("ZGF0YTp0ZXh0L2h0bWwsPHNjcmlwdD5hbGVydCgnWFNTJyk8L3NjcmlwdD4")]
    public void DecodeBase64Url_WhenDecodedContainsPathTraversalOrDangerousProtocol_ThrowsInvalidUserInputException(string encoded)
    {
        var exception = Assert.Throws<InvalidUserInputException>(() =>
        encoded.DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EncodeBase64Url_WhenNullOrWhitespaceInput_ReturnsEmptyString(string plainText)
    {
        var result = plainText.EncodeBase64Url(_logger);

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("hello", "aGVsbG8")]
    [InlineData("test", "dGVzdA")]
    [InlineData("https://example.com", "aHR0cHM6Ly9leGFtcGxlLmNvbQ")]
    public void EncodeBase64Url_WhenValidInput_ReturnsEncodedString(string plainText, string expected)
    {
        var result = plainText.EncodeBase64Url(_logger);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void EncodeBase64Url_WhenLongInput_ReturnsEncodedString()
    {
        var longString = "https://example.com/" + new string('a', 500);

        var result = longString.EncodeBase64Url(_logger);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void EncodeAndDecode_RoundTrip_PreservesOriginalString()
    {
        const string Original = "https://admin-shell.io/idta/aas/ContactInformation/1/0";

        var encoded = Original.EncodeBase64Url(_logger);
        var decoded = encoded.DecodeBase64Url(_logger);

        Assert.Equal(Original, decoded);
    }

    [Theory]
    [InlineData("https://example.com/ids/aas/1170_1160_3052_6568")]
    [InlineData("urn:uuid:123e4567-e89b-12d3-a456-426614174000")]
    [InlineData("https://mm-software.com/submodel/1170_1160_3052_6568/Nameplate")]
    public void EncodeAndDecode_RoundTrip_WithCommonAasIdentifiers_PreservesOriginalString(string original)
    {
        var encoded = original.EncodeBase64Url(_logger);
        var decoded = encoded.DecodeBase64Url(_logger);

        Assert.Equal(original, decoded);
    }

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("' OR '1'='1")]
    [InlineData("../../../etc/passwd")]
    public void EncodeAndDecode_WithMaliciousContent_ThrowsOnDecode(string maliciousContent)
    {
        var encoded = maliciousContent.EncodeBase64Url(_logger);

        var exception = Assert.Throws<InvalidUserInputException>(() =>
        encoded.DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public void DecodeBase64Url_WhenDecodedContainsNullByte_ThrowsInvalidUserInputException()
    {
        const string StringWithNullByte = "test\0value";
        var encoded = StringWithNullByte.EncodeBase64Url(_logger);

        var exception = Assert.Throws<InvalidUserInputException>(() =>
        encoded.DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public void DecodeBase64Url_WhenDecodedContainsUrlEncodedNullByte_ThrowsInvalidUserInputException()
    {
        const string StringWithEncodedNull = "test%00value";
        var encoded = StringWithEncodedNull.EncodeBase64Url(_logger);

        var exception = Assert.Throws<InvalidUserInputException>(() =>
        encoded.DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("simple-identifier-123")]
    [InlineData("_underscore_identifier")]
    [InlineData("UPPERCASE-IDENTIFIER")]
    [InlineData("2206-1631/1000-859")]
    public void DecodeBase64Url_WhenDecodedIsSimpleValidIdentifier_ReturnsDecodedString(string validIdentifier)
    {
        var encoded = validIdentifier.EncodeBase64Url(_logger);

        var result = encoded.DecodeBase64Url(_logger);

        Assert.Equal(validIdentifier, result);
    }

    [Fact]
    public void DecodeBase64Url_WithoutLogger_StillValidatesSuccessfully()
    {
        const string Original = "https://example.com/test";
        var encoded = Original.EncodeBase64Url();

        var result = encoded.DecodeBase64Url();

        Assert.Equal(Original, result);
    }

    [Fact]
    public void DecodeBase64Url_WithoutLogger_StillThrowsOnInvalidInput()
    {
        const string Malicious = "javascript:alert(1)";
        var encoded = Malicious.EncodeBase64Url();

        var exception = Assert.Throws<InvalidUserInputException>(() =>
        encoded.DecodeBase64Url());

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public void EncodeBase64Url_WithSpecialCharacters_HandlesCorrectly()
    {
        const string SpecialChars = "https://example.com/path?param=value&other=123#fragment";

        var encoded = SpecialChars.EncodeBase64Url(_logger);
        var decoded = encoded.DecodeBase64Url(_logger);

        Assert.Equal(SpecialChars, decoded);
    }

    [Fact]
    public void DecodeBase64Url_WhenDecodedContainsSqlKeywords_ThrowsInvalidUserInputException()
    {
        const string SqlInjection = "1 UNION SELECT * FROM users";
        var encoded = SqlInjection.EncodeBase64Url(_logger);

        var exception = Assert.Throws<InvalidUserInputException>(() =>
        encoded.DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public void DecodeBase64Url_WhenDecodedContainsXSSPayload_ThrowsInvalidUserInputException()
    {
        const string XssPayload = "<img src=x onerror=alert(1)>";
        var encoded = XssPayload.EncodeBase64Url(_logger);

        var exception = Assert.Throws<InvalidUserInputException>(() =>
        encoded.DecodeBase64Url(_logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public void EncodeBase64Url_WhenEncodedLengthExceedsMaxLength_ThrowsInternalDataProcessingException()
    {
        var longString = new string('a', 2000);

        var exception = Assert.Throws<InternalDataProcessingException>(() =>
        longString.EncodeBase64Url(_logger));

        Assert.Equal("Internal Server Error.", exception.Message);
    }

    [Fact]
    public void EncodeBase64Url_WhenEncodedLengthAtMaxLength_ReturnsEncodedString()
    {
        var stringAtLimit = new string('a', 1536);

        var result = stringAtLimit.EncodeBase64Url(_logger);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.Length <= 2048);
    }
}
