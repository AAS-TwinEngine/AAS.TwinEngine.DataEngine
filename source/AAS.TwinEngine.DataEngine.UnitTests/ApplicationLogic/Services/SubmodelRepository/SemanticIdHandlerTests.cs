using System.Reflection;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MongoDB.Bson;

using NSubstitute;

using static Xunit.Assert;

using File = AasCore.Aas3_0.File;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository;

public class SemanticIdHandlerTests
{
    private readonly SemanticIdHandler _sut;
    private readonly ILogger<SemanticIdHandler> _logger;
    private readonly IOptions<MultiLanguagePropertySettings> _mlpSettings;
    private readonly IOptions<Semantics> _semantics;

    public SemanticIdHandlerTests()
    {
        _logger = Substitute.For<ILogger<SemanticIdHandler>>();
        _mlpSettings = Substitute.For<IOptions<MultiLanguagePropertySettings>>();
        _ = _mlpSettings.Value.Returns(new MultiLanguagePropertySettings { DefaultLanguages = null });
        _semantics = Substitute.For<IOptions<Semantics>>();
        _ = _semantics.Value.Returns(new Semantics { MultiLanguageSemanticPostfixSeparator = "_", SubmodelElementIndexContextPrefix = "_aastwinengineindex_" });
        _sut = new SemanticIdHandler(_logger, _semantics, _mlpSettings);
    }

    [Fact]
    public void Extract_TemplateNull_ThrowsException()
    {
        _ = Throws<ArgumentNullException>(() => _sut.Extract(submodelTemplate: null!));
        _ = Throws<ArgumentNullException>(() => _sut.Extract(submodelTemplate: null!, idShortPath: "TestIdShortPath"));
    }

    [Fact]
    public void Extract_IdShortNull_ThrowsException() => _ = Throws<ArgumentNullException>(() => _sut.Extract(TestData.CreateSubmodel(), null!));

    [Fact]
    public void FillOutTemplate_ValuesNull_ThrowsException() => _ = Throws<ArgumentNullException>(() => _sut.FillOutTemplate(TestData.CreateSubmodel(), values: null!));

    [Fact]
    public void FillOutTemplate_TemplateNull_ThrowsException() => _ = Throws<ArgumentNullException>(() => _sut.FillOutTemplate(submodelTemplate: null!, TestData.SubmodelTreeNode));

    [Fact]
    public void SemanticIdHandler_NullSemantics_ThrowsException()
    {
        var options = Options.Create<Semantics>(options: null!);
        var logger = Substitute.For<ILogger<SemanticIdHandler>>();

        _ = Throws<NullReferenceException>(() => new SemanticIdHandler(logger, options, _mlpSettings));
    }

    [Fact]
    public void Extract_Submodel_ReturnsSemanticTreeNode()
    {
        var node = (SemanticBranchNode)_sut.Extract(TestData.CreateSubmodel());

        Equal("http://example.com/idta/digital-nameplate/semantic-id", node.SemanticId);
        Equal(8, node.Children.Count);
        var manufacturerNameNode = node.Children[0] as SemanticBranchNode;
        NotNull(manufacturerNameNode);
        Equal("http://example.com/idta/digital-nameplate/manufacturer-name", manufacturerNameNode.SemanticId);
        var manufacturerNameNodeEn = manufacturerNameNode.Children[0] as SemanticLeafNode;
        NotNull(manufacturerNameNodeEn);
        Equal("http://example.com/idta/digital-nameplate/manufacturer-name_en", manufacturerNameNodeEn.SemanticId);
        Equal(DataType.String, manufacturerNameNodeEn.DataType);
        Equal(Cardinality.ZeroToOne, manufacturerNameNodeEn.Cardinality);
        var manufacturerNameNodeDe = manufacturerNameNode.Children[1] as SemanticLeafNode;
        NotNull(manufacturerNameNodeDe);
        Equal("http://example.com/idta/digital-nameplate/manufacturer-name_de", manufacturerNameNodeDe.SemanticId);
        Equal(DataType.String, manufacturerNameNodeDe.DataType);
        Equal(Cardinality.ZeroToOne, manufacturerNameNodeDe.Cardinality);
        var modelTypeNode = node.Children[1] as SemanticLeafNode;
        NotNull(modelTypeNode);
        Equal("http://example.com/idta/digital-nameplate/model-type", modelTypeNode.SemanticId);
        Equal(DataType.Number, modelTypeNode.DataType);
        Equal(Cardinality.ZeroToOne, modelTypeNode.Cardinality);
        var contactListNode = node.Children[2] as SemanticBranchNode;
        NotNull(contactListNode);
        Equal("http://example.com/idta/digital-nameplate/contact-list", contactListNode.SemanticId);
        Equal(2, contactListNode.Children.Count);
        var contactNameNode = contactListNode.Children[0] as SemanticLeafNode;
        NotNull(contactNameNode);
        Equal("http://example.com/idta/digital-nameplate/contact-name", contactNameNode.SemanticId);
        var modelNameNode = contactListNode.Children[1] as SemanticLeafNode;
        NotNull(modelNameNode);
        Equal("http://example.com/idta/digital-nameplate/model-name", modelNameNode.SemanticId);
        var contactInformationNode = node.Children[3] as SemanticBranchNode;
        NotNull(contactInformationNode);
        Equal("http://example.com/idta/digital-nameplate/contact-information", contactInformationNode.SemanticId);
        _ = Single(contactInformationNode.Children);
        var contactNameNode2 = contactInformationNode.Children[0] as SemanticLeafNode;
        NotNull(contactNameNode2);
        Equal("http://example.com/idta/digital-nameplate/contact-name", contactNameNode2.SemanticId);

        var file = node.Children[4] as SemanticLeafNode;
        NotNull(file);
        Equal("http://example.com/idta/digital-nameplate/thumbnail", file.SemanticId);
        Equal(DataType.String, file.DataType);
        Equal(Cardinality.Unknown, file.Cardinality);

        var blob = node.Children[5] as SemanticLeafNode;
        NotNull(blob);
        Equal("http://example.com/idta/digital-nameplate/blob", blob.SemanticId);
        Equal(DataType.String, blob.DataType);
        Equal(Cardinality.Unknown, blob.Cardinality);

        var range = node.Children[6] as SemanticBranchNode;
        NotNull(range);
        Equal("http://example.com/idta/digital-nameplate/range", range.SemanticId);
        var rangeMinNode = range.Children[0] as SemanticLeafNode;
        Equal("http://example.com/idta/digital-nameplate/range_min", rangeMinNode!.SemanticId);
        var rangeMaxNode = range.Children[1] as SemanticLeafNode;
        Equal("http://example.com/idta/digital-nameplate/range_max", rangeMaxNode!.SemanticId);

        var entityNode = node.Children[7] as SemanticBranchNode;
        NotNull(entityNode);
        Equal("http://example.com/idta/digital-nameplate/entitynode", entityNode.SemanticId);
        var entityNodeLeaf = entityNode.Children[0] as SemanticLeafNode;
        NotNull(entityNodeLeaf);
        Equal("http://example.com/idta/digital-nameplate/entitynode_globalAssetId", entityNodeLeaf.SemanticId);

        var entityNodeLeaf2 = entityNode.Children[1] as SemanticLeafNode;
        NotNull(entityNodeLeaf2);
        Equal("https://example.com/cd/manufacturer", entityNodeLeaf2.SemanticId);

        var entityNodeLeaf3 = entityNode.Children[2] as SemanticLeafNode;
        NotNull(entityNodeLeaf3);
        Equal("https://example.com/cd/serialnumber", entityNodeLeaf3.SemanticId);

        var entityNodeLeaf4 = entityNode.Children[3] as SemanticLeafNode;
        NotNull(entityNodeLeaf4);
        Equal("http://example.com/idta/digital-nameplate/contact-name", entityNodeLeaf4.SemanticId);
    }

