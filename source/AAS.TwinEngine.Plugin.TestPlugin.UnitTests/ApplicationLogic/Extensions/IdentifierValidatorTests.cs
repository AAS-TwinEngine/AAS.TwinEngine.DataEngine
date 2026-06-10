using Microsoft.Extensions.Logging;

using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Extensions;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.ApplicationLogic.Extensions;

public class IdentifierValidatorTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("JAVASCRIPT:alert('xss')")]
    [InlineData("data:text/html,<script>alert('XSS')</script>")]
    [InlineData("vbscript:msgbox('xss')")]
    [InlineData("file:///etc/passwd")]
    [InlineData("about:blank")]
    public void IsValidIdentifier_WhenContainsDangerousProtocol_ReturnsFalse(string identifier)
    {
        var result = identifier.IsValidIdentifier(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<SCRIPT>alert('xss')</SCRIPT>")]
    [InlineData("</script>")]
    [InlineData("<img onerror=alert('xss')>")]
    [InlineData("<body onload=alert('xss')>")]
    [InlineData("<div onclick=alert('xss')>")]
    [InlineData("eval(alert('xss'))")]
    [InlineData("expression(alert('xss'))")]
    public void IsValidIdentifier_WhenContainsDangerousPattern_ReturnsFalse(string identifier)
    {
        var result = identifier.IsValidIdentifier(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("<svg/onload=alert('xss')>")]
    [InlineData("<iframe onload=alert('xss')>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    public void IsValidIdentifier_WhenContainsXSSPattern_ReturnsFalse(string identifier)
    {
        var result = identifier.IsValidIdentifier(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("1' OR '1'='1")]
    [InlineData("admin'--")]
    [InlineData("'; DROP TABLE users--")]
    [InlineData("1 UNION SELECT * FROM users")]
    [InlineData("1; DELETE FROM users")]
    [InlineData("exec xp_cmdshell")]
    [InlineData("INSERT INTO users")]
    [InlineData("UPDATE users SET")]
    public void IsValidIdentifier_WhenContainsSqlInjectionPattern_ReturnsFalse(string identifier)
    {
        var result = identifier.IsValidIdentifier(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("%2e%2e/config")]
    [InlineData("..%2fconfig")]
    public void IsValidIdentifier_WhenContainsPathTraversalPattern_ReturnsFalse(string identifier)
    {
        var result = identifier.IsValidIdentifier(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("https://example.com/ids/aas/1170_1160_3052_6568")]
    [InlineData("https://admin-shell.io/idta/aas/ContactInformation/1/0")]
    [InlineData("urn:uuid:123e4567-e89b-12d3-a456-426614174000")]
    [InlineData("http://localhost:8081/shells/aHR0cHM6Ly9hZG1pbi1zaGVsbC5pby9pZHRhL2Fhcy")]
    [InlineData("https://mm-software.com/submodel/1170_1160_3052_6568/Nameplate")]
    [InlineData("simple-identifier-123")]
    [InlineData("_underscore_identifier")]
    [InlineData("UPPERCASE-IDENTIFIER")]
    public void IsValidIdentifier_WhenValidIdentifier_ReturnsTrue(string identifier)
    {
        var result = identifier.IsValidIdentifier(_logger);

        Assert.True(result);
    }

    [Fact]
    public void IsValidIdentifier_WhenNullIdentifier_ReturnsFalse()
    {
        string? identifier = null;
        var result = identifier!.IsValidIdentifier(_logger);

        Assert.False(result);
    }

    [Fact]
    public void IsValidIdentifier_WhenEmptyIdentifier_ReturnsFalse()
    {
        var result = string.Empty.IsValidIdentifier(_logger);

        Assert.False(result);
    }

    [Fact]
    public void IsValidIdentifier_WhenWhitespaceIdentifier_ReturnsFalse()
    {
        var result = "   ".IsValidIdentifier(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("https://example.com/path?param=value")]
    [InlineData("https://example.com/path#fragment")]
    [InlineData("https://user:pass@example.com/path")]
    [InlineData("https://example.com:8080/path")]
    public void IsValidIdentifier_WhenValidHttpsUrl_ReturnsTrue(string identifier)
    {
        var result = identifier.IsValidIdentifier(_logger);

        Assert.True(result);
    }

    [Theory]
    [InlineData("test\0value")]
    [InlineData("test%00value")]
    public void IsValidIdentifier_WhenContainsNullByte_ReturnsFalse(string identifier)
    {
        var result = identifier.IsValidIdentifier(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("https://example.com/ids/shell-123/submodel-456")]
    [InlineData("urn:uuid:f47ac10b-58cc-4372-a567-0e02b2c3d479")]
    [InlineData("shell_descriptor_001")]
    [InlineData("2206-1631/1000-859")]
    public void IsValidIdentifier_WhenCommonAasIdentifierPatterns_ReturnsTrue(string identifier)
    {
        var result = identifier.IsValidIdentifier(_logger);

        Assert.True(result);
    }
}
