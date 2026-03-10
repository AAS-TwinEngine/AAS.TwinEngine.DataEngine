using AAS.TwinEngine.Plugin.TestPlugin.Common;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.ApplicationLogic.Extensions;

public class LogSanitizerTests
{
    [Fact]
    public void Sanitize_NullInput_ReturnsEmpty()
    {
        var result = LogSanitizer.Sanitize(null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_EmptyString_ReturnsEmpty()
    {
        var result = LogSanitizer.Sanitize(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_CleanString_ReturnsSameString()
    {
        const string input = "normal-header-value";

        var result = LogSanitizer.Sanitize(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Sanitize_NewlineCharacters_AreEscaped()
    {
        const string input = "line1\nline2\rline3\r\nline4";

        var result = LogSanitizer.Sanitize(input);

        Assert.Equal("line1\\nline2\\rline3\\r\\nline4", result);
    }

    [Fact]
    public void Sanitize_NullByte_IsEscaped()
    {
        const string input = "before\0after";

        var result = LogSanitizer.Sanitize(input);

        Assert.Equal("before\\0after", result);
    }

    [Fact]
    public void Sanitize_AnsiEscapeSequence_IsEscaped()
    {
        const string input = "normal\x1B[31mRED\x1B[0m";

        var result = LogSanitizer.Sanitize(input);

        Assert.Equal("normal\\x1B[31mRED\\x1B[0m", result);
    }

    [Fact]
    public void Sanitize_LogInjectionAttempt_IsEscaped()
    {
        const string input = "valid\n[2025-01-01] CRITICAL: Forged entry";

        var result = LogSanitizer.Sanitize(input);

        Assert.DoesNotContain("\n", result);
        Assert.Contains("\\n", result);
    }

    [Fact]
    public void Sanitize_ExceedsMaxLength_IsTruncated()
    {
        var input = new string('A', 600);

        var result = LogSanitizer.Sanitize(input, 100);

        Assert.Contains("...[truncated]", result);
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
        var result = LogSanitizer.Sanitize(input);

        Assert.Equal(expected, result);
    }
}
