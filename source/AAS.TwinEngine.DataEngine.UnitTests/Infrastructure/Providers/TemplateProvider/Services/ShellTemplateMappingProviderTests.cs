using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.TemplateProvider.Services;

public class ShellTemplateMappingProviderTests
{
    private readonly ILogger<ShellTemplateMappingProvider> _logger = Substitute.For<ILogger<ShellTemplateMappingProvider>>();

    private ShellTemplateMappingProvider CreateSut(
        IList<AasIdExtractionRule> rules,
        IList<ShellTemplateMappings>? shellMappings = null)
    {
        var options = Substitute.For<IOptions<TemplateManagementConfig>>();
        var config = new TemplateManagementConfig
        {
            TemplateMappingRules = new TemplateMappingRules
            {
                ShellTemplateMappings = shellMappings ??
                [
                    new ShellTemplateMappings { Pattern = [".*"], TemplateId = "default-template" }
                ],
                AasIdExtractionRules = rules
            }
        };
        options.Value.Returns(config);
        return new ShellTemplateMappingProvider(_logger, options);
    }

    // ── Regex Strategy: Single-segment extraction ──

    [Fact]
    public void GetProductIdFromRule_Regex_SingleSegment_ExtractsCorrectly()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+)(?:/|$)",
                Index = 1
            }
        ]);

        var result = sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/ContactInformation");
        Assert.Equal("2000-2201", result);
    }

    // ── Regex Strategy: Multi-segment extraction (two parts) ──

    [Fact]
    public void GetProductIdFromRule_Regex_MultiSegment_TwoParts_ExtractsCorrectly()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+/[^/]+)(?:/|$)",
                Index = 1,
                ValidationPattern = @"^[0-9\-/]+$"
            }
        ]);

        var result = sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/353-000/ContactInformation");
        Assert.Equal("2000-2201/353-000", result);
    }

    // ── Regex Strategy: Multi-segment extraction (three parts) ──

    [Fact]
    public void GetProductIdFromRule_Regex_MultiSegment_ThreeParts_ExtractsCorrectly()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+/[^/]+/[^/]+)(?:/|$)",
                Index = 1,
                ValidationPattern = @"^[0-9\-/v]+$"
            }
        ]);

        var result = sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/353-000/v2/ContactInformation");
        Assert.Equal("2000-2201/353-000/v2", result);
    }

    // ── Regex Strategy: Validation pattern rejects — falls through ──

    [Fact]
    public void GetProductIdFromRule_Regex_ValidationFails_FallsToNextRule()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+/[^/]+)(?:/|$)",
                Index = 1,
                ValidationPattern = @"^[0-9\-/]+$",
                Description = "Multi-segment (digits only)"
            },
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+)(?:/|$)",
                Index = 1,
                ValidationPattern = @"^[a-zA-Z0-9\-]+$",
                Description = "Single-segment"
            }
        ]);

        // "2000-2201/ContactInformation" matches first regex, extracts "2000-2201/ContactInformation"
        // but validation "^[0-9\-/]+$" fails (letters in "ContactInformation")
        // Falls to second regex which extracts "2000-2201" and validates OK
        var result = sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/ContactInformation");
        Assert.Equal("2000-2201", result);
    }

    // ── Regex Strategy: No match at all ──

    [Fact]
    public void GetProductIdFromRule_Regex_NoMatch_ThrowsResourceNotFoundException()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+)(?:/|$)",
                Index = 1
            }
        ]);

        Assert.Throws<ResourceNotFoundException>(() => sut.GetProductIdFromRule("random-garbage"));
    }

    // ── Regex Strategy: Invalid capture group index ──

    [Fact]
    public void GetProductIdFromRule_Regex_InvalidGroupIndex_SkipsRule()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^https?://[^/]+/ids/submodel/([^/]+)(?:/|$)",
                Index = 5 // only 1 capture group exists
            }
        ]);

        Assert.Throws<ResourceNotFoundException>(() =>
            sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/CI"));
    }

    // ── Split Strategy: Single segment (no EndIndex = old IndexSplit) ──

    [Fact]
    public void GetProductIdFromRule_Split_SingleSegment_ExtractsCorrectly()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6
            }
        ]);

        // Split by "/": ["https:", "", "wago.com", "ids", "submodel", "2000-2201", "ContactInformation"]
        //  indices:        1       2       3         4        5          6               7
        var result = sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/ContactInformation");
        Assert.Equal("2000-2201", result);
    }

    // ── Split Strategy: Range (with EndIndex = old RangeSplit) ──

    [Fact]
    public void GetProductIdFromRule_Split_Range_ExtractsMultipleSegments()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6,
                EndIndex = 7
            }
        ]);

        // Split: ["https:", "", "wago.com", "ids", "submodel", "2000-2201", "353-000", "ContactInformation"]
        //          1       2       3         4        5          6            7              8
        var result = sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/353-000/ContactInformation");
        Assert.Equal("2000-2201/353-000", result);
    }

    // ── Split Strategy: Out of bounds ──

    [Fact]
    public void GetProductIdFromRule_Split_IndexOutOfBounds_SkipsRule()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 99
            }
        ]);

        Assert.Throws<ResourceNotFoundException>(() =>
            sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/CI"));
    }

    // ── Split Strategy: EndIndex out of bounds ──

    [Fact]
    public void GetProductIdFromRule_Split_EndIndexOutOfBounds_SkipsRule()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6,
                EndIndex = 99
            }
        ]);

        Assert.Throws<ResourceNotFoundException>(() =>
            sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/CI"));
    }

    // ── Split Strategy: Validation pattern rejects ──

    [Fact]
    public void GetProductIdFromRule_Split_ValidationFails_SkipsRule()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6,
                ValidationPattern = @"^[0-9]+$" // digits only
            }
        ]);

        // Segment 6 = "2000-2201" which contains hyphens → fails validation
        Assert.Throws<ResourceNotFoundException>(() =>
            sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/CI"));
    }

    // ── First rule matches — stops (does not try second) ──

    [Fact]
    public void GetProductIdFromRule_FirstRuleMatches_StopsImmediately()
    {
        var sut = CreateSut(
        [
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
            }
        ]);

        var result = sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/353-000/ContactInformation");
        Assert.Equal("2000-2201/353-000", result);
    }

    // ── Mixed strategies: Regex then Split fallback ──

    [Fact]
    public void GetProductIdFromRule_RegexFails_FallsToSplit()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^urn:([^:]+)$",
                Index = 1,
                ValidationPattern = @"^[a-z]+$"
            },
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6,
                ValidationPattern = @"^[0-9\-]+$"
            }
        ]);

        var result = sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/ContactInformation");
        Assert.Equal("2000-2201", result);
    }

    // ── Single rule without ValidationPattern works ──

    [Fact]
    public void GetProductIdFromRule_SingleRule_NoValidationPattern_Works()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6
                // No ValidationPattern — allowed for single rule
            }
        ]);

        var result = sut.GetProductIdFromRule("https://wago.com/ids/submodel/2000-2201/ContactInformation");
        Assert.Equal("2000-2201", result);
    }

    // ── Split with colon separator ──

    [Fact]
    public void GetProductIdFromRule_Split_ColonSeparator_ExtractsCorrectly()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = ":",
                Index = 3
            }
        ]);

        var result = sut.GetProductIdFromRule("one:two:shell1:four");
        Assert.Equal("shell1", result);
    }

    // ── GetTemplateId: end-to-end with Regex extraction + template matching ──

    [Fact]
    public void GetTemplateId_RegexExtraction_MatchesTemplate()
    {
        var sut = CreateSut(
            rules:
            [
                new AasIdExtractionRule
                {
                    Strategy = ExtractionStrategy.Regex,
                    Pattern = @"^https?://[^/]+/ids/submodel/([^/]+)(?:/|$)",
                    Index = 1
                }
            ],
            shellMappings:
            [
                new ShellTemplateMappings { Pattern = ["2000.*"], TemplateId = "wago-template" }
            ]);

        var result = sut.GetTemplateId("https://wago.com/ids/submodel/2000-2201/ContactInformation");
        Assert.Equal("wago-template", result);
    }

    // ── GetTemplateId: case-insensitive template matching ──

    [Fact]
    public void GetTemplateId_CaseInsensitive_MatchesTemplate()
    {
        var sut = CreateSut(
            rules:
            [
                new AasIdExtractionRule
                {
                    Strategy = ExtractionStrategy.Split,
                    Pattern = ":",
                    Index = 3
                }
            ],
            shellMappings:
            [
                new ShellTemplateMappings { Pattern = ["SHELL1"], TemplateId = "template1" }
            ]);

        var result = sut.GetTemplateId("A:B:shell1:D");
        Assert.Equal("template1", result);
    }

    // ── GetTemplateId: no matching template ──

    [Fact]
    public void GetTemplateId_NoMatchingTemplate_ThrowsResourceNotFoundException()
    {
        var sut = CreateSut(
            rules:
            [
                new AasIdExtractionRule
                {
                    Strategy = ExtractionStrategy.Split,
                    Pattern = ":",
                    Index = 3
                }
            ],
            shellMappings:
            [
                new ShellTemplateMappings { Pattern = ["nomatch"], TemplateId = "template1" }
            ]);

        Assert.Throws<ResourceNotFoundException>(() => sut.GetTemplateId("A:B:shell1:D"));
    }

    // ── Constructor: null logger ──

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var options = Substitute.For<IOptions<TemplateManagementConfig>>();
        options.Value.Returns(new TemplateManagementConfig());

        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ShellTemplateMappingProvider(null!, options));
        Assert.Equal("logger", ex.ParamName);
    }

    // ── Constructor: null ShellTemplateMappings ──

    [Fact]
    public void Constructor_NullShellTemplateMappings_ThrowsArgumentException()
    {
        var options = Substitute.For<IOptions<TemplateManagementConfig>>();
        options.Value.Returns(new TemplateManagementConfig
        {
            TemplateMappingRules = new TemplateMappingRules
            {
                ShellTemplateMappings = null!,
                AasIdExtractionRules = []
            }
        });

        var ex = Assert.Throws<ArgumentException>(() =>
            new ShellTemplateMappingProvider(_logger, options));
        Assert.Contains("ShellTemplateMappings are missing", ex.Message, StringComparison.Ordinal);
    }

    // ── Constructor: null AasIdExtractionRules ──

    [Fact]
    public void Constructor_NullAasIdExtractionRules_ThrowsArgumentException()
    {
        var options = Substitute.For<IOptions<TemplateManagementConfig>>();
        options.Value.Returns(new TemplateManagementConfig
        {
            TemplateMappingRules = new TemplateMappingRules
            {
                ShellTemplateMappings = [],
                AasIdExtractionRules = null!
            }
        });

        var ex = Assert.Throws<ArgumentException>(() =>
            new ShellTemplateMappingProvider(_logger, options));
        Assert.Contains("AasIdExtractionRules are missing", ex.Message, StringComparison.Ordinal);
    }

    // ── Empty identifier ──

    [Fact]
    public void GetProductIdFromRule_EmptyIdentifier_ThrowsResourceNotFoundException()
    {
        var sut = CreateSut(
        [
            new AasIdExtractionRule
            {
                Strategy = ExtractionStrategy.Split,
                Pattern = "/",
                Index = 6
            }
        ]);

        Assert.Throws<ResourceNotFoundException>(() => sut.GetProductIdFromRule(""));
    }
}
