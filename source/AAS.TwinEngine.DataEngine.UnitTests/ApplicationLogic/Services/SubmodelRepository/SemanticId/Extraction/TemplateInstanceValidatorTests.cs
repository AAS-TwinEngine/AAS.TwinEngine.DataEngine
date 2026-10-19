using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Extraction;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

using Microsoft.Extensions.Logging;

using NSubstitute;

using static Xunit.Assert;

using File = AasCore.Aas3_1.File;
using Range = AasCore.Aas3_1.Range;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.Extraction;

public class TemplateInstanceValidatorTests
{
    private readonly ISemanticIdResolver _resolver = Substitute.For<ISemanticIdResolver>();
    private readonly ISubmodelElementHelper _elementHelper = Substitute.For<ISubmodelElementHelper>();
    private readonly ILogger _logger = Substitute.For<ILogger>();

    public TemplateInstanceValidatorTests()
    {
        _ = _resolver.ExtractSemanticId(Arg.Any<ISubmodelElement>())
                     .Returns(call => call.Arg<ISubmodelElement>().SemanticId?.Keys.FirstOrDefault()?.Value ?? string.Empty);
        _ = _resolver.GetCardinality(Arg.Any<ISubmodelElement>())
                     .Returns(call => Enum.Parse<Cardinality>(call.Arg<ISubmodelElement>().Qualifiers!.First().Value!));
    }

    [Fact]
    public void Validate_MultipleInstancesWithMissingMandatoryValue_Throws()
    {
        List<ISubmodelElement> elements =
        [
            Contact("Contact1", name: "Hardi", department: "HR"),
            Contact("Contact2", name: "John", department: null)
        ];

        _ = Throws<TemplateNotValidException>(() => Validate(elements));
    }

    [Theory]
    [InlineData("")]
    [InlineData("NULL")]
    [InlineData("  ")]
    public void Validate_MultipleInstancesWithUnresolvedPlaceholder_Throws(string department)
    {
        List<ISubmodelElement> elements =
        [
            Contact("Contact1", name: "Hardi", department: "HR"),
            Contact("Contact2", name: "John", department: department)
        ];

        _ = Throws<TemplateNotValidException>(() => Validate(elements));
    }

    [Fact]
    public void Validate_MultipleInstancesFullyResolved_DoesNotThrow()
    {
        List<ISubmodelElement> elements =
        [
            Contact("Contact1", name: "Hardi", department: "HR"),
            Contact("Contact2", name: "John", department: "0")
        ];

        Validate(elements);
    }

    [Fact]
    public void Validate_MultipleInstancesMissingOnlyOptionalValue_DoesNotThrow()
    {
        List<ISubmodelElement> elements =
        [
            Contact("Contact1", name: "Hardi", department: "HR", departmentCardinality: "ZeroToOne"),
            Contact("Contact2", name: "John", department: null, departmentCardinality: "ZeroToOne")
        ];

        Validate(elements);
    }

    [Fact]
    public void Validate_MultipleInstancesWithMissingSpecificAssetId_Throws()
    {
        List<ISubmodelElement> elements =
        [
            Container(CreateSelfManagedEntity("Manufacturer A")),
            Container(CreateSelfManagedEntity(""))
        ];

        _ = Throws<TemplateNotValidException>(() => Validate(elements));
    }

    [Fact]
    public void Validate_MultipleInstancesWithMissingRangeBound_Throws()
        => Throws<TemplateNotValidException>(() => ValidatePair(CreateRequiredRange("0", "10"), CreateRequiredRange("0", "")));

    [Fact]
    public void Validate_MultipleInstancesWithMissingMultiLanguageValue_Throws()
    {
        var complete = CreateRequiredMultiLanguageProperty("Pump", "Pumpe");
        var incomplete = CreateRequiredMultiLanguageProperty("Pump", "");
        _elementHelper.ResolveLanguages(Arg.Any<MultiLanguageProperty>()).Returns(["en", "de"]);

        _ = Throws<TemplateNotValidException>(() => ValidatePair(complete, incomplete));
    }

