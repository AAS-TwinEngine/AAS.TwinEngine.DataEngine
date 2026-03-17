using AAS.TwinEngine.DataEngine.Infrastructure.Logging;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Logging;

public class LogSanitizerExtensionTests
{
    [Fact]
    public void Sanitize_NullInput_ReturnsEmpty()
    {
        var result = LogSanitizerExtension.Sanitize(null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_EmptyString_ReturnsEmpty()
    {
        var result = LogSanitizerExtension.Sanitize(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_CleanString_ReturnsSameString()
    {
        const string input = "normal-header-value";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_NewlineCharacters_AreEscaped()
    {
        const string input = "line1\nline2\rline3\r\nline4";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal("line1\\nline2\\rline3\\r\\nline4", result);
    }

    [Fact]
    public void Sanitize_TabCharacter_IsEscaped()
    {
        const string input = "col1\tcol2";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal("col1\\tcol2", result);
    }

    [Fact]
    public void Sanitize_NullByte_IsEscaped()
    {
        const string input = "before\0after";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal("before\\0after", result);
    }

    [Fact]
    public void Sanitize_AnsiEscapeSequence_IsEscaped()
    {
        const string input = "normal\x1B[31mRED_TEXT\x1B[0m";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal("normal\\x1B[31mRED_TEXT\\x1B[0m", result);
    }

    [Fact]
    public void Sanitize_BackspaceAndFormFeed_AreEscaped()
    {
        const string input = "before\b\fafter";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal("before\\b\\fafter", result);
    }

    [Fact]
    public void Sanitize_OtherControlCharacters_AreHexEscaped()
    {
        const string input = "test\x01\x02\x03";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal("test\\x01\\x02\\x03", result);
    }

    [Fact]
    public void Sanitize_LogInjectionAttempt_NewlineForgedEntry_IsEscaped()
    {
        const string input = "valid\n[2025-01-01 00:00:00] CRITICAL: Forged log entry";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.DoesNotContain("\n", result);
        Assert.Contains("\\n", result);
    }

    [Fact]
    public void Sanitize_LogInjectionAttempt_CarriageReturnForgedEntry_IsEscaped()
    {
        const string input = "valid\r\n[ERROR] Fake error injected by attacker";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.DoesNotContain("\r", result);
        Assert.DoesNotContain("\n", result);
    }

    [Fact]
    public void Sanitize_ExceedsMaxLength_IsTruncated()
    {
        var input = new string('A', 600);

        var result = LogSanitizerExtension.Sanitize(input, 100);

        Assert.Contains("...[truncated]", result);
        Assert.True(result.Length <= 100 + "...[truncated]".Length);
    }

    [Fact]
    public void Sanitize_ExactlyMaxLength_IsNotTruncated()
    {
        var input = new string('A', 100);

        var result = LogSanitizerExtension.Sanitize(input, 100);

        Assert.Equal(input, result);
        Assert.DoesNotContain("...[truncated]", result);
    }

    [Fact]
    public void Sanitize_DefaultMaxLength_Is500()
    {
        var input = new string('X', 501);

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Contains("...[truncated]", result);
    }

    [Fact]
    public void Sanitize_MixedControlAndNormalChars_CorrectlyEscapes()
    {
        const string input = "Authorization: Bearer token123\r\nX-Injected: malicious";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal("Authorization: Bearer token123\\r\\nX-Injected: malicious", result);
    }

    [Fact]
    public void Sanitize_UnicodeCharacters_ArePreserved()
    {
        const string input = "Ünïcödé-Hëadêr-Välüe";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_SpecialPrintableCharacters_ArePreserved()
    {
        const string input = "key=value&foo=bar@example.com#section";

        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData("\n", "\\n")]
    [InlineData("\r", "\\r")]
    [InlineData("\t", "\\t")]
    [InlineData("\0", "\\0")]
    [InlineData("\b", "\\b")]
    [InlineData("\f", "\\f")]
    [InlineData("\x1B", "\\x1B")]
    public void Sanitize_SingleControlCharacter_IsCorrectlyEscaped(string input, string expected)
    {
        var result = LogSanitizerExtension.Sanitize(input);

        Assert.Equal(expected, result);
    }
}
