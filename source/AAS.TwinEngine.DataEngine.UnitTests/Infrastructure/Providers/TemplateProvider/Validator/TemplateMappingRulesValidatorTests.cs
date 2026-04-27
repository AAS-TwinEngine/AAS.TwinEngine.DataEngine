using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config.Helpers;

using Microsoft.Extensions.Logging.Abstractions;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.TemplateProvider.Validator;

public class TemplateMappingRulesValidatorTests
{
    private readonly TemplateMappingRulesValidator _sut = new(new NullLogger<TemplateMappingRulesValidator>());

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

    [Fact]
    public void Validate_ZeroRules_Fails()
    {
        var config = CreateConfig();

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
    }

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

    [Fact]
    public void Validate_MultipleRules_OneMissingValidationPattern_Fails()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+)(?:/|$)",
                Index = 1,
                // Missing ValidationPattern
            },
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
    }

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
    }

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
    }

    [Fact]


    public void Validate_IndexLessThanOne_Fails()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = -1
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
    }

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
    }

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
    }

    [Fact]
    public void Validate_UsesRuleIndexInErrorMessage()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = -1
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_ValidRegexRule_Succeeds()
    {
        var config = CreateConfig(
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+)(?:/|$)",
                Index = 1
            });

        var result = _sut.Validate(null, config);

        Assert.True(result.Succeeded);
    }

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
