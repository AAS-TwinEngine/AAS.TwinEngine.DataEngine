using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class ReferenceElementHandlerTests
{
    private readonly ReferenceElementHandler _sut;
    private readonly ISemanticIdResolver _resolver;
    private readonly IReferenceHelper _referenceHelper;
    private readonly ILogger<ReferenceElementHandler> _logger;

    public ReferenceElementHandlerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _referenceHelper = Substitute.For<IReferenceHelper>();
        _logger = Substitute.For<ILogger<ReferenceElementHandler>>();
        _sut = new ReferenceElementHandler(_resolver, _referenceHelper, _logger);
    }

    [Fact]
    public void CanHandle_ReferenceElement_ReturnsTrue()
    {
        var refElement = new ReferenceElement(idShort: "Test");

        True(_sut.CanHandle(refElement));
    }

    [Fact]
    public void CanHandle_NonReferenceElement_ReturnsFalse()
    {
        var property = new Property(idShort: "Test", valueType: DataTypeDefXsd.String);

        False(_sut.CanHandle(property));
    }

    [Fact]
    public void Extract_WithNullValue_ReturnsNull()
    {
        var refElement = new ReferenceElement(idShort: "Test", value: null);

        var result = _sut.Extract(refElement, _ => null);

        Null(result);
    }

    [Fact]
    public void Extract_WithExternalReference_ReturnsNull()
    {
        var refElement = new ReferenceElement(
            idShort: "Test",
            value: new Reference(ReferenceTypes.ExternalReference,
                [new Key(KeyTypes.GlobalReference, "http://external")])
        );

        var result = _sut.Extract(refElement, _ => null);

        Null(result);
    }

    [Fact]
    public void Extract_WithModelReference_DelegatesToReferenceHelper()
    {
        var modelRef = new Reference(ReferenceTypes.ModelReference,
            [new Key(KeyTypes.Submodel, "http://submodel")]);
        var refElement = new ReferenceElement(idShort: "Test", value: modelRef);
        _resolver.ResolveElementSemanticId(refElement, "Test").Returns("http://test/ref");
        _resolver.GetCardinality(refElement).Returns(Cardinality.One);

        var expectedNode = new SemanticBranchNode("http://test/ref", Cardinality.One);
        _referenceHelper.ExtractReferenceKeys(modelRef, "http://test/ref", Cardinality.One).Returns(expectedNode);

        var result = _sut.Extract(refElement, _ => null);

        Same(expectedNode, result);
    }

    [Fact]
    public void FillOut_WithModelReference_DelegatesToReferenceHelper()
    {
        var modelRef = new Reference(ReferenceTypes.ModelReference,
            [new Key(KeyTypes.Submodel, "")]);
        var refElement = new ReferenceElement(idShort: "Test", value: modelRef);
        _resolver.ExtractSemanticId(refElement).Returns("http://test/ref");

        var values = new SemanticBranchNode("http://test/ref", Cardinality.One);

        _sut.FillOut(refElement, values, (_, _, _) => { });

        _referenceHelper.Received(1).PopulateReferenceKeys(modelRef, values, "http://test/ref");
    }

    [Fact]
    public void FillOut_WithNullValue_LogsInfoAndSkips()
    {
        var refElement = new ReferenceElement(idShort: "Test", value: null);
        _resolver.ExtractSemanticId(refElement).Returns("http://test/ref");

        var values = new SemanticBranchNode("http://test/ref", Cardinality.One);

        _sut.FillOut(refElement, values, (_, _, _) => { });

        _referenceHelper.DidNotReceive().PopulateReferenceKeys(
            Arg.Any<IReference>(), Arg.Any<SemanticTreeNode>(), Arg.Any<string>());
    }

    [Fact]
    public void FillOut_WithExternalReference_LogsInfoAndSkips()
    {
        var externalRef = new Reference(ReferenceTypes.ExternalReference,
            [new Key(KeyTypes.GlobalReference, "http://external")]);
        var refElement = new ReferenceElement(idShort: "Test", value: externalRef);
        _resolver.ExtractSemanticId(refElement).Returns("http://test/ref");

        var values = new SemanticBranchNode("http://test/ref", Cardinality.One);

        _sut.FillOut(refElement, values, (_, _, _) => { });

        _referenceHelper.DidNotReceive().PopulateReferenceKeys(
            Arg.Any<IReference>(), Arg.Any<SemanticTreeNode>(), Arg.Any<string>());
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains("does not contain a ModelReference")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!
        );
    }
}
