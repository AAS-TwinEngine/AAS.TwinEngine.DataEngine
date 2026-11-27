using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository;

public class SubmodelElementReferenceBuilderTests
{
    private const string SubmodelId = "https://example.com/submodel/1";

    [Fact]
    public void Build_WithNestedListPath_ReturnsKeyForEveryElementOnThePath()
    {
        var reference = SubmodelElementReferenceBuilder.Build(CreateSubmodel(), SubmodelId, "Markings[1].MarkingName");

        Assert.Equal(ReferenceTypes.ModelReference, reference.Type);
        Assert.Equal(
            [
                (KeyTypes.Submodel, SubmodelId),
                (KeyTypes.SubmodelElementList, "Markings"),
                (KeyTypes.SubmodelElementCollection, "1"),
                (KeyTypes.Property, "MarkingName")
            ],
            reference.Keys.Select(k => (k.Type, k.Value)));
    }

    [Fact]
    public void Build_WithEntityStatementPath_ReturnsEntityKey()
    {
        var reference = SubmodelElementReferenceBuilder.Build(CreateSubmodel(), SubmodelId, "Part.SerialNumber");

        Assert.Equal([KeyTypes.Submodel, KeyTypes.Entity, KeyTypes.Property], reference.Keys.Select(k => k.Type));
    }

    [Fact]
    public void Build_WithNestedListIndices_ReturnsKeyForEveryIndexedElement()
    {
        var reference = SubmodelElementReferenceBuilder.Build(CreateSubmodel(), SubmodelId, "Matrix[1][0]");

        Assert.Equal(
            [
                (KeyTypes.Submodel, SubmodelId),
                (KeyTypes.SubmodelElementList, "Matrix"),
                (KeyTypes.SubmodelElementList, "1"),
                (KeyTypes.Property, "0")
            ],
            reference.Keys.Select(k => (k.Type, k.Value)));
    }

    [Fact]
    public void Build_WithAnnotationPath_ReturnsAnnotatedRelationshipKey()
    {
        var reference = SubmodelElementReferenceBuilder.Build(CreateSubmodel(), SubmodelId, "Rel.Note");

        Assert.Equal([KeyTypes.Submodel, KeyTypes.AnnotatedRelationshipElement, KeyTypes.Property], reference.Keys.Select(k => k.Type));
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("Markings[5]")]
    [InlineData("Part[0]")]
    [InlineData("Matrix[1][3]")]
    [InlineData("Markings[0][0]")]
    public void Build_WithUnresolvablePath_ThrowsSubmodelElementNotFoundException(string idShortPath)
        => Assert.Throws<SubmodelElementNotFoundException>(() => SubmodelElementReferenceBuilder.Build(CreateSubmodel(), SubmodelId, idShortPath));

    private static Reference CreateReference(string value) => new(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, value)]);

    private static Submodel CreateSubmodel() => new(
        SubmodelId,
        submodelElements:
        [
            new SubmodelElementList(
                AasSubmodelElements.SubmodelElementCollection,
                idShort: "Markings",
                value:
                [
                    new SubmodelElementCollection(value: [new Property(DataTypeDefXsd.String, idShort: "MarkingName", value: "CE")]),
                    new SubmodelElementCollection(value: [new Property(DataTypeDefXsd.String, idShort: "MarkingName", value: "UL")])
                ]),
            new Entity(idShort: "Part", statements: [new Property(DataTypeDefXsd.String, idShort: "SerialNumber", value: "42")]),
            new SubmodelElementList(
                AasSubmodelElements.SubmodelElementList,
                idShort: "Matrix",
                value:
                [
                    new SubmodelElementList(AasSubmodelElements.Property, value: [new Property(DataTypeDefXsd.Int, value: "1")]),
                    new SubmodelElementList(AasSubmodelElements.Property, value: [new Property(DataTypeDefXsd.Int, value: "2")])
                ]),
            new AnnotatedRelationshipElement(
                first: CreateReference("urn:a"),
                second: CreateReference("urn:b"),
                idShort: "Rel",
                annotations: [new Property(DataTypeDefXsd.String, idShort: "Note", value: "n")])
        ]);
}