    [Fact]
    public void Extract_SubmodelWithReferenceElement_ReturnsExpectedStructure()
    {
        var submodel = TestData.CreateSubmodelWithReferenceElement();

        var node = _sut.Extract(submodel) as SemanticBranchNode;

        Equal("http://example.com/idta/digital-nameplate/semantic-id", node?.SemanticId);
        Single(node!.Children);
        var referenceElementNode = node.Children[0] as SemanticBranchNode;
        NotEqual("http://example.com/idta/digital-nameplate/reference-element/external-reference", referenceElementNode?.SemanticId);
        Equal("http://example.com/idta/digital-nameplate/reference-element/model-reference", referenceElementNode?.SemanticId);
        Equal(5, referenceElementNode!.Children.Count);
        var key1 = referenceElementNode.Children[0] as SemanticLeafNode;
        Equal("http://example.com/idta/digital-nameplate/reference-element/model-reference_Submodel", key1?.SemanticId);
        var key2 = referenceElementNode.Children[1] as SemanticLeafNode;
        Equal("http://example.com/idta/digital-nameplate/reference-element/model-reference_SubmodelElementList", key2?.SemanticId);
        var key3 = referenceElementNode.Children[2] as SemanticLeafNode;
        Equal("http://example.com/idta/digital-nameplate/reference-element/model-reference_SubmodelElementCollection_0", key3?.SemanticId);
        var key4 = referenceElementNode.Children[3] as SemanticLeafNode;
        Equal("http://example.com/idta/digital-nameplate/reference-element/model-reference_SubmodelElementCollection_1", key4?.SemanticId);
        var key5 = referenceElementNode.Children[4] as SemanticLeafNode;
        Equal("http://example.com/idta/digital-nameplate/reference-element/model-reference_Property", key5?.SemanticId);
    }

    [Fact]
    public void Extract_SubmodelWithRelationshipElement_ReturnsExpectedStructure()
    {
        var submodel = TestData.CreateSubmodelRelationshipElement();

        var node = _sut.Extract(submodel) as SemanticBranchNode;

        Equal("http://example.com/idta/digital-nameplate/semantic-id", node?.SemanticId);
        Equal(2, node!.Children.Count);
        var relationshipElementWithBoth = node.Children[0] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/relationship-element/both-model-reference", relationshipElementWithBoth!.SemanticId);
        Equal(2, relationshipElementWithBoth.Children.Count);
        var firstBranchNode = relationshipElementWithBoth.Children[0] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/relationship-element/both-model-reference_first", firstBranchNode!.SemanticId);
        Equal(4, firstBranchNode.Children.Count);
        var secondBranchNode = relationshipElementWithBoth.Children[1] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/relationship-element/both-model-reference_second", secondBranchNode!.SemanticId);
        Equal(3, secondBranchNode.Children.Count);
        var relationshipElementWithOne = node.Children[1] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/relationship-element/second-model-reference", relationshipElementWithOne!.SemanticId);
        Single(relationshipElementWithOne!.Children);
        var branchNode = relationshipElementWithOne.Children[0] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/relationship-element/second-model-reference_second", branchNode!.SemanticId);
        Equal(4, branchNode.Children.Count);
    }

