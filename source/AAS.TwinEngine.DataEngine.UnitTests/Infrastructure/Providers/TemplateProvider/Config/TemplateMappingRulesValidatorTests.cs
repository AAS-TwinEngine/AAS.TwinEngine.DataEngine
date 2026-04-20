using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Options;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.TemplateProvider.Config;

public class TemplateMappingRulesValidatorTests
{
    private readonly TemplateMappingRulesValidator _sut = new();

    private static TemplateManagementConfig CreateConfig(params AasIdExtractionRule[] rules)
    {
        return new TemplateManagementConfig
        {
            TemplateMappingRules = new TemplateMappingRules
            {
                AasIdExtractionRules = rules
            }
        };
    }

    // ── Rule 1: At least one rule is required ──

    [Fact]
    public void Validate_ZeroRules_Fails()
    {
        var config = CreateConfig();

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("At least one AasIdExtractionRule is required", result.FailureMessage);
    }

    // ── Rule 2: Single rule, no ValidationPattern → succeeds ──

    [Fact]
    public void Validate_SingleRule_NoValidationPattern_Succeeds()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Succeeded);
    }

    // ── Rule 3: Multiple rules, all have ValidationPattern → succeeds ──

    [Fact]
    public void Validate_MultipleRules_AllHaveValidationPattern_Succeeds()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+/[^/]+)(?:/|$)",
                Index = 1,
                ValidationPattern = @"^[0-9\-/]+$"
            },
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+)(?:/|$)",
                Index = 1,
                ValidationPattern = @"^[0-9\-]+$"
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Succeeded);
    }

    // ── Rule 3: Multiple rules, one missing ValidationPattern → fails ──

    [Fact]
    public void Validate_MultipleRules_OneMissingValidationPattern_Fails()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+)(?:/|$)",
                Index = 1,
                ValidationPattern = @"^[0-9\-]+$"
            },
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6
                // Missing ValidationPattern
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("ValidationPattern is required when multiple extraction rules are configured", result.FailureMessage);
    }

    // ── Regex with invalid pattern → fails ──

    [Fact]
    public void Validate_Regex_InvalidPattern_Fails()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = "[invalid",
                Index = 1
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("invalid regex Pattern", result.FailureMessage);
    }

    // ── Split with empty separator → fails ──

    [Fact]
    public void Validate_Split_EmptyPattern_Fails()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "",
                Index = 6
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("empty Pattern", result.FailureMessage);
    }

    // ── Index < 1 → fails ──

    [Fact]
    public void Validate_IndexLessThanOne_Fails()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 0
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("Index must be >= 1", result.FailureMessage);
    }

    // ── EndIndex < Index → fails ──

    [Fact]
    public void Validate_Split_EndIndexLessThanIndex_Fails()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 5,
                EndIndex = 3
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("EndIndex (3) must be >= Index (5)", result.FailureMessage);
    }

    // ── Invalid ValidationPattern regex → fails ──

    [Fact]
    public void Validate_InvalidValidationPattern_Fails()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6,
                ValidationPattern = "[broken"
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("invalid ValidationPattern", result.FailureMessage);
    }

    // ── Null options → throws ──

    [Fact]
    public void Validate_NullOptions_ThrowsInvalidDependencyException() => Assert.Throws<InvalidDependencyException>(() => _sut.Validate(null, null!));

    // ── Uses Description in error message when available ──

    [Fact]
    public void Validate_UsesDescriptionInErrorMessage()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 0,
                Description = "My broken rule"
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
        Assert.Contains("My broken rule", result.FailureMessage);
    }

    // ── Valid Regex strategy rule → succeeds ──

    [Fact]
    public void Validate_ValidRegexRule_Succeeds()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+)(?:/|$)",
                Index = 1,
                Description = "Single-segment"
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Succeeded);
    }

    // ── Valid Split with EndIndex → succeeds ──

    [Fact]
    public void Validate_ValidSplitWithEndIndex_Succeeds()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 5,
                EndIndex = 6
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Succeeded);
    }
}
