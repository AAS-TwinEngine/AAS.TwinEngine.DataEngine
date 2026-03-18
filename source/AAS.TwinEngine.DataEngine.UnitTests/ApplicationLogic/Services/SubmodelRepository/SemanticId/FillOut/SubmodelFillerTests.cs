using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.FillOut;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.FillOut;

public class SubmodelFillerTests
{
    private readonly SubmodelFiller _sut;
    private readonly ISemanticIdResolver _resolver;
    private readonly ISubmodelElementHelper _elementHelper;
    private readonly ILogger<SubmodelFiller> _logger;
    private readonly List<ISubmodelElementTypeHandler> _handlers;

    public SubmodelFillerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _elementHelper = Substitute.For<ISubmodelElementHelper>();
        _logger = Substitute.For<ILogger<SubmodelFiller>>();
        _handlers = [];
        _sut = new SubmodelFiller(_resolver, _elementHelper, _handlers, _logger);
    }

    [Fact]
    public void FillOutTemplate_NullSubmodel_ThrowsArgumentNullException()
    {
        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        Throws<ArgumentNullException>(() => _sut.FillOutTemplate(null!, values));
    }

    [Fact]
    public void FillOutTemplate_NullValues_ThrowsArgumentNullException()
    {
        var submodel = Substitute.For<ISubmodel>();
        submodel.SubmodelElements.Returns([]);

        Throws<ArgumentNullException>(() => _sut.FillOutTemplate(submodel, null!));
    }

    [Fact]
    public void FillOutTemplate_NullSubmodelElements_ThrowsArgumentNullException()
    {
        var submodel = Substitute.For<ISubmodel>();
        submodel.SubmodelElements.Returns((List<ISubmodelElement>?)null);
        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        Throws<ArgumentNullException>(() => _sut.FillOutTemplate(submodel, values));
    }

    [Fact]
    public void FillOutTemplate_NoMatchingNodes_PreservesElements()
    {
        var property = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);
        var submodel = Substitute.For<ISubmodel>();
        var elements = new List<ISubmodelElement> { property };
        submodel.SubmodelElements.Returns(elements);
        _resolver.ExtractSemanticId(property).Returns("http://test/prop");

        var values = new SemanticBranchNode("root", Cardinality.Unknown);

        _sut.FillOutTemplate(submodel, values);

        Single(elements);
    }

    [Fact]
    public void FillOutElement_NullElement_ThrowsArgumentNullException()
    {
        var values = new SemanticLeafNode("test", "val", DataType.String, Cardinality.One);

        Throws<ArgumentNullException>(() => _sut.FillOutElement(null!, values));
    }

    [Fact]
    public void FillOutElement_NullValues_ThrowsArgumentNullException()
    {
        var element = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);

        Throws<ArgumentNullException>(() => _sut.FillOutElement(element, null!));
    }

    [Fact]
    public void FillOutElement_NoMatchingHandler_ThrowsException()
    {
        var element = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);
        var values = new SemanticLeafNode("test", "val", DataType.String, Cardinality.One);

        var ex = Throws<InternalDataProcessingException>(() => _sut.FillOutElement(element, values));
        Equal("Internal Server Error.", ex.Message);
    }

    [Fact]
    public void FillOutElement_WithMatchingHandler_DelegatesToHandler()
    {
        var element = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);
        var values = new SemanticLeafNode("test", "val", DataType.String, Cardinality.One);

        var handler = Substitute.For<ISubmodelElementTypeHandler>();
        handler.CanHandle(element).Returns(true);
        _handlers.Add(handler);

        _sut.FillOutElement(element, values);

        handler.Received(1).FillOut(element, values, Arg.Any<Action<List<ISubmodelElement>, SemanticTreeNode, bool>>());
    }
}