    [Fact]
    public void Extract_SubmodelWithModel3DList_ReturnsExpectedStructure()
    {
        var submodel = TestData.CreateSubmodelWithModel3DList();

        var node = _sut.Extract(submodel) as SemanticBranchNode;

        Equal("http://example.com/idta/digital-nameplate/semantic-id", node?.SemanticId);
        Single(node!.Children);
        var model3D = node!.Children[0] as SemanticBranchNode;
        NotNull(model3D);
        Equal("http://example.com/idta/digital-nameplate/model-3d", model3D.SemanticId);
        Equal(2, model3D.Children.Count);
        Equal(Cardinality.ZeroToOne, model3D.Cardinality);
        var modelDataCollection = model3D.Children[0] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/model-data_aastwinengineindex_1", modelDataCollection?.SemanticId);
        Equal(Cardinality.ZeroToMany, modelDataCollection?.Cardinality);
        var modelDataCollectionFile = modelDataCollection!.Children[0] as SemanticLeafNode;
        Equal(DataType.String, modelDataCollectionFile?.DataType);
    }

    [Fact]
    public void Extract_EmptyMultiLanguageProperty_WithDefaultLanguagesAsNull()
    {
        var mlp = TestData.CreateSubmodelWithManufacturerNameWithOutElements();

        var node = _sut.Extract(mlp) as SemanticBranchNode;

        Equal("http://example.com/idta/digital-nameplate/semantic-id", node?.SemanticId);
        Single(node!.Children);
        var manufacturerNameNode = node.Children[0] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/manufacturer-name", manufacturerNameNode?.SemanticId);
        Empty(manufacturerNameNode!.Children);
    }

    [Fact]
    public void Extract_EmptyMultiLanguageProperty_WithDefaultLanguagesAs_En_De_Fr()
    {
        var mlpSettings = CreateMlpSettings(["de", "en", "fr"]);
        var sut = new SemanticIdHandler(_logger, _semantics, mlpSettings);
        var mlp = TestData.CreateSubmodelWithManufacturerNameWithOutElements();

        var node = sut.Extract(mlp) as SemanticBranchNode;

        Equal("http://example.com/idta/digital-nameplate/semantic-id", node?.SemanticId);
        Single(node!.Children);
        var manufacturerNameNode = node.Children[0] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/manufacturer-name", manufacturerNameNode?.SemanticId);
        Equal(3, manufacturerNameNode!.Children.Count);
        var languages = manufacturerNameNode.Children.Select(c => c.SemanticId.Split('_').Last()).OrderBy(l => l).ToList();
        Equal(["de", "en", "fr"], languages);
    }

    [Fact]
    public void Extract_MultiLanguageProperty_WithDefaultLanguagesAs_En_De_Fr()
    {
        var mlpSettings = CreateMlpSettings(["de", "en", "fr"]);
        var sut = new SemanticIdHandler(_logger, _semantics, mlpSettings);
        var mlp = TestData.CreateSubmodelWithManufacturerNameWithTwoLanguagesInTemplate();

        var node = sut.Extract(mlp) as SemanticBranchNode;

        Equal("http://example.com/idta/digital-nameplate/semantic-id", node?.SemanticId);
        Single(node!.Children);
        var manufacturerNameNode = node.Children[0] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/manufacturer-name", manufacturerNameNode?.SemanticId);
        Equal(3, manufacturerNameNode!.Children.Count);
        var languages = manufacturerNameNode.Children.Select(c => c.SemanticId.Split('_').Last()).OrderBy(l => l).ToList();
        Equal(["de", "en", "fr"], languages);
    }

    [Fact]
    public void Extract_EmptySubmodelElementCollection_LogsWarningAndReturnsNode()
    {
        var mlp = TestData.CreateSubmodelWithContactInformationWithOutElements();

        var node = _sut.Extract(mlp) as SemanticBranchNode;

        Equal("http://example.com/idta/digital-nameplate/semantic-id", node?.SemanticId);
        Single(node!.Children);
        var contactInformationNode = node.Children[0] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/contact-information", contactInformationNode?.SemanticId);
        Empty(contactInformationNode!.Children);
        _logger.Received(1).Log(LogLevel.Warning, Arg.Any<EventId>(),
                                Arg.Is<object>(state => state.ToString()!
                                                             .Contains("No elements defined in SubmodelElementCollection ContactInformation")),
                                null,
                                Arg.Any<Func<object, Exception, string>>()!);
    }

    [Fact]
    public void Extract_EmptySubmodelElementList_LogsWarningAndReturnsNode()
    {
        var mlp = TestData.CreateSubmodelWithContactListWithOutElements();

        var node = _sut.Extract(mlp) as SemanticBranchNode;

        Equal("http://example.com/idta/digital-nameplate/semantic-id", node?.SemanticId);
        Single(node!.Children);
        var contactInformationNode = node.Children[0] as SemanticBranchNode;
        Equal("http://example.com/idta/digital-nameplate/contact-list", contactInformationNode?.SemanticId);
        Empty(contactInformationNode!.Children);
        _logger.Received(1).Log(LogLevel.Warning, Arg.Any<EventId>(),
        Arg.Is<object>(state => state.ToString()!
                                     .Contains("No elements defined in SubmodelElementList ContactList")),
        null,
        Arg.Any<Func<object, Exception, string>>()!);
    }

    [Fact]
    public void Extract_ReturnsSubmodelElement_WhenPathIsValid()
    {
        var submodel = TestData.CreateSubmodelWithoutExtraElements();
        const string Path = "ManufacturerName";
        var expected = TestData.CreateManufacturerName();

        var result = _sut.Extract(submodel, Path);

        Equal(GetSemanticId(expected), GetSemanticId(result));
    }

    [Fact]
    public void Extract_ReturnsSubmodelElement_WhenPathIsValidAndNested()
    {
        var submodel = TestData.CreateSubmodelWithoutExtraElementsNested();
        const string Path = "ContactInformation.ContactName";
        var expected = TestData.CreateContactName();

        var result = _sut.Extract(submodel, Path);

        Equal(GetSemanticId(expected), GetSemanticId(result));
    }

