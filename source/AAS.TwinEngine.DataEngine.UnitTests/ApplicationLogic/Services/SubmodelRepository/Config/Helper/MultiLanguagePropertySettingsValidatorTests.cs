using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config.Helper;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.Config.Helper;

public class MultiLanguagePropertySettingsValidatorTests
{
    private readonly MultiLanguagePropertySettingsValidator _validator = new();

    [Theory]
    [InlineData("en")]
    [InlineData("en-US")]
    [InlineData("de")]
    [InlineData("fr-FR")]
    [InlineData("zh-Hans")]
    [InlineData("pt-BR")]
    [InlineData("en-GB")]
    [InlineData("zh-Hant-TW")]
    [InlineData("sr-Cyrl-RS")]
    public void Validate_ValidBcp47Tags_ReturnsSuccess(string languageTag)
    {
        var settings = new MultiLanguagePropertySettings
        {
            DefaultLanguages = [languageTag]
        };

        var result = _validator.Validate(null, settings);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("en-")]
    [InlineData("123")]
    [InlineData("en_US")] // Should be en-US
    [InlineData("EN")] // Uppercase not allowed by pattern
    [InlineData("e")] // Too short
    [InlineData("toolong")] // Too long (>4 chars)
    [InlineData("-en")]
    [InlineData("en-us")] // Region code must be uppercase
    public void Validate_InvalidBcp47Tags_ReturnsFail(string languageTag)
    {
        var settings = new MultiLanguagePropertySettings
        {
            DefaultLanguages = [languageTag]
        };

        var result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains(languageTag, result.FailureMessage, StringComparison.CurrentCulture);
    }

    [Fact]
    public void Validate_NullDefaultLanguages_ReturnsSuccess()
    {
        var settings = new MultiLanguagePropertySettings
        {
            DefaultLanguages = null
        };

        var result = _validator.Validate(null, settings);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyDefaultLanguages_ReturnsSuccess()
    {
        var settings = new MultiLanguagePropertySettings
        {
            DefaultLanguages = []
        };

        var result = _validator.Validate(null, settings);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_MixedValidAndInvalid_ReturnsFail()
    {
        var settings = new MultiLanguagePropertySettings
        {
            DefaultLanguages = ["fr", "invalid", "hi"]
        };

        var result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("invalid", result.FailureMessage, StringComparison.CurrentCulture);
        Assert.DoesNotContain("fr", result.FailureMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("hi", result.FailureMessage, StringComparison.CurrentCulture);
    }

    [Fact]
    public void Validate_EmptyStringInList_ReturnsFail()
    {
        var settings = new MultiLanguagePropertySettings
        {
            DefaultLanguages = ["en", "", "de"]
        };

        var result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("(empty)", result.FailureMessage, StringComparison.CurrentCulture);
    }

    [Fact]
    public void Validate_WhitespaceInList_ReturnsFail()
    {
        var settings = new MultiLanguagePropertySettings
        {
            DefaultLanguages = ["en", "   ", "de"]
        };

        var result = _validator.Validate(null, settings);

        Assert.True(result.Failed);
        Assert.Contains("(empty)", result.FailureMessage, StringComparison.CurrentCulture);
    }

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _validator.Validate(null, null!));
    }
}
