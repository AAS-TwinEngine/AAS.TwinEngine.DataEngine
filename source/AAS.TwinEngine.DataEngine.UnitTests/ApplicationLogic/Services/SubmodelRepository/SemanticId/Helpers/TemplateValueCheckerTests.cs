using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

using AasCore.Aas3_1;

using static Xunit.Assert;

using File = AasCore.Aas3_1.File;
using Range = AasCore.Aas3_1.Range;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public class TemplateValueCheckerTests
{
    [Theory]
    [InlineData("Pump")]
    [InlineData("0")]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("nullable")]
    public void IsResolved_String_ValidValue_ReturnsTrue(string value) => True(TemplateValueChecker.IsResolved(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("NULL")]
    [InlineData("Null")]
    [InlineData(" null ")]
    public void IsResolved_String_UnresolvedValue_ReturnsFalse(string? value) => False(TemplateValueChecker.IsResolved(value));

    [Fact]
    public void IsResolved_Bytes_NonEmpty_ReturnsTrue() => True(TemplateValueChecker.IsResolved([1]));

    [Fact]
    public void IsResolved_Bytes_NullOrEmpty_ReturnsFalse()
    {
        False(TemplateValueChecker.IsResolved((byte[]?)null));
        False(TemplateValueChecker.IsResolved([]));
    }

    [Fact]
    public void HasResolvedKey_ExternalReference_ReturnsFalse()
    {
        var reference = new Reference(ReferenceTypes.ExternalReference, [new Key(KeyTypes.GlobalReference, "http://external")]);

        False(TemplateValueChecker.HasResolvedKey(reference));
    }

    [Fact]
    public void HasResolvedKey_ModelReferenceWithValue_ReturnsTrue()
    {
        var reference = new Reference(ReferenceTypes.ModelReference, [new Key(KeyTypes.Submodel, ""), new Key(KeyTypes.Property, "Name")]);

        True(TemplateValueChecker.HasResolvedKey(reference));
    }

    [Fact]
    public void HasResolvedValue_NestedCollectionWithResolvedProperty_ReturnsTrue()
    {
        var collection = new SubmodelElementCollection(
            idShort: "Outer",
            value: [new SubmodelElementCollection(idShort: "Inner", value: [new Property(idShort: "P", valueType: DataTypeDefXsd.String, value: "x")])]);

        True(TemplateValueChecker.HasResolvedValue(collection));
    }

    [Fact]
    public void HasResolvedValue_ElementsWithoutValues_ReturnsFalse()
    {
        False(TemplateValueChecker.HasResolvedValue(new Property(idShort: "P", valueType: DataTypeDefXsd.String, value: "null")));
        False(TemplateValueChecker.HasResolvedValue(new File(contentType: "image/png", idShort: "F", value: "")));
        False(TemplateValueChecker.HasResolvedValue(new Blob(contentType: "image/png", idShort: "B", value: [])));
        False(TemplateValueChecker.HasResolvedValue(new Range(valueType: DataTypeDefXsd.Double, idShort: "R")));
        False(TemplateValueChecker.HasResolvedValue(new MultiLanguageProperty(idShort: "M", value: [new LangStringTextType("en", "")])));
    }

    [Fact]
    public void HasResolvedValue_RangeWithOnlyMax_ReturnsTrue()
        => True(TemplateValueChecker.HasResolvedValue(new Range(valueType: DataTypeDefXsd.Double, idShort: "R", max: "0")));

    [Fact]
    public void HasResolvedValue_CoManagedEntityGlobalAssetId_IsIgnored()
        => False(TemplateValueChecker.HasResolvedValue(new Entity(idShort: "E", entityType: EntityType.CoManagedEntity, globalAssetId: "urn:x")));
}