    [Fact]
    public void Validate_MultipleInstancesWithMissingConfiguredDefaultLanguage_Throws()
    {
        var complete = CreateRequiredMultiLanguageProperty("Pump", "Pumpe");
        var incomplete = CreateRequiredMultiLanguageProperty("Pump", null);
        _elementHelper.ResolveLanguages(Arg.Any<MultiLanguageProperty>()).Returns(["en", "de"]);

        _ = Throws<TemplateNotValidException>(() => ValidatePair(complete, incomplete));
    }

    [Fact]
    public void Validate_MultipleInstancesWithMissingReferenceKey_Throws()
        => Throws<TemplateNotValidException>(() => ValidatePair(CreateRequiredReferenceElement("submodel-a"), CreateRequiredReferenceElement("")));

    [Fact]
    public void Validate_MultipleInstancesWithMissingRelationshipKey_Throws()
        => Throws<TemplateNotValidException>(() => ValidatePair(CreateRequiredRelationshipElement("first-a"), CreateRequiredRelationshipElement("")));

    [Fact]
    public void Validate_MultipleInstancesWithMissingFileValue_Throws()
        => Throws<TemplateNotValidException>(() => ValidatePair(CreateRequiredFile("file-a"), CreateRequiredFile("")));

    [Fact]
    public void Validate_MultipleInstancesWithEmptyBlobValue_Throws()
        => Throws<TemplateNotValidException>(() => ValidatePair(CreateRequiredBlob([1]), CreateRequiredBlob([])));

    [Fact]
    public void Validate_MultipleInstancesWithResolvedSpecificAssetIds_DoesNotThrow()
        => ValidatePair(CreateSelfManagedEntity("Manufacturer A"), CreateSelfManagedEntity("Manufacturer B"));

    [Fact]
    public void Validate_MultipleInstancesWithUnmappedSpecificAssetId_DoesNotThrow()
        => ValidatePair(CreateSelfManagedEntity("", includeSemanticId: false), CreateSelfManagedEntity("", includeSemanticId: false));

    [Fact]
    public void Validate_SingleInstanceWithMissingMandatoryValue_DoesNotThrow()
    {
        List<ISubmodelElement> elements = [Contact("Contact1", name: "Hardi", department: null)];

        Validate(elements);
    }

    [Fact]
    public void Validate_MultipleListItemsWithMissingMandatoryValue_Throws()
    {
        var list = new SubmodelElementList(
            idShort: "Contacts",
            typeValueListElement: AasSubmodelElements.SubmodelElementCollection,
            value:
            [
                Contact("Item0", name: "Hardi", department: "HR"),
                Contact("Item1", name: "John", department: null)
            ]);

        _ = Throws<TemplateNotValidException>(() => Validate([list]));
    }

    [Fact]
    public void Validate_MultipleInstancesNested_Throws()
    {
        var parent = new SubmodelElementCollection(
            idShort: "Parent",
            value:
            [
                Contact("Contact1", name: "Hardi", department: "HR"),
                Contact("Contact2", name: "John", department: null)
            ]);

        _ = Throws<TemplateNotValidException>(() => Validate([parent]));
    }

    [Fact]
    public void Validate_ElementsWithoutSemanticId_AreNotTreatedAsInstances()
    {
        List<ISubmodelElement> elements =
        [
            new SubmodelElementCollection(idShort: "A", value: [Prop("Name", null, "One")]),
            new SubmodelElementCollection(idShort: "B", value: [Prop("Name", null, "One")])
        ];

        Validate(elements);
    }

    [Fact]
    public void Validate_NullElements_DoesNotThrow() => Validate(null);

    private void Validate(IEnumerable<ISubmodelElement>? elements)
        => TemplateInstanceValidator.Validate(elements, _resolver, _elementHelper, _logger, "Submodel");

