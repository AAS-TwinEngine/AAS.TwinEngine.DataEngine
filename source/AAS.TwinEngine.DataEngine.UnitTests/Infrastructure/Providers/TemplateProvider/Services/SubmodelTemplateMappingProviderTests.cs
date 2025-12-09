using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.TemplateProvider.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.TemplateProvider.Services;

public class SubmodelTemplateMappingProviderTests
{
    private readonly ILogger<SubmodelTemplateMappingProvider> _logger = Substitute.For<ILogger<SubmodelTemplateMappingProvider>>();
    private readonly IOptions<TemplateMappingRules> _options = Substitute.For<IOptions<TemplateMappingRules>>();
    private readonly SubmodelTemplateMappingProvider _sut;

    public SubmodelTemplateMappingProviderTests()
    {
        var settings = new TemplateMappingRules
        {
            SubmodelTemplateMappings =
            [
                new SubmodelTemplateMappings
                {
                    Pattern = ["submodel1", "submodel2"],
                    TemplateId = "template1"
                },
                new SubmodelTemplateMappings
                {
                    Pattern = ["submodel3"],
                    TemplateId = "template2"
                }
            ]
        };
        _options.Value.Returns(settings);
        _sut = new SubmodelTemplateMappingProvider(_logger, _options);
    }

    [Fact]
    public void GetTemplateId_ReturnsTemplateId_WhenPatternMatches()
    {
        var result = _sut.GetTemplateId("submodel1");

        Assert.Equal("template1", result);
    }

    [Fact]
    public void GetTemplateId_ThrowsResourceNotFoundException_WhenPatternDoesNotMatch()
    {
        var exception = Assert.Throws<ResourceNotFoundException>(() => _sut.GetTemplateId("submodel4"));
        Assert.IsType<ResourceNotFoundException>(exception);
    }

    [Fact]
    public void GetTemplateId_ThrowsArgumentException_WhenTemplateMappingsIsMissing()
    {
        _options.Value.Returns(new TemplateMappingRules { SubmodelTemplateMappings = null! });

        var exception = Assert.Throws<ArgumentException>(() => new SubmodelTemplateMappingProvider(_logger, _options));
        Assert.IsType<ArgumentException>(exception);
        Assert.Equal("SubmodelTemplateMappings is missing in TemplateMappingSettings", exception.Message);
    }
}