    [Fact]
    public void Extract_SubmodelWithoutSemanticId_ReturnsRootNodeWithEmptyId()
    {
        var submodel = Substitute.For<ISubmodel>();
        submodel.IdShort.Returns("nameplate");
        submodel.SemanticId.Returns((Reference)null!);
        submodel.SubmodelElements.Returns([]);

        var result = _sut.Extract(submodel);

        NotNull(result);
        Equal(string.Empty, result.SemanticId);
    }

    [Fact]
    public void Extract_ReturnsElementFromList_WhenPathContainsBracketIndex()
    {
        var submodel = TestData.CreateSubmodelWithModel3DList();
        const string Path = "Model3D[0].ModelFile";

        var result = _sut.Extract(submodel, Path);

        NotNull(result);
        Equal("ModelFile", result.IdShort);
        IsType<File>(result);
    }

    [Fact]
    public void Extract_ThrowNotFound_WhenElementFromList_WithBracketIndex_NotFound()
    {
        var submodel = TestData.CreateSubmodelWithModel3DList();
        const string Path = "Model3D[0].ModelFileID";

        Throws<InternalDataProcessingException>(() => _sut.Extract(submodel, Path));
    }

    [Fact]
    public void Extract_ThrowsNotFoundException_WhenElementIsNotAList()
    {
        const string Path = "ContactName[0]";

        var ex = Throws<InternalDataProcessingException>(() => _sut.Extract(TestData.CreateSubmodel(), Path));
        Contains("Internal Server Error.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ThrowsNotFoundException_WhenPathIsInvalid()
    {
        var submodel = TestData.CreateSubmodelWithModel3DList();
        const string Path = "Model3D[5].ModelFile1";

        var ex = Throws<InternalDataProcessingException>(() => _sut.Extract(submodel, Path));
        Contains("Internal Server Error.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ThrowsNotFoundException_WhenElementNotFound()
    {
        var submodel = TestData.CreateSubmodelWithoutExtraElements();
        const string Path = "NonExistent";

        Throws<InternalDataProcessingException>(() => _sut.Extract(submodel, Path));
    }

    [Theory]
    [InlineData("One", Cardinality.One)]
    [InlineData("ZeroToOne", Cardinality.ZeroToOne)]
    [InlineData("ZeroToMany", Cardinality.ZeroToMany)]
    [InlineData("OneToMany", Cardinality.OneToMany)]
    [InlineData("", Cardinality.Unknown)]
    public void GetCardinality_VariousQualifierValues_ReturnsExpected(string? qualifierValue, Cardinality expected)
    {
        var qualifier = Substitute.For<IQualifier>();
        qualifier.Value.Returns(qualifierValue);
        var element = Substitute.For<ISubmodelElement>();
        element.Qualifiers.Returns([qualifier]);

        var actual = (Cardinality)typeof(SemanticIdHandler)
            .GetMethod("GetCardinality", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [element])!;

        Equal(expected, actual);
    }

    [Fact]
    public void GetCardinality_QualifiersNull_ReturnsUnknown()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.Qualifiers.Returns((List<IQualifier>?)null);

        var actual = (Cardinality)typeof(SemanticIdHandler)
            .GetMethod("GetCardinality", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [element])!;

        Equal(Cardinality.Unknown, actual);
    }

    [Fact]
    public void GetCardinality_EmptyQualifiers_ReturnsUnknown()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.Qualifiers.Returns([]);

        var actual = (Cardinality)typeof(SemanticIdHandler)
            .GetMethod("GetCardinality", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [element])!;

        Equal(Cardinality.Unknown, actual);
    }

    [Theory]
    [InlineData(DataTypeDefXsd.DateTime, DataType.String)]
    [InlineData(DataTypeDefXsd.UnsignedShort, DataType.Integer)]
    [InlineData(DataTypeDefXsd.Double, DataType.Number)]
    [InlineData(DataTypeDefXsd.Boolean, DataType.Boolean)]
    [InlineData((DataTypeDefXsd)999, DataType.Unknown)]
    [InlineData(DataTypeDefXsd.AnyUri, DataType.String)]
    [InlineData(DataTypeDefXsd.Duration, DataType.String)]
    [InlineData(DataTypeDefXsd.NonNegativeInteger, DataType.Integer)]
    [InlineData(DataTypeDefXsd.GYearMonth, DataType.String)]
    [InlineData(DataTypeDefXsd.Float, DataType.Number)]
    [InlineData(DataTypeDefXsd.HexBinary, DataType.String)]
    [InlineData(DataTypeDefXsd.PositiveInteger, DataType.Integer)]
    [InlineData(DataTypeDefXsd.Decimal, DataType.Number)]
    public void GetValueType_PropertyValueType_ReturnsExpected(DataTypeDefXsd valueType, DataType expected)
    {
        var prop = new Property(
            idShort: "MyProp",
            valueType: valueType,
            value: "",
            semanticId: TestData.CreateContactName().SemanticId,
            qualifiers: []
        );

        var actual = (DataType)typeof(SemanticIdHandler)
            .GetMethod("GetValueType", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [prop])!;

        Equal(expected, actual);
    }

    [Fact]
    public void GetValueType_ElementWithoutValueProperty_ReturnsUnknown()
    {
        var element = Substitute.For<ISubmodelElement>();

        var actual = (DataType)typeof(SemanticIdHandler)
            .GetMethod("GetValueType", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [element])!;

        Equal(DataType.Unknown, actual);
    }

    [Theory]
    [InlineData("ContactList01", "http://example.com/idta/digital-nameplate/contact-list_aastwinengineindex_01")]
    [InlineData("ContactList42", "http://example.com/idta/digital-nameplate/contact-list_aastwinengineindex_42")]
    [InlineData("ContactList", "http://example.com/idta/digital-nameplate/contact-list")]
    public void Extract_SubmodelWithSingleElement_AppendsIndexCorrectly(string idShort, string expectedSemanticId)
    {
        var semanticKey = Substitute.For<IKey>();
        semanticKey.Value.Returns("http://example.com/idta/digital-nameplate/contact-list");
        var semanticReference = Substitute.For<IReference>();
        semanticReference.Keys.Returns([semanticKey]);
        var submodelElement = Substitute.For<ISubmodelElement>();
        submodelElement.IdShort.Returns(idShort);
        submodelElement.SemanticId.Returns(semanticReference);
        submodelElement.Qualifiers.Returns([]);
        var submodel = Substitute.For<ISubmodel>();
        submodel.IdShort.Returns("AnySubmodel");
        submodel.SemanticId.Returns(Substitute.For<IReference>());
        submodel.SubmodelElements.Returns([submodelElement]);

        var rootNode = (SemanticBranchNode)_sut.Extract(submodel);

        Single(rootNode.Children);
        var leaf = IsType<SemanticLeafNode>(rootNode.Children[0]);
        Equal(expectedSemanticId, leaf.SemanticId);
    }

    [Fact]
    public void FillOutTemplate_Submodel_ReturnsSubmodelWithValues()
    {
        var submodel = TestData.CreateSubmodel();
        var values = TestData.SubmodelTreeNode;

        var submodelWithNewValues = (Submodel)_sut.FillOutTemplate(submodel, values);

        var manufacturerName = submodelWithNewValues.SubmodelElements?[0] as MultiLanguageProperty;
        NotNull(manufacturerName);
        Equal("en", manufacturerName.Value?[0].Language);
        Equal("Test Example Manufacturer", manufacturerName.Value?[0].Text);
        Equal("de", manufacturerName.Value![1].Language);
        Equal("Test Beispiel Hersteller", manufacturerName.Value[1].Text);
        var modelType = submodelWithNewValues.SubmodelElements?[1] as Property;
        NotNull(modelType);
        Equal("22.47", modelType.Value);
        var contactList = submodelWithNewValues.SubmodelElements?[2] as SubmodelElementList;
        NotNull(contactList);
        Equal(2, contactList.Value!.Count);
        var contactName = contactList.Value[0] as Property;
        NotNull(contactName);
        Equal("Test John Doe", contactName.Value);
        var modelName = contactList.Value[1] as Property;
        NotNull(modelName);
        Equal("Test Example Model", modelName.Value);
        var contactInformation = submodelWithNewValues.SubmodelElements?[3] as SubmodelElementCollection;
        NotNull(contactInformation);
        _ = Single(contactInformation.Value!);
        var contactName2 = contactInformation.Value![0] as Property;
        NotNull(contactName2);
        Equal("Test John Doe", contactName2.Value);
        var file = submodelWithNewValues.SubmodelElements?[4] as File;
        NotNull(file);
        Equal("https://localhost/TestThumbnail", file.Value);

        var imagePath = Path.Combine(AppContext.BaseDirectory, "TestData", "Test.png");
        var originalBytes = System.IO.File.ReadAllBytes(imagePath);

        var blob = submodelWithNewValues.SubmodelElements?[5] as Blob;
        NotNull(blob);
        Equal(originalBytes, blob.Value);

        var range = submodelWithNewValues.SubmodelElements?[6] as AasCore.Aas3_0.Range;
        NotNull(range);
        Equal("10.02", range.Min);
        Equal("99.98", range.Max);

        var entityNode = submodelWithNewValues.SubmodelElements?[7] as Entity;
        NotNull(entityNode);
        _ = Single(entityNode.Statements!);
        var entityleafNode = entityNode.Statements![0] as Property;
        NotNull(entityleafNode);
        Equal("urn:uuid:123e4567-e89b-12d3-a456-426614174000", entityNode.GlobalAssetId);
        Equal("manufacturer_Value", entityNode.SpecificAssetIds![0].Value);
        Equal("serialnumber_Value", entityNode.SpecificAssetIds[1].Value);
    }

    [Fact]
    public void FillOutTemplate_SubmodelOfComplexData_ReturnsSubmodelWithValues()
    {
        var complexData = TestData.ComplexData;
        var submodel = TestData.CreateSubmodelWithComplexData();
        submodel.SubmodelElements?.Add(complexData);
        var values = TestData.CreateSubmodelWithComplexDataTreeNode();

        var submodelWithValues = (Submodel)_sut.FillOutTemplate(submodel, values);

        Equal(2, submodelWithValues.SubmodelElements!.Count);
        Equal("ComplexData0", submodelWithValues.SubmodelElements[0].IdShort);
        Equal("ComplexData1", submodelWithValues.SubmodelElements[1].IdShort);
        var complexData0 = GetSubmodelElementCollection(submodelWithValues, 0);
        var complexData1 = GetSubmodelElementCollection(submodelWithValues, 1);
        AssertMultiLanguageProperty(complexData0, "Test Example Manufacturer", "Test Beispiel Hersteller");
        AssertMultiLanguageProperty(complexData1, "Test1 Example Manufacturer", "Test1 Beispiel Hersteller");
        AssertModelType(complexData0, 1, "22.47");
        AssertModelType(complexData1, 1, "22.47");
        AssertContactList(complexData0, 2, "Test John Doe", "Test Example Model");
        AssertContactList(complexData1, 3, "Test1 John Doe", "Test1 Example Model");
        AssertContactList(complexData1, 4, "Test2 John Doe", "Test2 Example Model");
        AssertContactInfo(complexData0, 3, "Test John Doe");
        AssertContactInfo(complexData1, 2, "Test1 John Doe");
    }

    [Fact]
    public void FillOutTemplate_SubmodelOfComplexData_ReturnsSubmodelWithValue()
    {
        var complexData = TestData.ComplexData;
        var submodel = TestData.CreateSubmodelWithComplexData();
        submodel.SubmodelElements?.Add(complexData);
        var values = TestData.CreateSubmodelWithInValidComplexDataTreeNode();

        var submodelWithValues = (Submodel)_sut.FillOutTemplate(submodel, values);

        Equal(2, submodelWithValues.SubmodelElements!.Count);
        Equal("ComplexData0", submodelWithValues.SubmodelElements[0].IdShort);
        Equal("ComplexData1", submodelWithValues.SubmodelElements[1].IdShort);
        var complexData0 = GetSubmodelElementCollection(submodelWithValues, 0);
        var complexData1 = GetSubmodelElementCollection(submodelWithValues, 1);
        Equal(3, complexData1.Value!.Count);
        AssertMultiLanguageProperty(complexData0, "Test Example Manufacturer", "Test Beispiel Hersteller");
        AssertMultiLanguageProperty(complexData1, "Test1 Example Manufacturer", "Test1 Beispiel Hersteller");
        AssertModelType(complexData0, 1, "22.47");
        AssertModelType(complexData1, 1, "22.47");
        AssertContactList(complexData0, 2, "Test John Doe", "Test Example Model");
        AssertContactInfo(complexData0, 3, "Test John Doe");
        AssertContactInfo(complexData1, 2, "Test1 John Doe");
    }

    [Fact]
    public void FillOutTemplate_ShouldNotChangeAnyThing_WhenReferenceElementHasNullValue()
    {
        var value = TestData.CreateReferenceElementWithEmptyValues();
        var submodel = CreateSubmodelWithSubmodelElement(value);
        var semanticTree = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.One);
        semanticTree.AddChild(new SemanticLeafNode("http://example.com/idta/digital-nameplate/reference-element/empty-values", "test", DataType.String,
                                                   Cardinality.Unknown));
        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result!.SubmodelElements!);
        IsType<ReferenceElement>(result!.SubmodelElements![0]);
        var referenceElement = result.SubmodelElements[0] as ReferenceElement;
        Equal(TestData.CreateReferenceElementWithEmptyValues().ToString(), referenceElement!.ToString());
    }