    private void ValidatePair(ISubmodelElement first, ISubmodelElement second)
        => Validate([Container(first), Container(second)]);

    private static SubmodelElementCollection Contact(string idShort, string? name, string? department, string departmentCardinality = "One")
        => new(
            idShort: idShort,
            semanticId: new Reference(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, "http://test/contact")]),
            qualifiers: [new Qualifier(type: "Cardinality", valueType: DataTypeDefXsd.String, value: "ZeroToMany")],
            value:
            [
                Prop("Name", name, "One"),
                Prop("Department", department, departmentCardinality),
                Prop("Company", "M&M", "One")
            ]);

    private static Property Prop(string idShort, string? value, string cardinality)
        => new(
            valueType: DataTypeDefXsd.String,
            idShort: idShort,
            value: value,
            semanticId: new Reference(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, $"http://test/{idShort.ToUpperInvariant()}")]),
            qualifiers: [new Qualifier(type: "Cardinality", valueType: DataTypeDefXsd.String, value: cardinality)]);

    private static SubmodelElementCollection Container(ISubmodelElement element)
        => new(
            idShort: "Container",
            semanticId: new Reference(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, "http://test/container")]),
            qualifiers: [new Qualifier(type: "Cardinality", valueType: DataTypeDefXsd.String, value: "ZeroToMany")],
            value: [element]);

    private static Entity CreateSelfManagedEntity(string specificAssetValue, bool includeSemanticId = true)
    {
        var specificAssetId = new SpecificAssetId(name: "Manufacturer", value: specificAssetValue);
        if (includeSemanticId)
        {
            specificAssetId.SemanticId = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.ConceptDescription, "http://test/manufacturer")]);
        }

        return new Entity(
            idShort: "Entity",
            entityType: EntityType.SelfManagedEntity,
            globalAssetId: "urn:asset:1",
            specificAssetIds: [specificAssetId],
            qualifiers: [new Qualifier(type: "Cardinality", valueType: DataTypeDefXsd.String, value: "One")]);
    }

    private static Range CreateRequiredRange(string? min, string? max)
        => new(
            valueType: DataTypeDefXsd.Double,
            idShort: "Range",
            min: min,
            max: max,
            qualifiers: [new Qualifier(type: "Cardinality", valueType: DataTypeDefXsd.String, value: "One")]);

    private static MultiLanguageProperty CreateRequiredMultiLanguageProperty(string? english, string? german)
    {
        List<ILangStringTextType> values = [new LangStringTextType("en", english ?? "")];
        if (german is not null)
        {
            values.Add(new LangStringTextType("de", german));
        }

        return new MultiLanguageProperty(
            idShort: "Name",
            value: values,
            qualifiers: [new Qualifier(type: "Cardinality", valueType: DataTypeDefXsd.String, value: "One")]);
    }

    private static ReferenceElement CreateRequiredReferenceElement(string keyValue)
        => new(
            idShort: "Reference",
            value: new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, keyValue)]),
            qualifiers: [new Qualifier(type: "Cardinality", valueType: DataTypeDefXsd.String, value: "One")]);

    private static RelationshipElement CreateRequiredRelationshipElement(string firstKeyValue)
        => new(
            idShort: "Relationship",
            first: new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, firstKeyValue)]),
            second: new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, "submodel-b")]),
            qualifiers: [new Qualifier(type: "Cardinality", valueType: DataTypeDefXsd.String, value: "One")]);

    private static File CreateRequiredFile(string value)
        => new(
            contentType: "text/plain",
            idShort: "File",
            value: value,
            qualifiers: [new Qualifier(type: "Cardinality", valueType: DataTypeDefXsd.String, value: "One")]);

    private static Blob CreateRequiredBlob(byte[] value)
        => new(
            contentType: "application/octet-stream",
            idShort: "Blob",
            value: value,
            qualifiers: [new Qualifier(type: "Cardinality", valueType: DataTypeDefXsd.String, value: "One")]);
}
