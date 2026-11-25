using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;

using AasCore.Aas3_1;

using File = AasCore.Aas3_1.File;

namespace AAS.TwinEngine.DataEngine.UnitTests.Api.Shared.ContentModifiers;

public class ContentSerializerTests
{
    private const string SubmodelId = "https://example.com/submodel/1";

    [Fact]
    public void SerializeSubmodel_WithMetadata_RemovesSubmodelElementsAndKeepsHeader()
    {
        var result = ContentSerializer.SerializeSubmodel(CreateSubmodel(), Content.Metadata).AsObject();

        Assert.False(result.ContainsKey("submodelElements"));
        Assert.Equal(SubmodelId, result["id"]!.GetValue<string>());
        Assert.Equal("TechnicalData", result["idShort"]!.GetValue<string>());
        Assert.Equal("Submodel", result["modelType"]!.GetValue<string>());
        Assert.NotNull(result["semanticId"]);
    }

    [Fact]
    public void SerializeSubmodel_WithValue_ReturnsUnnamedValueOnlyObjectSortedByIdShortWithTypedValues()
    {
        var expected = JsonNode.Parse("""
            {
              "Address": { "City": [ { "de": "Berlin" }, { "en": "Berlin" } ], "Street": "Main Street 1" },
              "Documents": [ { "Title": "Manual" }, {} ],
              "IsActive": true,
              "Logo": { "contentType": "image/png", "value": "https://example.com/logo.png" },
              "MaxRotationSpeed": 5000,
              "Ratio": 0.75,
              "Speed": { "min": 10, "max": 20 }
            }
            """);

        var result = ContentSerializer.SerializeSubmodel(CreateSubmodel(), Content.Value);

        Assert.True(JsonNode.DeepEquals(expected, result), result.ToJsonString());
        Assert.Equal(["Address", "Documents", "IsActive", "Logo", "MaxRotationSpeed", "Ratio", "Speed"], result.AsObject().Select(p => p.Key));
    }

    [Fact]
    public void SerializeSubmodel_WithPath_ReturnsPreOrderPathsWithoutSubmodelIdShort()
    {
        var result = ContentSerializer.SerializeSubmodel(CreateSubmodel(), Content.Path);

        string[] expected =
        [
            "Address", "Address.City", "Address.Street",
            "Documents", "Documents[0]", "Documents[0].Title", "Documents[1]",
            "IsActive", "Logo", "MaxRotationSpeed", "Ratio", "SerialNumber", "Speed", "Trigger"
        ];
        Assert.Equal(expected, result.AsArray().Select(p => p!.GetValue<string>()));
    }

    [Fact]
    public void SerializeSubmodel_WithReference_ReturnsModelReferenceWithSubmodelKey()
    {
        var expected = JsonNode.Parse($$"""
            { "type": "ModelReference", "keys": [ { "type": "Submodel", "value": "{{SubmodelId}}" } ] }
            """);

        var result = ContentSerializer.SerializeSubmodel(CreateSubmodel(), Content.Reference);

        Assert.True(JsonNode.DeepEquals(expected, result), result.ToJsonString());
    }

    [Fact]
    public void SerializeSubmodelElement_WithValueOnProperty_ReturnsRawValue()
    {
        var property = new Property(DataTypeDefXsd.Int, idShort: "MaxRotationSpeed", value: "5000");

        var result = ContentSerializer.SerializeSubmodelElement(property, Content.Value, "MaxRotationSpeed");

        Assert.Equal(5000, result!.GetValue<int>());
    }

    [Fact]
    public void SerializeSubmodelElement_WithMetadataOnProperty_RemovesValueButKeepsValueType()
    {
        var property = new Property(DataTypeDefXsd.Int, idShort: "MaxRotationSpeed", value: "5000");

        var result = ContentSerializer.SerializeSubmodelElement(property, Content.Metadata, "MaxRotationSpeed")!.AsObject();

        Assert.False(result.ContainsKey("value"));
        Assert.Equal("xs:int", result["valueType"]!.GetValue<string>());
    }

