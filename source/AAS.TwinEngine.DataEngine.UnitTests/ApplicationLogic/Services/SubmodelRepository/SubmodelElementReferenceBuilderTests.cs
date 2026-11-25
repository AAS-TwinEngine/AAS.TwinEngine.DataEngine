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

    [Theory]
    [InlineData("Unknown")]
    [InlineData("Markings[5]")]
    [InlineData("Part[0]")]
    public void Build_WithUnresolvablePath_ThrowsSubmodelElementNotFoundException(string idShortPath)
        => Assert.Throws<SubmodelElementNotFoundException>(() => SubmodelElementReferenceBuilder.Build(CreateSubmodel(), SubmodelId, idShortPath));

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
            new Entity(idShort: "Part", statements: [new Property(DataTypeDefXsd.String, idShort: "SerialNumber", value: "42")])
        ]);
}