    [Fact]
    public void FillOutTemplate_ShouldNotChange_ExternalReferenceElement_InReferenceElement()
    {
        var value = TestData.CreateReferenceElementWithExternalReference();
        var submodel = CreateSubmodelWithSubmodelElement(value);
        var semanticTree = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.One);
        semanticTree.AddChild(new SemanticLeafNode("http://example.com/idta/digital-nameplate/reference-element/external-reference", "test", DataType.String,
                                                   Cardinality.Unknown));
        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result!.SubmodelElements!);
        IsType<ReferenceElement>(result!.SubmodelElements![0]);
        var referenceElement = result.SubmodelElements[0] as ReferenceElement;
        Equal(TestData.CreateFilledReferenceElementWithExternalReference().ToString(), referenceElement!.ToString());
        NotEqual("test", referenceElement!.ToString());
    }

    [Fact]
    public void FillOutTemplate_WhereModelReference_AndEachLeafNodeValueIsPresent_InReferenceElement()
    {
        var value = TestData.CreateReferenceElementWithModelReference();
        var submodel = CreateSubmodelWithSubmodelElement(value);
        var semanticTree = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.One);
        semanticTree.AddChild(TestData.CreateReferenceElementTreeNodeWhereEachValueOfLeafIsPresent());

        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result!.SubmodelElements!);
        IsType<ReferenceElement>(result!.SubmodelElements![0]);
        var referenceElement = result.SubmodelElements[0] as ReferenceElement;
        Equal(TestData.CreateFilledReferenceElementWithModelReference()!.Value!.Keys!.Count!, referenceElement!.Value!.Keys!.Count);
        var expectedKeys = TestData.CreateFilledReferenceElementWithModelReference()!.Value!.Keys!;
        for (var i = 0; i < expectedKeys.Count - 1; i++)
        {
            var expectedKey = expectedKeys[i];
            var actualKey = referenceElement.Value.Keys[i];
            NotNull(actualKey);
            Equal(expectedKey.Value, actualKey!.Value);
        }
    }

    [Fact]
    public void FillOutTemplate_WhereModelReference_AndEachLeafNodeValueIsNotPresent_InReferenceElement_TakesValueFormTemplate()
    {
        var value = TestData.CreateReferenceElementWithModelReference();
        var submodel = CreateSubmodelWithSubmodelElement(value);
        var semanticTree = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.One);
        semanticTree.AddChild(TestData.CreateReferenceElementTreeNodeWhereEachValueOfLeafIsNotPresent());

        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result!.SubmodelElements!);
        IsType<ReferenceElement>(result!.SubmodelElements![0]);
        var referenceElement = result.SubmodelElements[0] as ReferenceElement;
        Equal(TestData.CreateFilledReferenceElementWithModelReferenceWithTemplateValueForProperty()!.Value!.Keys!.Count!, referenceElement!.Value!.Keys!.Count);
        var expectedKeys = TestData.CreateFilledReferenceElementWithModelReference()!.Value!.Keys!;
        for (var i = 0; i < expectedKeys.Count - 1; i++)
        {
            var expectedKey = expectedKeys[i];
            var actualKey = referenceElement.Value.Keys[i];
            NotNull(actualKey);
            Equal(expectedKey.Value, actualKey!.Value);
        }
    }

    [Fact]
    public void FillOutTemplate_ShouldNotChange_RelationShipElementWhenExternalReferenceForBoth()
    {
        var value = TestData.CreateRelationshipElementWithBothExternalReference();
        var submodel = CreateSubmodelWithSubmodelElement(value);
        var semanticTree = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.One);
        semanticTree.AddChild(new SemanticBranchNode("http://example.com/idta/digital-nameplate/relationship-element/both-external-reference", Cardinality.Unknown));

        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result!.SubmodelElements!);
        IsType<RelationshipElement>(result!.SubmodelElements![0]);
        var relationShipElement = result.SubmodelElements[0] as RelationshipElement;
        Equal(TestData.CreateRelationshipElementWithBothExternalReference().ToJson(), relationShipElement!.ToJson());
    }

    [Fact]
    public void FillOutTemplate_RelationShipElementHaveOneModelReference_ProvidesAllLeafNodeValue()
    {
        var value = TestData.CreateRelationshipElementWithOneExternalReferenceAndOneModelReference();
        var submodel = CreateSubmodelWithSubmodelElement(value);
        var semanticTree = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.One);
        semanticTree.AddChild(TestData.CreateRelationShipElementHaveOneModelReferenceWhereEachValueOfLeafIsPresent());

        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result!.SubmodelElements!);
        IsType<RelationshipElement>(result!.SubmodelElements![0]);
        var relationShipElement = result.SubmodelElements[0] as RelationshipElement;
        Equal(TestData.CreateFilledRelationshipElementWithOneExternalReferenceAndOneModelReference().ToJson(), relationShipElement!.ToJson());
    }

    [Fact]
    public void FillOutTemplate_RelationShipElementBothModelReference_HandlesMissingLeafNodeValuesAsWell()
    {
        var value = TestData.CreateRelationshipElementWithBothModelReference();
        var submodel = CreateSubmodelWithSubmodelElement(value);
        var semanticTree = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.One);
        semanticTree.AddChild(TestData.CreateRelationshipElementWithBothModelReferenceWhereEachValueOfLeafIsNotPresent());

        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result!.SubmodelElements!);
        IsType<RelationshipElement>(result!.SubmodelElements![0]);
        var relationShipElement = result.SubmodelElements[0] as RelationshipElement;
        Equal(TestData.CreateFilledRelationshipElementWithBothModelReference().ToJson(), relationShipElement!.ToJson());
    }

    [Fact]
    public void FillOutTemplate_ShouldPreserveElement_WhenListValueIsNull()
    {
        var listWithNullValue = TestData.CreateModel3DListWithoutValues();
        var submodel = CreateSubmodelWithSubmodelElement(listWithNullValue);
        var semanticTree = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.One);
        semanticTree.AddChild(TestData.CreateModel3DTreeNode());

        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result!.SubmodelElements!);
        IsType<SubmodelElementList>(result!.SubmodelElements![0]);
        var list = result.SubmodelElements[0] as SubmodelElementList;
        Null(list?.Value);
    }

    [Fact]
    public void FillOutTemplate_ShouldPreserveElement_WhenCollectionIsNull()
    {
        var submodel = TestData.CreateSubmodelWithContactInformationWithOutElements();

        var semanticTree = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.Unknown);
        semanticTree.AddChild(new SemanticBranchNode("http://example.com/idta/digital-nameplate/contact-information", Cardinality.Unknown));

        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result.SubmodelElements!);
        IsType<SubmodelElementCollection>(result.SubmodelElements![0]);
        var collection = result.SubmodelElements[0] as SubmodelElementCollection;
        Null(collection?.Value);
    }

    [Fact]
    public void FillOutTemplate_ThrowsArgumentException_WhenElementTypeIsUnsupported()
    {
        var unsupportedElement = new Operation
        {
            IdShort = "Unsupported",
            SemanticId = new Reference(
                                       ReferenceTypes.ExternalReference,
                                       [
                                           new Key(KeyTypes.Property, "http://example.com/idta/digital-nameplate/unsupported")
                                       ])
        };
        var submodel = CreateSubmodelWithSubmodelElement(unsupportedElement);

        var semanticTree = new SemanticBranchNode("http://example.com/idta/digital-nameplate/semantic-id", Cardinality.One);
        semanticTree.AddChild(new SemanticLeafNode("http://example.com/idta/digital-nameplate/unsupported", "SomeValue", DataType.String, Cardinality.One));

        var ex = Throws<InternalDataProcessingException>(() => _sut.FillOutTemplate(submodel, semanticTree));
        Equal("Internal Server Error.", ex.Message);
    }

    [Fact]
    public void FillOutTemplate_MultiLanguageProperty_WithDefaultLanguagesAsNull_PopulatesOnlyTemplateLanguages()
    {
        var submodel = TestData.CreateSubmodelWithManufacturerNameWithTwoLanguagesInTemplate();
        var semanticTree = TestData.CreateSubmodelWithManufacturerName();

        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result.SubmodelElements!);
        var mlp = result.SubmodelElements![0] as MultiLanguageProperty;
        NotNull(mlp);
        Equal(2, mlp.Value!.Count);
        var enValue = mlp.Value.FirstOrDefault(v => v.Language == "en");
        NotNull(enValue);
        Equal("Test Example Manufacturer", enValue.Text);
        var deValue = mlp.Value.FirstOrDefault(v => v.Language == "de");
        NotNull(deValue);
        Equal("Test Beispiel Hersteller", deValue.Text);
    }

    [Fact]
    public void FillOutTemplate_EmptyMultiLanguageProperty_WithDefaultLanguagesAs_En_De_Fr_AddsAllLanguages()
    {
        var mlpSettings = CreateMlpSettings(["de", "en", "fr"]);
        var sut = new SemanticIdHandler(_logger, _semantics, mlpSettings);
        var submodel = TestData.CreateSubmodelWithManufacturerNameWithOutElements();
        var semanticTree = TestData.CreateSubmodelWithManufacturerName();

        var result = sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result.SubmodelElements!);
        var mlp = result.SubmodelElements![0] as MultiLanguageProperty;
        NotNull(mlp);
        Equal(3, mlp.Value!.Count);
        var languages = mlp.Value.Select(v => v.Language).OrderBy(l => l).ToList();
        Equal(["de", "en", "fr"], languages);
        _logger.Received(3).Log(
                                LogLevel.Information,
                                Arg.Any<EventId>(),
                                Arg.Is<object>(state => state.ToString()!.Contains("Added language")),
                                null,
                                Arg.Any<Func<object, Exception?, string>>()!
                               );
    }

    [Fact]
    public void FillOutTemplate_MultiLanguageProperty_WithDefaultLanguagesAs_En_De_Fr_MergesWithTemplateLanguages()
    {
        var mlpSettings = CreateMlpSettings(["de", "en", "fr"]);
        var sut = new SemanticIdHandler(_logger, _semantics, mlpSettings);
        var submodel = TestData.CreateSubmodelWithManufacturerNameWithTwoLanguagesInTemplate();
        var semanticTree = TestData.CreateSubmodelWithManufacturerName();

        var result = sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result.SubmodelElements!);
        var mlp = result.SubmodelElements![0] as MultiLanguageProperty;
        NotNull(mlp);
        Equal(3, mlp.Value!.Count);
        var enValue = mlp.Value.FirstOrDefault(v => v.Language == "en");
        NotNull(enValue);
        Equal("Test Example Manufacturer", enValue.Text);
        var deValue = mlp.Value.FirstOrDefault(v => v.Language == "de");
        NotNull(deValue);
        Equal("Test Beispiel Hersteller", deValue.Text);
        var frValue = mlp.Value.FirstOrDefault(v => v.Language == "fr");
        NotNull(frValue);
        Equal("Exemple de test Fabricant", frValue.Text);
        _logger.Received(1).Log(
                                LogLevel.Information,
                                Arg.Any<EventId>(),
                                Arg.Is<object>(state => state.ToString()!.Contains("Added language 'fr'")),
                                null,
                                Arg.Any<Func<object, Exception?, string>>()!
                               );
    }

    [Fact]
    public void FillOutTemplate_MultiLanguageProperty_WithNullValue_InitializesWithoutLanguagesWhenDefaultsAreNull()
    {
        var submodel = TestData.CreateSubmodelWithManufacturerNameWithOutElements();
        var semanticTree = TestData.CreateSubmodelWithManufacturerName();

        var result = _sut.FillOutTemplate(submodel, semanticTree);

        NotNull(result);
        Single(result.SubmodelElements!);
        var mlp = result.SubmodelElements![0] as MultiLanguageProperty;
        NotNull(mlp);
        NotNull(mlp.Value);
        Empty(mlp.Value);
    }

    private static SubmodelElementCollection GetSubmodelElementCollection(Submodel submodel, int index) => (submodel.SubmodelElements?[index] as SubmodelElementCollection)!;

    private static void AssertMultiLanguageProperty(
        SubmodelElementCollection complexData, string expectedTextEn, string expectedTextDe)
    {
        var manufacturerName = complexData.Value![0] as MultiLanguageProperty;

        NotNull(manufacturerName);
        Equal("en", manufacturerName.Value![0].Language);
        Equal(expectedTextEn, manufacturerName.Value[0].Text);
        Equal("de", manufacturerName.Value[1].Language);
        Equal(expectedTextDe, manufacturerName.Value[1].Text);
    }

    private static void AssertModelType(SubmodelElementCollection complexData, int elementIndex, string expectedValue)
    {
        var modelType = complexData.Value![elementIndex] as Property;
        NotNull(modelType);
        Equal(expectedValue, modelType.Value);
    }

    private static void AssertContactInfo(SubmodelElementCollection complexData, int elementIndex, string expectedName)
    {
        var contactInfoCollection = complexData.Value![elementIndex] as SubmodelElementCollection;
        NotNull(contactInfoCollection);
        _ = Single(contactInfoCollection.Value!);

        var contactNameProperty = contactInfoCollection.Value![0] as Property;
        NotNull(contactNameProperty);
        Equal(expectedName, contactNameProperty.Value);
    }

    private static void AssertContactList(
        SubmodelElementCollection complexData,
        int elementIndex,
        string expectedContactName,
        string expectedModelName)
    {
        var contactList = complexData.Value![elementIndex] as SubmodelElementList;
        NotNull(contactList);
        Equal(2, contactList.Value!.Count);

        var contactName = contactList.Value[0] as Property;
        NotNull(contactName);
        Equal(expectedContactName, contactName.Value);

        var modelName = contactList.Value[1] as Property;
        NotNull(modelName);
        Equal(expectedModelName, modelName.Value);
    }

    private static Submodel CreateSubmodelWithSubmodelElement(ISubmodelElement submodelElement)
    {
        return new Submodel(
                            id: "http://example.com/idta/digital-nameplate",
                            idShort: "DigitalNameplate",
                            semanticId: new Reference(
                                                      ReferenceTypes.ExternalReference,
                                                      [
                                                          new Key(KeyTypes.Submodel, "http://example.com/idta/digital-nameplate/semantic-id")
                                                      ]),
                            submodelElements: [submodelElement]
                           );
    }

    private static string GetSemanticId(IHasSemantics hasSemantics) => hasSemantics.SemanticId?.Keys?.FirstOrDefault()?.Value ?? string.Empty;

    private static IOptions<MultiLanguagePropertySettings> CreateMlpSettings(List<string>? defaultLanguages)
    {
        var settings = new MultiLanguagePropertySettings
        {
            DefaultLanguages = defaultLanguages
        };
        return Options.Create(settings);
    }
}
