using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using AasCore.Aas3_1;

using File = AasCore.Aas3_1.File;
using Range = AasCore.Aas3_1.Range;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Shared.ContentModifiers;

public class ContentSerializerElementTypeTests
{
    private static Reference CreateReference(string value) => new(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, value)]);

    private static void AssertJson(string expectedJson, JsonNode? actual)
        => Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expectedJson), actual), actual?.ToJsonString());

    [Fact]
    public void SerializeSubmodelElement_WithValueOnBlob_ReturnsContentTypeAndBase64Value()
    {
        var blob = new Blob(contentType: "application/pdf", idShort: "B", value: [1, 2, 3]);

        AssertJson("""{ "contentType": "application/pdf", "value": "AQID" }""", ContentSerializer.SerializeSubmodelElement(blob, Content.Value, "B"));
    }

    [Fact]
    public void SerializeSubmodelElement_WithValueOnRange_ReturnsTypedMinAndMax()
    {
        var range = new Range(DataTypeDefXsd.Int, idShort: "R", min: "10", max: "20");

        AssertJson("""{ "min": 10, "max": 20 }""", ContentSerializer.SerializeSubmodelElement(range, Content.Value, "R"));
    }

    [Fact]
    public void SerializeSubmodelElement_WithValueOnReferenceElement_ReturnsTheReference()
    {
        var element = new ReferenceElement(idShort: "Ref", value: CreateReference("urn:x"));

        AssertJson("""{ "type": "ExternalReference", "keys": [ { "type": "GlobalReference", "value": "urn:x" } ] }""", ContentSerializer.SerializeSubmodelElement(element, Content.Value, "Ref"));
    }

    [Fact]
    public void SerializeSubmodelElement_WithValueOnRelationshipElement_ReturnsFirstAndSecond()
    {
        var element = new RelationshipElement(first: CreateReference("urn:a"), second: CreateReference("urn:b"), idShort: "Rel");

        var result = ContentSerializer.SerializeSubmodelElement(element, Content.Value, "Rel")!.AsObject();

        Assert.Equal("urn:a", result["first"]!["keys"]![0]!["value"]!.GetValue<string>());
        Assert.Equal("urn:b", result["second"]!["keys"]![0]!["value"]!.GetValue<string>());
        Assert.False(result.ContainsKey("annotations"));
    }

    [Fact]
    public void SerializeSubmodelElement_WithValueOnAnnotatedRelationshipElement_ReturnsAnnotationsKeyedByIdShort()
    {
        var element = new AnnotatedRelationshipElement(
            first: CreateReference("urn:a"),
            second: CreateReference("urn:b"),
            idShort: "Rel",
            annotations: [new Property(DataTypeDefXsd.String, idShort: "Note", value: "n")]);

        var result = ContentSerializer.SerializeSubmodelElement(element, Content.Value, "Rel")!.AsObject();

        AssertJson("""{ "Note": "n" }""", result["annotations"]);
    }

    [Fact]
    public void SerializeSubmodelElement_WithValueOnBasicEventElement_ReturnsObservedReference()
    {
        var element = new BasicEventElement(observed: CreateReference("urn:observed"), direction: Direction.Input, state: StateOfEvent.On, idShort: "Event");

        var result = ContentSerializer.SerializeSubmodelElement(element, Content.Value, "Event")!.AsObject();

        Assert.Equal("urn:observed", result["observed"]!["keys"]![0]!["value"]!.GetValue<string>());
    }

    [Fact]
    public void SerializeSubmodelElement_WithValueOnEntity_ReturnsStatementsEntityTypeAndGlobalAssetId()
    {
        var entity = new Entity(
            entityType: EntityType.SelfManagedEntity,
            idShort: "E",
            statements: [new Property(DataTypeDefXsd.String, idShort: "S", value: "x")],
            globalAssetId: "urn:asset");

        AssertJson(
            """{ "statements": { "S": "x" }, "entityType": "SelfManagedEntity", "globalAssetId": "urn:asset" }""",
            ContentSerializer.SerializeSubmodelElement(entity, Content.Value, "E"));
    }

    [Theory]
    [InlineData(DataTypeDefXsd.Boolean, "false", "false")]
    [InlineData(DataTypeDefXsd.Boolean, "0", "false")]
    [InlineData(DataTypeDefXsd.Int, "+7", "7")]
    [InlineData(DataTypeDefXsd.Int, "-3", "-3")]
    [InlineData(DataTypeDefXsd.Double, "1.5", "1.5")]
    [InlineData(DataTypeDefXsd.Double, "2.5e3", "2.5e3")]
    [InlineData(DataTypeDefXsd.Int, "abc", "\"abc\"")]
    [InlineData(DataTypeDefXsd.Double, "INF", "\"INF\"")]
    [InlineData(DataTypeDefXsd.Date, "2022-01-01", "\"2022-01-01\"")]
    [InlineData(DataTypeDefXsd.AnyUri, "https://example.com", "\"https://example.com\"")]
    [InlineData(DataTypeDefXsd.String, "42", "\"42\"")]
    public void SerializeSubmodelElement_WithValueOnProperty_MapsXsdTypeToJsonType(DataTypeDefXsd valueType, string value, string expectedJson)
    {
        var property = new Property(valueType, idShort: "P", value: value);

        var result = ContentSerializer.SerializeSubmodelElement(property, Content.Value, "P");

        Assert.Equal(JsonNode.Parse(expectedJson)!.GetValueKind(), result!.GetValueKind());
        Assert.Equal(JsonNode.Parse(expectedJson)!.ToJsonString(), result.ToJsonString());
    }

    [Fact]
    public void SerializeSubmodel_WithValue_OmitsElementsWithoutValueAndOperationsButKeepsEmptyCollectionsAndListItems()
    {
        var submodel = new Submodel(
            "sm",
            submodelElements:
            [
                new Property(DataTypeDefXsd.String, idShort: "NoValue"),
                new MultiLanguageProperty(idShort: "NoText"),
                new Range(DataTypeDefXsd.Int, idShort: "NoRange"),
                new Operation(idShort: "Op"),
                new Capability(idShort: "Cap"),
                new SubmodelElementCollection(idShort: "EmptyCollection"),
                new SubmodelElementList(
                    AasSubmodelElements.MultiLanguageProperty,
                    idShort: "List",
                    value: [new MultiLanguageProperty()])
            ]);

        var result = ContentSerializer.SerializeSubmodel(submodel, Content.Value);

        AssertJson("""{ "EmptyCollection": {}, "List": [ [] ] }""", result);
    }

    [Fact]
    public void SerializeSubmodel_WithValue_SortsLanguagesAndMembersDeterministically()
    {
        var submodel = new Submodel(
            "sm",
            submodelElements:
            [
                new Property(DataTypeDefXsd.String, idShort: "b", value: "1"),
                new Property(DataTypeDefXsd.String, idShort: "B", value: "2"),
                new Property(DataTypeDefXsd.String, idShort: "a", value: "3"),
                new MultiLanguageProperty(idShort: "Name", value: [new LangStringTextType("en", "Hello"), new LangStringTextType("de", "Hallo")])
            ]);

        var result = ContentSerializer.SerializeSubmodel(submodel, Content.Value).AsObject();

        Assert.Equal(["B", "Name", "a", "b"], result.Select(p => p.Key));
        Assert.Equal("de", result["Name"]![0]!.AsObject().Single().Key);
    }

    [Fact]
    public void SerializeSubmodelElement_WithMetadataOnEntity_RemovesStatementsAndAssetIdsButKeepsEntityType()
    {
        var entity = new Entity(entityType: EntityType.SelfManagedEntity, idShort: "E", statements: [new Property(DataTypeDefXsd.String, idShort: "S")], globalAssetId: "urn:asset");

        var result = ContentSerializer.SerializeSubmodelElement(entity, Content.Metadata, "E")!.AsObject();

        Assert.False(result.ContainsKey("statements"));
        Assert.False(result.ContainsKey("globalAssetId"));
        Assert.Equal("SelfManagedEntity", result["entityType"]!.GetValue<string>());
    }

    [Fact]
    public void SerializeSubmodelElement_WithMetadataOnFile_RemovesValueAndContentType()
    {
        var file = new File(contentType: "image/png", idShort: "F", value: "https://example.com/a.png");

        var result = ContentSerializer.SerializeSubmodelElement(file, Content.Metadata, "F")!.AsObject();

        Assert.False(result.ContainsKey("value"));
        Assert.False(result.ContainsKey("contentType"));
        Assert.Equal("File", result["modelType"]!.GetValue<string>());
    }

    [Fact]
    public void SerializeSubmodelElement_WithMetadataOnOperation_KeepsAllFields()
    {
        var operation = new Operation(idShort: "Op", category: "PARAMETER");

        var result = ContentSerializer.SerializeSubmodelElement(operation, Content.Metadata, "Op")!.AsObject();

        Assert.Equal("PARAMETER", result["category"]!.GetValue<string>());
    }

    [Fact]
    public void SerializeSubmodelElement_WithPathOnEntity_IncludesStatements()
    {
        var entity = new Entity(
            entityType: EntityType.CoManagedEntity,
            idShort: "E",
            statements: [new Property(DataTypeDefXsd.String, idShort: "S2"), new Property(DataTypeDefXsd.String, idShort: "S1")]);

        var result = ContentSerializer.SerializeSubmodelElement(entity, Content.Path, "E");

        Assert.Equal(["E", "E.S1", "E.S2"], result!.AsArray().Select(p => p!.GetValue<string>()));
    }

    [Fact]
    public void SerializeSubmodelElement_WithPathOnList_UsesIndexNotationAndKeepsItemOrder()
    {
        var list = new SubmodelElementList(
            AasSubmodelElements.SubmodelElementCollection,
            idShort: "L",
            value:
            [
                new SubmodelElementCollection(value: [new Property(DataTypeDefXsd.String, idShort: "Z")]),
                new SubmodelElementCollection(value: [new Property(DataTypeDefXsd.String, idShort: "A")])
            ]);

        var result = ContentSerializer.SerializeSubmodelElement(list, Content.Path, "L");

        Assert.Equal(["L", "L[0]", "L[0].Z", "L[1]", "L[1].A"], result!.AsArray().Select(p => p!.GetValue<string>()));
    }

    [Fact]
    public void SerializeSubmodelElements_WithMetadata_KeepsInputOrder()
    {
        List<ISubmodelElement> elements = [new Property(DataTypeDefXsd.String, idShort: "B"), new Property(DataTypeDefXsd.String, idShort: "A")];

        var result = ContentSerializer.SerializeSubmodelElements(elements, Content.Metadata, "sm").AsArray();

        Assert.Equal(["B", "A"], result.Select(e => e!["idShort"]!.GetValue<string>()));
    }

    [Fact]
    public void SerializeShellReference_ReturnsModelReferenceWithShellKey()
    {
        var shell = new AssetAdministrationShell("aas-1", new AssetInformation(AssetKind.Instance));

        AssertJson("""{ "type": "ModelReference", "keys": [ { "type": "AssetAdministrationShell", "value": "aas-1" } ] }""", ContentSerializer.SerializeShellReference(shell));
    }

    [Theory]
    [MemberData(nameof(KeyTypeCases))]
    public void ToKeyType_MapsElementToItsKeyType(ISubmodelElement element, KeyTypes expected)
        => Assert.Equal(expected, element.ToKeyType());

    public static TheoryData<ISubmodelElement, KeyTypes> KeyTypeCases => new()
    {
        { new Property(DataTypeDefXsd.String), KeyTypes.Property },
        { new MultiLanguageProperty(), KeyTypes.MultiLanguageProperty },
        { new Range(DataTypeDefXsd.Int), KeyTypes.Range },
        { new File(contentType: "text/plain"), KeyTypes.File },
        { new Blob(contentType: "text/plain"), KeyTypes.Blob },
        { new ReferenceElement(), KeyTypes.ReferenceElement },
        { new RelationshipElement(first: CreateReference("a"), second: CreateReference("b")), KeyTypes.RelationshipElement },
        { new AnnotatedRelationshipElement(first: CreateReference("a"), second: CreateReference("b")), KeyTypes.AnnotatedRelationshipElement },
        { new BasicEventElement(observed: CreateReference("a"), direction: Direction.Input, state: StateOfEvent.On), KeyTypes.BasicEventElement },
        { new Entity(entityType: EntityType.SelfManagedEntity), KeyTypes.Entity },
        { new SubmodelElementCollection(), KeyTypes.SubmodelElementCollection },
        { new SubmodelElementList(AasSubmodelElements.Property), KeyTypes.SubmodelElementList },
        { new Operation(), KeyTypes.Operation },
        { new Capability(), KeyTypes.Capability }
    };
}
