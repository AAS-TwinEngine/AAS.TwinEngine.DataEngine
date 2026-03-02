using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Extensions;

public class IdentifierValidatorTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateIdentifier_WhenNullOrWhiteSpace_ThrowsInvalidUserInputException(string? identifier)
    {
        var exception = Assert.Throws<InvalidUserInputException>(() =>
        identifier!.ValidateIdentifier("testParameter", _logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

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

    [Theory]
    [InlineData("https://example.com/test")]
    [InlineData("urn:uuid:test-123")]
    [InlineData("validId123")]
    public void ValidateIdentifier_WhenValidIdentifier_DoesNotThrow(string identifier)
    {
        var exception = Record.Exception(() =>
        identifier.ValidateIdentifier("testParameter", _logger));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("' OR '1'='1")]
    public void ValidateIdentifier_WhenInvalidIdentifier_ThrowsInvalidUserInputException(string identifier)
    {
        var exception = Assert.Throws<InvalidUserInputException>(() =>
        identifier.ValidateIdentifier("testParameter", _logger));

        Assert.Equal("Invalid User Input.", exception.Message);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateIdShortPath_WhenNullOrWhiteSpace_ThrowsInvalidUserInputException(string? idShortPath)
    {
        var exception = Assert.Throws<InvalidUserInputException>(() =>
        idShortPath!.ValidateIdShortPath("testParameter", _logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Theory]
    [InlineData("ContactInformation1")]
    [InlineData("ManufacturerName")]
    [InlineData("element.subelement")]
    [InlineData("element.subelement.property")]
    [InlineData("list[0]")]
    [InlineData("list%5B0%5D")] // URL-encoded [0]
    [InlineData("element[3].property")]
    [InlineData("collection.item_name")]
    [InlineData("element-with-hyphen")]
    [InlineData("element_with_underscore")]
    [InlineData("Element123")]
    [InlineData("a.b.c.d.e")]
    public void IsValidIdShortPath_WhenValidIdShortPath_ReturnsTrue(string idShortPath)
    {
        var result = idShortPath.IsValidIdShortPath(_logger);

        Assert.True(result);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32")]
    [InlineData("%2e%2e/config")]
    [InlineData("..%2fconfig")]
    [InlineData("element/../otherElement")]
    public void IsValidIdShortPath_WhenContainsPathTraversal_ReturnsFalse(string idShortPath)
    {
        var result = idShortPath.IsValidIdShortPath(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("<img onerror=alert('xss')>")]
    [InlineData("element<script>alert(1)</script>")]
    [InlineData("<svg/onload=alert('xss')>")]
    public void IsValidIdShortPath_WhenContainsXssPattern_ReturnsFalse(string idShortPath)
    {
        var result = idShortPath.IsValidIdShortPath(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("element'; DROP TABLE--")]
    [InlineData("1 UNION SELECT * FROM users")]
    [InlineData("element; DELETE FROM table")]
    public void IsValidIdShortPath_WhenContainsSqlInjection_ReturnsFalse(string idShortPath)
    {
        var result = idShortPath.IsValidIdShortPath(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("data:text/html,<script>")]
    [InlineData("vbscript:msgbox('xss')")]
    [InlineData("file:///etc/passwd")]
    public void IsValidIdShortPath_WhenContainsDangerousProtocol_ReturnsFalse(string idShortPath)
    {
        var result = idShortPath.IsValidIdShortPath(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("element\0value")]
    [InlineData("element%00value")]
    public void IsValidIdShortPath_WhenContainsNullByte_ReturnsFalse(string idShortPath)
    {
        var result = idShortPath.IsValidIdShortPath(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("element with spaces")]
    [InlineData("element/slash")]
    [InlineData("element\\backslash")]
    [InlineData("element|pipe")]
    [InlineData("element;semicolon")]
    [InlineData("element&ampersand")]
    [InlineData("element$dollar")]
    [InlineData("element@at")]
    [InlineData("element#hash")]
    [InlineData("element!exclamation")]
    [InlineData("element*asterisk")]
    [InlineData("element(paren)")]
    [InlineData("element{brace}")]
    [InlineData("element<angle>")]
    [InlineData("element\"quote\"")]
    [InlineData("element'apostrophe'")]
    public void IsValidIdShortPath_WhenContainsInvalidCharacters_ReturnsFalse(string idShortPath)
    {
        var result = idShortPath.IsValidIdShortPath(_logger);

        Assert.False(result);
    }

    [Theory]
    [InlineData("ContactInformation1")]
    [InlineData("element.property")]
    [InlineData("valid_idShort")]
    public void ValidateIdShortPath_WhenValidIdShortPath_DoesNotThrow(string idShortPath)
    {
        var exception = Record.Exception(() =>
                                             idShortPath.ValidateIdShortPath("testParameter", _logger));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("../../../etc/passwd")]
    [InlineData("' OR '1'='1")]
    [InlineData("element with spaces")]
    public void ValidateIdShortPath_WhenInvalidIdShortPath_ThrowsInvalidUserInputException(string idShortPath)
    {
        var exception = Assert.Throws<InvalidUserInputException>(() =>
                                                                     idShortPath.ValidateIdShortPath("testParameter", _logger));

        Assert.Equal("Invalid User Input.", exception.Message);
    }

    [Fact]
    public void IsValidIdShortPath_WhenNullIdShortPath_ReturnsFalse()
    {
        string? idShortPath = null;
        var result = idShortPath!.IsValidIdShortPath(_logger);

        Assert.False(result);
    }

    [Fact]
    public void IsValidIdShortPath_WhenEmptyIdShortPath_ReturnsFalse()
    {
        var result = string.Empty.IsValidIdShortPath(_logger);

        Assert.False(result);
    }

    [Fact]
    public void IsValidIdShortPath_WhenWhitespaceIdShortPath_ReturnsFalse()
    {
        var result = "   ".IsValidIdShortPath(_logger);

        Assert.False(result);
    }
}
