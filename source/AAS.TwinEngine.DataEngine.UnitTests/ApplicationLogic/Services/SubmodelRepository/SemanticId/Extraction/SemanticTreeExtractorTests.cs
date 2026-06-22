using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Extraction;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.Extraction;

public class SemanticTreeExtractorTests
{
    private readonly SemanticTreeExtractor _sut;
    private readonly ISemanticIdResolver _resolver;
    private readonly ISubmodelElementHelper _elementHelper;
    private readonly ILogger<SemanticTreeExtractor> _logger;
    private readonly List<ISubmodelElementTypeHandler> _handlers;

    public SemanticTreeExtractorTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _logger = Substitute.For<ILogger<SemanticTreeExtractor>>();
        _elementHelper = Substitute.For<ISubmodelElementHelper>();
        _handlers = [];
        _sut = new SemanticTreeExtractor(_resolver, _elementHelper, _handlers, _logger);
    }

    [Fact]
    public void Extract_NullSubmodel_ThrowsInvalidDependencyException() => Throws<InvalidDependencyException>(() => _sut.Extract(null!));

    [Fact]
    public void Extract_SubmodelWithNoElements_ReturnsRootNodeWithNoChildren()
    {
        var submodel = Substitute.For<ISubmodel>();
        submodel.IdShort.Returns("TestSubmodel");
        submodel.SubmodelElements.Returns([]);
        _resolver.ResolveSemanticId(submodel, "TestSubmodel").Returns("http://test/root");

        var result = _sut.Extract(submodel) as SemanticBranchNode;

        NotNull(result);
        Equal("http://test/root", result!.SemanticId);
        Empty(result.Children);
    }

    [Fact]
    public void Extract_SubmodelWithElements_DelegatesToHandlers()
    {
        var property = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);
        var submodel = Substitute.For<ISubmodel>();
        submodel.IdShort.Returns("Test");
        submodel.SubmodelElements.Returns([property]);
        _resolver.ResolveSemanticId(submodel, "Test").Returns("http://test/root");

        var handler = Substitute.For<ISubmodelElementTypeHandler>();
        handler.CanHandle(property).Returns(true);
        var expectedNode = new SemanticLeafNode("http://test/prop", "", DataType.String, Cardinality.One);
        handler.Extract(property, Arg.Any<Func<ISubmodelElement, SemanticTreeNode?>>()).Returns(expectedNode);
        _handlers.Add(handler);

        var result = _sut.Extract(submodel) as SemanticBranchNode;

        NotNull(result);
        Single(result!.Children);
        Same(expectedNode, result.Children[0]);
    }

    [Fact]
    public void Extract_ElementWithNoHandler_CreatesLeafNodeFallback()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.IdShort.Returns("UnknownElement");
        var submodel = Substitute.For<ISubmodel>();
        submodel.IdShort.Returns("Test");
        submodel.SubmodelElements.Returns([element]);
        _resolver.ResolveSemanticId(submodel, "Test").Returns("http://test/root");
        _resolver.ResolveElementSemanticId(element, "UnknownElement").Returns("http://test/unknown");
        _resolver.GetValueType(element).Returns(DataType.Unknown);
        _resolver.GetCardinality(element).Returns(Cardinality.One);

        var result = _sut.Extract(submodel) as SemanticBranchNode;

        NotNull(result);
        Single(result!.Children);
        var leaf = IsType<SemanticLeafNode>(result.Children[0]);
        Equal("http://test/unknown", leaf.SemanticId);
        Equal(DataType.Unknown, leaf.DataType);
    }

    [Fact]
    public void Extract_ElementWithUnknownCardinality_ThrowsInternalDataProcessingException()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.IdShort.Returns("ElementWithUnknownCard");
        var submodel = Substitute.For<ISubmodel>();
        submodel.IdShort.Returns("Test");
        submodel.SubmodelElements.Returns([element]);
        _resolver.ResolveSemanticId(submodel, "Test").Returns("http://test/root");
        _resolver.GetCardinality(Arg.Is(element)).Returns(x => throw new InternalDataProcessingException("Cardinality is mandatory for SubmodelElement 'ElementWithUnknownCard' in template. Found: Unknown"));
        _resolver.ResolveElementSemanticId(element, "ElementWithUnknownCard").Returns("http://test/elem");
        _resolver.GetValueType(element).Returns(DataType.String);

        var exception = Throws<InternalDataProcessingException>(() => _sut.Extract(submodel));
        Contains("Cardinality is mandatory", exception.Message);
    }

    [Fact]
    public void Extract_SubmodelWithoutCardinality_ReturnsRootNodeWithCardinalityOne()
    {
        var submodel = Substitute.For<ISubmodel>();
        submodel.IdShort.Returns("TestSubmodel");
        submodel.SubmodelElements.Returns([]);
        _resolver.ResolveSemanticId(submodel, "TestSubmodel").Returns("http://test/root");

        var result = _sut.Extract(submodel) as SemanticBranchNode;

        NotNull(result);
        Equal(Cardinality.One, result!.Cardinality);
    }

    [Theory]
    [InlineData(Cardinality.ZeroToOne)]
    [InlineData(Cardinality.One)]
    [InlineData(Cardinality.ZeroToMany)]
    [InlineData(Cardinality.OneToMany)]
    public void Extract_ElementWithValidCardinality_CreatesLeafNode(Cardinality validCardinality)
    {
        var element = Substitute.For<ISubmodelElement>();
        element.IdShort.Returns("ValidElement");
        var submodel = Substitute.For<ISubmodel>();
        submodel.IdShort.Returns("Test");
        submodel.SubmodelElements.Returns([element]);
        _resolver.ResolveSemanticId(submodel, "Test").Returns("http://test/root");
        _resolver.ResolveElementSemanticId(element, "ValidElement").Returns("http://test/valid");
        _resolver.GetValueType(element).Returns(DataType.String);
        _resolver.GetCardinality(element).Returns(validCardinality);

        var result = _sut.Extract(submodel) as SemanticBranchNode;

        NotNull(result);
        Single(result!.Children);
        var leaf = IsType<SemanticLeafNode>(result.Children[0]);
        Equal(validCardinality, leaf.Cardinality);
    }

    [Fact]
    public void Extract_ByIdShortPath_NullSubmodel_ThrowsInvalidDependencyException() => Throws<InvalidDependencyException>(() => _sut.Extract(null!, "path"));

    [Fact]
    public void Extract_ByIdShortPath_NullPath_ThrowsInvalidDependencyException()
    {
        var submodel = Substitute.For<ISubmodel>();
        Throws<InvalidDependencyException>(() => _sut.Extract(submodel, null!));
    }

    [Fact]
    public void Extract_ByIdShortPath_SingleSegment_ReturnsMatchingElement()
    {
        var property = new Property(idShort: "MyProp", valueType: DataTypeDefXsd.String, value: "test");
        var submodel = Substitute.For<ISubmodel>();
        submodel.SubmodelElements.Returns([property]);
        _elementHelper.GetElementByIdShort(Arg.Any<IEnumerable<ISubmodelElement>>(), "MyProp").Returns(property);

        var result = _sut.Extract(submodel, "MyProp");

        Same(property, result);
    }

    [Fact]
    public void Extract_ByIdShortPath_NestedPath_ReturnsNestedElement()
    {
        var childProp = new Property(idShort: "ChildProp", valueType: DataTypeDefXsd.String);
        var collection = new SubmodelElementCollection(idShort: "Parent", value: [childProp]);
        var submodel = Substitute.For<ISubmodel>();
        submodel.SubmodelElements.Returns([collection]);
        _elementHelper.GetElementByIdShort(Arg.Any<IEnumerable<ISubmodelElement>>(), "Parent").Returns(collection);
        _elementHelper.GetChildElements(collection).Returns(collection.Value);
        _elementHelper.GetElementByIdShort(collection.Value, "ChildProp").Returns(childProp);

        var result = _sut.Extract(submodel, "Parent.ChildProp");

        Same(childProp, result);
    }

    [Fact]
    public void Extract_ByIdShortPath_ElementNotFound_ThrowsException()
    {
        var submodel = Substitute.For<ISubmodel>();
        submodel.SubmodelElements.Returns([]);
        _elementHelper.GetElementByIdShort(Arg.Any<IEnumerable<ISubmodelElement>>(), "NonExistent").Returns((ISubmodelElement?)null);

        Throws<InternalDataProcessingException>(() => _sut.Extract(submodel, "NonExistent"));
    }

    [Fact]
    public void Extract_ByIdShortPath_ChildElementsNull_ThrowsException()
    {
        var property = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);
        var submodel = Substitute.For<ISubmodel>();
        submodel.SubmodelElements.Returns([property]);
        _elementHelper.GetElementByIdShort(Arg.Any<IEnumerable<ISubmodelElement>>(), "Prop").Returns(property);
        _elementHelper.GetChildElements(property).Returns((IList<ISubmodelElement>?)null);

        Throws<InternalDataProcessingException>(() => _sut.Extract(submodel, "Prop.Child"));
    }

    [Fact]
    public void ExtractElement_WithUnknownCardinality_ThrowsInternalDataProcessingException()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.IdShort.Returns("TestElement");
        _resolver.ResolveElementSemanticId(element, "TestElement").Returns("http://test/elem");
        _resolver.GetValueType(element).Returns(DataType.String);
    _resolver.GetCardinality(Arg.Is(element)).Returns(x => throw new InternalDataProcessingException("Cardinality is mandatory for SubmodelElement 'TestElement' in template. Found: Unknown"));

        var exception = Throws<InternalDataProcessingException>(() => _sut.ExtractElement(element));
        Contains("Cardinality is mandatory", exception.Message);
    }

    [Theory]
    [InlineData(Cardinality.ZeroToOne)]
    [InlineData(Cardinality.One)]
    [InlineData(Cardinality.ZeroToMany)]
    [InlineData(Cardinality.OneToMany)]
    public void ExtractElement_WithValidCardinality_CreatesLeafNode(Cardinality validCardinality)
    {
        var element = Substitute.For<ISubmodelElement>();
        element.IdShort.Returns("ValidElement");
        _resolver.ResolveElementSemanticId(element, "ValidElement").Returns("http://test/valid");
        _resolver.GetValueType(element).Returns(DataType.Integer);
        _resolver.GetCardinality(element).Returns(validCardinality);

        var result = _sut.ExtractElement(element);

        var leaf = IsType<SemanticLeafNode>(result);
        Equal(validCardinality, leaf.Cardinality);
        Equal(DataType.Integer, leaf.DataType);
    }
}
