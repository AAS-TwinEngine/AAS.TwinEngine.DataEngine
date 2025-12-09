using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.TemplateProvider.Services;

public class ShellTemplateMappingProviderTests
{
    private readonly ILogger<ShellTemplateMappingProvider> _logger = Substitute.For<ILogger<ShellTemplateMappingProvider>>();
    private readonly IOptions<TemplateMappingRules> _options = Substitute.For<IOptions<TemplateMappingRules>>();
    private ShellTemplateMappingProvider _sut;

    public ShellTemplateMappingProviderTests()
    {
        var settings = new TemplateMappingRules
        {
            ShellTemplateMappings =
            [
                new ShellTemplateMappings
                {
                    Pattern = ["shell1"],
                    TemplateId = "template1"
                },
                new ShellTemplateMappings
                {
                    Pattern = ["shell2"],
                    TemplateId = "template2"
                }
            ],
            AasIdExtractionRules =
            [
                new AasIdExtractionRules
                {
                    Pattern = ".*",
                    Index = 3,
                    Separator = ":"
                },
                new AasIdExtractionRules
                {
                    Pattern = ".*",
                    Index = 2,
                    Separator = "-"
                }
            ]
        };

        _options.Value.Returns(settings);
        _sut = new ShellTemplateMappingProvider(_logger, _options);
    }

    [Fact]
    public void GetTemplateId_ValidInput_MatchesFirstRule_ReturnsTemplate1()
    {
        var result = _sut.GetTemplateId("one:two:shell1:four");
        Assert.Equal("template1", result);
    }

    [Fact]
    public void GetTemplateId_ValidInput_MatchesSecondRule_ReturnsTemplate2()
    {
        var result = _sut.GetTemplateId("part1-shell2-part3");
        Assert.Equal("template2", result);
    }

    [Fact]
    public void GetTemplateId_ValidInput_MultipleMatches_ReturnsFirstMatchingTemplate()
    {
        var result = _sut.GetTemplateId("x:y:shell2:z");
        Assert.Equal("template2", result);
    }

    [Fact]
    public void GetTemplateId_InputDoesNotContainEnoughParts_SkipsRule()
    {
        const string AasId = "x:y";
        Assert.Throws<ResourceNotFoundException>(() => _sut.GetTemplateId(AasId));
    }

    [Fact]
    public void GetTemplateId_ValidFormatButNoPatternMatch_ThrowsResourceNotFoundException()
    {
        const string AasId = "one:two:nomatch:four";
        Assert.Throws<ResourceNotFoundException>(() => _sut.GetTemplateId(AasId));
    }

    [Fact]
    public void GetTemplateId_EmptyAasIdentifier_ThrowsResourceNotFoundException()
    {
        const string AasId = "";
        Assert.Throws<ResourceNotFoundException>(() => _sut.GetTemplateId(AasId));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ShellTemplateMappingProvider(null!, _options));
        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_NullShellTemplateMappings_ThrowsArgumentException()
    {
        var config = new TemplateMappingRules
        {
            ShellTemplateMappings = null!,
            AasIdExtractionRules = []
        };

        _options.Value.Returns(config);

        var ex = Assert.Throws<ArgumentException>(() =>
            new ShellTemplateMappingProvider(_logger, _options));
        Assert.Contains("ShellTemplateMappings are missing in TemplateMappingRules", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_NullAasIdExtractionRules_ThrowsArgumentException()
    {
        var config = new TemplateMappingRules
        {
            ShellTemplateMappings = [],
            AasIdExtractionRules = null!
        };

        _options.Value.Returns(config);

        var ex = Assert.Throws<ArgumentException>(() =>
            new ShellTemplateMappingProvider(_logger, _options));
        Assert.Contains("AasIdExtractionRules are missing", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetTemplateId_WithExactMatchButCaseInsensitive_ReturnsMatch()
    {
        var result = _sut.GetTemplateId("A:B:ShElL1:D");
        Assert.Equal("template1", result);
    }

    [Fact]
    public void GetTemplateId_PatternIsRegex_WildcardMatch_ReturnsMatch()
    {
        var config = new TemplateMappingRules
        {
            ShellTemplateMappings =
            [
                new ShellTemplateMappings { Pattern = ["shell.*"], TemplateId = "template-wild" }
            ],
            AasIdExtractionRules =
            [
                new AasIdExtractionRules { Pattern = ".*", Index = 2, Separator = "-" }
            ]
        };

        _options.Value.Returns(config);
        _sut = new ShellTemplateMappingProvider(_logger, _options);

        var result = _sut.GetTemplateId("aaa-shellX-ccc");
        Assert.Equal("template-wild", result);
    }

    [Fact]
    public void GetTemplateId_NoMatchingTemplate_LogsError()
    {
        const string AasId = "aaa-bbb-ccc";

        Assert.Throws<ResourceNotFoundException>(() => _sut.GetTemplateId(AasId));

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("No matching template found for shell: aaa-bbb-ccc")),
            null,
            Arg.Any<Func<object, Exception?, string>>()
        );
    }
}
