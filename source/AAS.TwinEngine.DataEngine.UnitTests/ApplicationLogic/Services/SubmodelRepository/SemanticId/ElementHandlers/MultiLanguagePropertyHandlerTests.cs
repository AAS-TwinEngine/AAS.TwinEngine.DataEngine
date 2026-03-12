using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class MultiLanguagePropertyHandlerTests
{
    private readonly MultiLanguagePropertyHandler _sut;
    private readonly ISemanticIdResolver _resolver;
    private readonly ISubmodelElementHelper _elementHelper;
    private readonly ILogger<MultiLanguagePropertyHandler> _logger;

    public MultiLanguagePropertyHandlerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _elementHelper = Substitute.For<ISubmodelElementHelper>();
        _logger = Substitute.For<ILogger<MultiLanguagePropertyHandler>>();
        _sut = new MultiLanguagePropertyHandler(_resolver, _elementHelper, _logger);
    }

    [Fact]
    public void CanHandle_MultiLanguageProperty_ReturnsTrue()
    {
        var mlp = new MultiLanguageProperty(idShort: "Test");

        True(_sut.CanHandle(mlp));
    }

    [Fact]
    public void CanHandle_NonMlp_ReturnsFalse()
    {
        var property = new Property(idShort: "Test", valueType: DataTypeDefXsd.String);

        False(_sut.CanHandle(property));
    }

    [Fact]
    public void Extract_WithLanguages_ReturnsBranchWithLanguageLeaves()
    {
        var mlp = new MultiLanguageProperty(
            idShort: "ManufacturerName",
            value: [new LangStringTextType("en", ""), new LangStringTextType("de", "")]
        );
        _resolver.ExtractSemanticId(mlp).Returns("http://test/manufacturer-name");
        _resolver.GetCardinality(mlp).Returns(Cardinality.One);
        _resolver.MlpPostFixSeparator.Returns("_");
        _elementHelper.ResolveLanguages(mlp).Returns(["en", "de"]);

        var result = _sut.Extract(mlp, _ => null);

        var branch = IsType<SemanticBranchNode>(result);
        Equal("http://test/manufacturer-name", branch.SemanticId);
        Equal(2, branch.Children.Count);
        var semanticIds = branch.Children.Select(c => c.SemanticId).OrderBy(s => s).ToList();
        Contains("http://test/manufacturer-name_de", semanticIds);
        Contains("http://test/manufacturer-name_en", semanticIds);
    }

    [Fact]
    public void Extract_WithNoLanguages_ReturnsEmptyBranchAndLogsInfo()
    {
        var mlp = new MultiLanguageProperty(idShort: "EmptyMlp", value: null);
        _resolver.ExtractSemanticId(mlp).Returns("http://test/empty");
        _resolver.GetCardinality(mlp).Returns(Cardinality.Unknown);
        _resolver.MlpPostFixSeparator.Returns("_");
        _elementHelper.ResolveLanguages(mlp).Returns([]);

        var result = _sut.Extract(mlp, _ => null);

        var branch = IsType<SemanticBranchNode>(result);
        Empty(branch.Children);
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("No languages defined")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }

    [Fact]
    public void FillOut_WithMatchingLeafNodes_SetsLanguageValues()
    {
        var mlp = new MultiLanguageProperty(
            idShort: "MfName",
            value: [new LangStringTextType("en", ""), new LangStringTextType("de", "")]
        );
        _resolver.ExtractSemanticId(mlp).Returns("http://test/mfname");
        _resolver.MlpPostFixSeparator.Returns("_");
        _elementHelper.ResolveLanguages(mlp).Returns(["en", "de"]);

        var valueNode = new SemanticBranchNode("http://test/mfname", Cardinality.One);
        valueNode.AddChild(new SemanticLeafNode("http://test/mfname_en", "English Value", DataType.String, Cardinality.One));
        valueNode.AddChild(new SemanticLeafNode("http://test/mfname_de", "German Value", DataType.String, Cardinality.One));

        _sut.FillOut(mlp, valueNode, (_, _, _) => { });

        Equal("English Value", mlp.Value!.First(v => v.Language == "en").Text);
        Equal("German Value", mlp.Value!.First(v => v.Language == "de").Text);
    }

    [Fact]
    public void FillOut_WithNoMatchingValueNode_LogsInfo()
    {
        var mlp = new MultiLanguageProperty(idShort: "MfName");
        _resolver.ExtractSemanticId(mlp).Returns("http://test/mfname");
        var nonMatchingNode = new SemanticBranchNode("http://test/other", Cardinality.One);

        _sut.FillOut(mlp, nonMatchingNode, (_, _, _) => { });

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("No value node found")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }

    [Fact]
    public void FillOut_WithNewDefaultLanguage_AddsLanguageAndLogsInfo()
    {
        var mlp = new MultiLanguageProperty(
            idShort: "MfName",
            value: [new LangStringTextType("en", "")]
        );
        _resolver.ExtractSemanticId(mlp).Returns("http://test/mfname");
        _resolver.MlpPostFixSeparator.Returns("_");
        _elementHelper.ResolveLanguages(mlp).Returns(["en", "fr"]);

        var valueNode = new SemanticBranchNode("http://test/mfname", Cardinality.One);
        valueNode.AddChild(new SemanticLeafNode("http://test/mfname_en", "English", DataType.String, Cardinality.One));
        valueNode.AddChild(new SemanticLeafNode("http://test/mfname_fr", "French", DataType.String, Cardinality.One));

        _sut.FillOut(mlp, valueNode, (_, _, _) => { });

        Equal(2, mlp.Value!.Count);
        Equal("French", mlp.Value.First(v => v.Language == "fr").Text);
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("Added language 'fr'")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }
}
