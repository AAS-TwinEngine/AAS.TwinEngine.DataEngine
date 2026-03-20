using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using NSubstitute;

using static Xunit.Assert;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.ElementHandlers;

public class RelationshipElementHandlerTests
{
    private readonly RelationshipElementHandler _sut;
    private readonly ISemanticIdResolver _resolver;
    private readonly IReferenceHelper _referenceHelper;

    public RelationshipElementHandlerTests()
    {
        _resolver = Substitute.For<ISemanticIdResolver>();
        _referenceHelper = Substitute.For<IReferenceHelper>();
        _sut = new RelationshipElementHandler(_resolver, _referenceHelper);
    }

    [Fact]
    public void CanHandle_RelationshipElement_ReturnsTrue()
    {
        var rel = new RelationshipElement(
            first: new Reference(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, "a")]),
            second: new Reference(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, "b")]),
            idShort: "Test"
        );

        True(_sut.CanHandle(rel));
    }

    [Fact]
    public void CanHandle_NonRelationshipElement_ReturnsFalse()
    {
        var property = new Property(idShort: "Test", valueType: DataTypeDefXsd.String);

        False(_sut.CanHandle(property));
    }

    [Fact]
    public void Extract_BothExternalReferences_ReturnsNull()
    {
        var rel = new RelationshipElement(
            first: new Reference(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, "a")]),
            second: new Reference(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, "b")]),
            idShort: "Test"
        );

        var result = _sut.Extract(rel, _ => null);

        Null(result);
    }

    [Fact]
    public void Extract_FirstModelReference_ExtractsFirstAndDelegatesToReferenceHelper()
    {
        var firstRef = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, "sub")]);
        var secondRef = new Reference(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, "ext")]);
        var rel = new RelationshipElement(first: firstRef, second: secondRef, idShort: "Test");
        _resolver.ExtractSemanticId(rel).Returns("http://test/rel");
        _resolver.GetCardinality(rel).Returns(Cardinality.One);

        var firstNode = new SemanticBranchNode("http://test/rel_first", Cardinality.One);
        _referenceHelper.ExtractReferenceKeys(
            firstRef,
            "http://test/rel" + SemanticIdResolver.RelationshipElementFirstPostFixSeparator,
            Cardinality.One
        ).Returns(firstNode);

        var result = _sut.Extract(rel, _ => null);

        var branch = IsType<SemanticBranchNode>(result);
        Equal("http://test/rel", branch.SemanticId);
        Single(branch.Children);
        Same(firstNode, branch.Children[0]);
    }

    [Fact]
    public void Extract_BothModelReferences_ExtractsBoth()
    {
        var firstRef = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, "sub1")]);
        var secondRef = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, "sub2")]);
        var rel = new RelationshipElement(first: firstRef, second: secondRef, idShort: "Test");
        _resolver.ExtractSemanticId(rel).Returns("http://test/rel");
        _resolver.GetCardinality(rel).Returns(Cardinality.One);

        var firstNode = new SemanticBranchNode("http://test/rel_first", Cardinality.One);
        var secondNode = new SemanticBranchNode("http://test/rel_second", Cardinality.One);
        _referenceHelper.ExtractReferenceKeys(
            firstRef,
            "http://test/rel" + SemanticIdResolver.RelationshipElementFirstPostFixSeparator,
            Cardinality.One
        ).Returns(firstNode);
        _referenceHelper.ExtractReferenceKeys(
            secondRef,
            "http://test/rel" + SemanticIdResolver.RelationshipElementSecondPostFixSeparator,
            Cardinality.One
        ).Returns(secondNode);

        var result = _sut.Extract(rel, _ => null);

        var branch = IsType<SemanticBranchNode>(result);
        Equal(2, branch.Children.Count);
    }

    [Fact]
    public void FillOut_DelegatesToReferenceHelperForBothReferences()
    {
        var firstRef = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, "")]);
        var secondRef = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, "")]);
        var rel = new RelationshipElement(first: firstRef, second: secondRef, idShort: "Test");

        var values = new SemanticBranchNode("http://test/rel", Cardinality.One);

        _sut.FillOut(rel, values, (_, _, _) => { });

        _referenceHelper.Received(1).PopulateRelationshipReference(
            firstRef, values, "http://test/rel",
            SemanticIdResolver.RelationshipElementFirstPostFixSeparator);
        _referenceHelper.Received(1).PopulateRelationshipReference(
            secondRef, values, "http://test/rel",
            SemanticIdResolver.RelationshipElementSecondPostFixSeparator);
    }
}