    [Fact]
    public void SerializeSubmodelElement_WithPathOnCollection_IncludesOwnPathAsPrefix()
    {
        var collection = CreateSubmodel().SubmodelElements!.Single(e => e.IdShort == "Address");

        var result = ContentSerializer.SerializeSubmodelElement(collection, Content.Path, "Parent.Address");

        Assert.Equal(["Parent.Address", "Parent.Address.City", "Parent.Address.Street"], result!.AsArray().Select(p => p!.GetValue<string>()));
    }

    [Fact]
    public void SerializeSubmodelElement_WithPathOnProperty_ThrowsInvalidUserInputException()
    {
        var property = new Property(DataTypeDefXsd.String, idShort: "Street", value: "Main Street 1");

        _ = Assert.Throws<InvalidUserInputException>(() => ContentSerializer.SerializeSubmodelElement(property, Content.Path, "Street"));
    }

    [Fact]
    public void SerializeSubmodelElement_WithValueOnOperation_ThrowsInvalidUserInputException()
    {
        var operation = new Operation(idShort: "Trigger");

        _ = Assert.Throws<InvalidUserInputException>(() => ContentSerializer.SerializeSubmodelElement(operation, Content.Value, "Trigger"));
    }

    [Fact]
    public void SerializeSubmodelElements_WithReference_ReturnsReferencePerElement()
    {
        var elements = new List<ISubmodelElement>
        {
            new Property(DataTypeDefXsd.String, idShort: "Street"),
            new SubmodelElementCollection(idShort: "Address")
        };

        var result = ContentSerializer.SerializeSubmodelElements(elements, Content.Reference, SubmodelId).AsArray();

        Assert.Equal(2, result.Count);
        var secondKeys = result[1]!["keys"]!.AsArray();
        Assert.Equal("Submodel", secondKeys[0]!["type"]!.GetValue<string>());
        Assert.Equal(SubmodelId, secondKeys[0]!["value"]!.GetValue<string>());
        Assert.Equal("SubmodelElementCollection", secondKeys[1]!["type"]!.GetValue<string>());
        Assert.Equal("Address", secondKeys[1]!["value"]!.GetValue<string>());
    }

    [Fact]
    public void SerializeSubmodels_WithPath_ReturnsFlatListOfAllSubmodels()
    {
        var first = new Submodel("sm1", submodelElements: [new Property(DataTypeDefXsd.String, idShort: "A")]);
        var second = new Submodel("sm2", submodelElements: [new Property(DataTypeDefXsd.String, idShort: "B")]);

        var result = ContentSerializer.SerializeSubmodels([first, second], Content.Path);

        Assert.Equal(["A", "B"], result.Select(p => p!.GetValue<string>()));
    }

    private static Submodel CreateSubmodel() => new(
        SubmodelId,
        idShort: "TechnicalData",
        semanticId: new Reference(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, "0173-1#01-AFZ615#016")]),
        submodelElements:
        [
            new Property(DataTypeDefXsd.Int, idShort: "MaxRotationSpeed", value: "5000"),
            new Property(DataTypeDefXsd.Double, idShort: "Ratio", value: "0.75"),
            new Property(DataTypeDefXsd.Boolean, idShort: "IsActive", value: "1"),
            new Property(DataTypeDefXsd.String, idShort: "SerialNumber"),
            new SubmodelElementCollection(
                idShort: "Address",
                value:
                [
                    new Property(DataTypeDefXsd.String, idShort: "Street", value: "Main Street 1"),
                    new MultiLanguageProperty(idShort: "City", value: [new LangStringTextType("en", "Berlin"), new LangStringTextType("de", "Berlin")])
                ]),
            new SubmodelElementList(
                AasSubmodelElements.SubmodelElementCollection,
                idShort: "Documents",
                value:
                [
                    new SubmodelElementCollection(value: [new Property(DataTypeDefXsd.String, idShort: "Title", value: "Manual")]),
                    new SubmodelElementCollection()
                ]),
            new File(contentType: "image/png", idShort: "Logo", value: "https://example.com/logo.png"),
            new AasCore.Aas3_1.Range(DataTypeDefXsd.Int, idShort: "Speed", min: "10", max: "20"),
            new Operation(idShort: "Trigger")
        ]);
}
