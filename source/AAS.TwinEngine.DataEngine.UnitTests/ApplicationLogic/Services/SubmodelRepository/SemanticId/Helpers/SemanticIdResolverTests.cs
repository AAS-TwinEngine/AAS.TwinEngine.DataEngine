using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_0;

using Microsoft.Extensions.Options;

using NSubstitute;

using static Xunit.Assert;

using File = AasCore.Aas3_0.File;
using Range = AasCore.Aas3_0.Range;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public class SemanticIdResolverTests
{
    private readonly SemanticIdResolver _sut;
    private readonly IOptions<PluginsConfig> _pluginsConfig;
    private readonly IOptions<TemplateManagementConfig> _templateManagementConfig;

    public SemanticIdResolverTests()
    {
        _pluginsConfig = Options.Create(new PluginsConfig
        {
            MultiLanguageProperty = new PluginMultiLanguagePropertyConfig { SemanticPostfixSeparator = "_" },
            SubmodelElementIndexContextPrefix = "_aastwinengineindex_"
        });
        _templateManagementConfig = Options.Create(new TemplateManagementConfig
        {
            Semantics = new TemplateSemanticsConfig { InternalSemanticId = "InternalSemanticId" }
        });
        _sut = new SemanticIdResolver(_pluginsConfig, _templateManagementConfig);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsException()
    {
        var options = Options.Create<PluginsConfig>(null!);
        var tmConfig = Options.Create(new TemplateManagementConfig());

        _ = Throws<NullReferenceException>(() => new SemanticIdResolver(options, tmConfig));
    }

    [Fact]
    public void GetSemanticId_WithSemanticId_ReturnsValue()
    {
        var element = CreateElementWithSemanticId("http://example.com/semantic-id");

        var result = _sut.GetSemanticId(element);

        Equal("http://example.com/semantic-id", result);
    }

    [Fact]
    public void GetSemanticId_WithNullSemanticId_ReturnsEmpty()
    {
        var element = Substitute.For<IHasSemantics>();
        element.SemanticId.Returns((Reference)null!);

        var result = _sut.GetSemanticId(element);

        Equal(string.Empty, result);
    }

    [Fact]
    public void GetSemanticId_WithEmptyKeys_ReturnsEmpty()
    {
        var reference = Substitute.For<IReference>();
        reference.Keys.Returns(new List<IKey>());
        var element = Substitute.For<IHasSemantics>();
        element.SemanticId.Returns(reference);

        var result = _sut.GetSemanticId(element);

        Equal(string.Empty, result);
    }

    [Fact]
    public void ExtractSemanticId_WithInternalSemanticIdQualifier_ReturnsQualifierValue()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.SemanticId.Returns(CreateReference("http://original-semantic-id"));
        var qualifier = Substitute.For<IQualifier>();
        qualifier.Type.Returns("InternalSemanticId");
        qualifier.Value.Returns("http://internal-semantic-id");
        element.Qualifiers.Returns(new List<IQualifier> { qualifier });

        var result = _sut.ExtractSemanticId(element);

        Equal("http://internal-semantic-id", result);
    }

    [Fact]
    public void ExtractSemanticId_WithoutInternalSemanticIdQualifier_ReturnsSemantId()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.SemanticId.Returns(CreateReference("http://original-semantic-id"));
        var qualifier = Substitute.For<IQualifier>();
        qualifier.Type.Returns("SomeOtherQualifier");
        qualifier.Value.Returns("http://other-value");
        element.Qualifiers.Returns(new List<IQualifier> { qualifier });

        var result = _sut.ExtractSemanticId(element);

        Equal("http://original-semantic-id", result);
    }

    [Fact]
    public void ExtractSemanticId_WithNullQualifiers_ReturnsSemanticId()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.SemanticId.Returns(CreateReference("http://original-semantic-id"));
        element.Qualifiers.Returns((List<IQualifier>?)null);

        var result = _sut.ExtractSemanticId(element);

        Equal("http://original-semantic-id", result);
    }

    [Fact]
    public void ResolveSemanticId_WithoutIndex_ReturnsBaseSemanticId()
    {
        var element = CreateElementWithSemanticId("http://example.com/semantic-id");

        var result = _sut.ResolveSemanticId(element, "MyElement");

        Equal("http://example.com/semantic-id", result);
    }

    [Fact]
    public void ResolveSemanticId_WithTrailingIndex_AppendsIndex()
    {
        var element = CreateElementWithSemanticId("http://example.com/semantic-id");

        var result = _sut.ResolveSemanticId(element, "MyElement42");

        Equal("http://example.com/semantic-id_aastwinengineindex_42", result);
    }

    [Fact]
    public void ResolveElementSemanticId_WithoutIndex_ReturnsBaseSemanticId()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.SemanticId.Returns(CreateReference("http://example.com/semantic-id"));
        element.Qualifiers.Returns(new List<IQualifier>());

        var result = _sut.ResolveElementSemanticId(element, "ContactList");

        Equal("http://example.com/semantic-id", result);
    }

    [Fact]
    public void ResolveElementSemanticId_WithTrailingDigits_AppendsIndex()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.SemanticId.Returns(CreateReference("http://example.com/semantic-id"));
        element.Qualifiers.Returns(new List<IQualifier>());

        var result = _sut.ResolveElementSemanticId(element, "ContactList01");

        Equal("http://example.com/semantic-id_aastwinengineindex_01", result);
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
        element.Qualifiers.Returns(new List<IQualifier> { qualifier });

        var actual = _sut.GetCardinality(element);

        Equal(expected, actual);
    }

    [Fact]
    public void GetCardinality_QualifiersNull_ReturnsUnknown()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.Qualifiers.Returns((List<IQualifier>?)null);

        var actual = _sut.GetCardinality(element);

        Equal(Cardinality.Unknown, actual);
    }

    [Fact]
    public void GetCardinality_EmptyQualifiers_ReturnsUnknown()
    {
        var element = Substitute.For<ISubmodelElement>();
        element.Qualifiers.Returns(new List<IQualifier>());

        var actual = _sut.GetCardinality(element);

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
            semanticId: new Reference(ReferenceTypes.ExternalReference,
                [new Key(KeyTypes.Property, "http://example.com/test")]),
            qualifiers: []
        );

        var actual = _sut.GetValueType(prop);

        Equal(expected, actual);
    }

    [Fact]
    public void GetValueType_RangeElement_ReturnsExpectedType()
    {
        var range = new Range(valueType: DataTypeDefXsd.Double, idShort: "TestRange");

        var actual = _sut.GetValueType(range);

        Equal(DataType.Number, actual);
    }

    [Fact]
    public void GetValueType_FileElement_ReturnsString()
    {
        var file = new File(contentType: "image/png", idShort: "TestFile");

        var actual = _sut.GetValueType(file);

        Equal(DataType.String, actual);
    }

    [Fact]
    public void GetValueType_BlobElement_ReturnsString()
    {
        var blob = new Blob(contentType: "application/octet-stream", idShort: "TestBlob");

        var actual = _sut.GetValueType(blob);

        Equal(DataType.String, actual);
    }

    [Fact]
    public void GetValueType_UnsupportedElement_ReturnsUnknown()
    {
        var element = Substitute.For<ISubmodelElement>();

        var actual = _sut.GetValueType(element);

        Equal(DataType.Unknown, actual);
    }

    [Fact]
    public void BuildReferenceKeySemanticId_SingleKey_OmitsIndex()
    {
        var result = _sut.BuildReferenceKeySemanticId("http://base", KeyTypes.Submodel, 0, 1);

        Equal("http://base_Submodel", result);
    }

    [Fact]
    public void BuildReferenceKeySemanticId_MultipleKeys_IncludesIndex()
    {
        var result = _sut.BuildReferenceKeySemanticId("http://base", KeyTypes.SubmodelElementCollection, 1, 3);

        Equal("http://base_SubmodelElementCollection_1", result);
    }

    [Fact]
    public void MlpPostFixSeparator_ReturnsConfiguredValue()
    {
        Equal("_", _sut.MlpPostFixSeparator);
    }

    private static IHasSemantics CreateElementWithSemanticId(string semanticId)
    {
        var element = Substitute.For<IHasSemantics>();
        element.SemanticId.Returns(CreateReference(semanticId));
        return element;
    }

    private static Reference CreateReference(string value) =>
        new(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, value)]);
}
