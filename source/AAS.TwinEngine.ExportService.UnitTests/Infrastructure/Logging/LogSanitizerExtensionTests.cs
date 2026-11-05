using AAS.TwinEngine.ExportService.Infrastructure.Logging;

namespace AAS.TwinEngine.ExportService.UnitTests.Infrastructure.Logging;

public class LogSanitizerExtensionTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void Sanitize_WhenNullOrEmpty_ReturnsEmptyString(string? input, string expected)
    {
        var result = LogSanitizerExtension.Sanitize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sanitize_WhenNormalString_ReturnsIdenticalString()
    {
        const string input = "Normal logging message with no control characters 12345.";
        var result = LogSanitizerExtension.Sanitize(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_WhenKnownControlCharacters_EscapesThemCorrectly()
    {
        // Testing \r, \n, \t, \0, \x1B (ESC), \b, \f
        const string input = "Line1\r\nLine2\tTab\0Null\u001BEsc\bBack\fForm";
        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal("Line1\\r\\nLine2\\tTab\\0Null\\x1BEsc\\bBack\\fForm", result);
        Assert.DoesNotContain('\r', result);
        Assert.DoesNotContain('\n', result);
        Assert.DoesNotContain('\t', result);
        Assert.DoesNotContain('\0', result);
        Assert.DoesNotContain('\u001B', result);
        Assert.DoesNotContain('\b', result);
        Assert.DoesNotContain('\f', result);
    }

    [Fact]
    public void Sanitize_WhenOtherControlCharacter_EscapesAsHex()
    {
        // \u0007 is Bell (BEL), \u0001 is Start of Heading (SOH)
        const string input = "Test\u0007Bell\u0001SOH";
        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal("Test\\x07Bell\\x01SOH", result);
    }

    [Fact]
    public void Sanitize_WhenInputExceedsMaxLength_TruncatesWithSuffix()
    {
        var input = new string('a', 600);
        var result = LogSanitizerExtension.Sanitize(input, maxLength: 50);

        Assert.StartsWith("aaaaaaaaaa", result);
        Assert.EndsWith("...[truncated]", result);
        Assert.True(result.Length <= 50 + "...[truncated]".Length);
    }
}
