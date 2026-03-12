using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

using AasCore.Aas3_0;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using static Xunit.Assert;

using File = AasCore.Aas3_0.File;

namespace AAS.TwinEngine.DataEngine.UnitTests.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public class SubmodelElementHelperTests
{
    private readonly SubmodelElementHelper _sut;
    private readonly ILogger<SubmodelElementHelper> _logger;

    public SubmodelElementHelperTests()
    {
        _logger = Substitute.For<ILogger<SubmodelElementHelper>>();
        var mlpSettings = Options.Create(new MultiLanguagePropertySettings { DefaultLanguages = null });
        _sut = new SubmodelElementHelper(_logger, mlpSettings);
    }

    [Fact]
    public void CloneElement_Property_ReturnsDeepCopy()
    {
        var original = new Property(
            idShort: "TestProp",
            valueType: DataTypeDefXsd.String,
            value: "original"
        );

        var cloned = _sut.CloneElement(original);

        NotSame(original, cloned);
        Equal("TestProp", cloned.IdShort);
        var clonedProp = IsType<Property>(cloned);
        Equal("original", clonedProp.Value);
    }

    [Fact]
    public void CloneElement_Collection_ReturnsDeepCopyWithChildren()
    {
        var original = new SubmodelElementCollection(
            idShort: "TestCollection",
            value: [new Property(idShort: "Child", valueType: DataTypeDefXsd.String, value: "childVal")]
        );

        var cloned = _sut.CloneElement(original);

        NotSame(original, cloned);
        var clonedCollection = IsType<SubmodelElementCollection>(cloned);
        Single(clonedCollection.Value!);
        Equal("Child", clonedCollection.Value![0].IdShort);
    }

    [Fact]
    public void GetElementByIdShort_MatchingElement_ReturnsElement()
    {
        var elements = new List<ISubmodelElement>
        {
            new Property(idShort: "First", valueType: DataTypeDefXsd.String),
            new Property(idShort: "Second", valueType: DataTypeDefXsd.String),
        };

        var result = _sut.GetElementByIdShort(elements, "Second");

        NotNull(result);
        Equal("Second", result!.IdShort);
    }

    [Fact]
    public void GetElementByIdShort_NoMatch_ReturnsNull()
    {
        var elements = new List<ISubmodelElement>
        {
            new Property(idShort: "First", valueType: DataTypeDefXsd.String),
        };

        var result = _sut.GetElementByIdShort(elements, "NonExistent");

        Null(result);
    }

    [Fact]
    public void GetElementByIdShort_NullCollection_ReturnsNull()
    {
        var result = _sut.GetElementByIdShort(null, "Any");

        Null(result);
    }

    [Fact]
    public void GetElementByIdShort_WithBracketIndex_ReturnsListElement()
    {
        var listElement = new SubmodelElementList(
            idShort: "MyList",
            typeValueListElement: AasSubmodelElements.Property,
            value: [
                new Property(idShort: "Item0", valueType: DataTypeDefXsd.String, value: "zero"),
                new Property(idShort: "Item1", valueType: DataTypeDefXsd.String, value: "one"),
            ]
        );
        var elements = new List<ISubmodelElement> { listElement };

        var result = _sut.GetElementByIdShort(elements, "MyList[1]");

        NotNull(result);
        Equal("Item1", result!.IdShort);
    }

    [Fact]
    public void GetElementByIdShort_WithEncodedBracketIndex_ReturnsListElement()
    {
        var listElement = new SubmodelElementList(
            idShort: "MyList",
            typeValueListElement: AasSubmodelElements.Property,
            value: [
                new Property(idShort: "Item0", valueType: DataTypeDefXsd.String, value: "zero"),
            ]
        );
        var elements = new List<ISubmodelElement> { listElement };

        var result = _sut.GetElementByIdShort(elements, "MyList%5B0%5D");

        NotNull(result);
        Equal("Item0", result!.IdShort);
    }

    [Fact]
    public void GetElementFromListByIndex_ValidIndex_ReturnsElement()
    {
        var list = new SubmodelElementList(
            idShort: "TestList",
            typeValueListElement: AasSubmodelElements.Property,
            value: [
                new Property(idShort: "Item0", valueType: DataTypeDefXsd.String),
                new Property(idShort: "Item1", valueType: DataTypeDefXsd.String),
            ]
        );
        var elements = new List<ISubmodelElement> { list };

        var result = _sut.GetElementFromListByIndex(elements, "TestList", 1);

        Equal("Item1", result.IdShort);
    }

    [Fact]
    public void GetElementFromListByIndex_OutOfBounds_ThrowsException()
    {
        var list = new SubmodelElementList(
            idShort: "TestList",
            typeValueListElement: AasSubmodelElements.Property,
            value: [new Property(idShort: "Item0", valueType: DataTypeDefXsd.String)]
        );
        var elements = new List<ISubmodelElement> { list };

        Throws<InternalDataProcessingException>(() => _sut.GetElementFromListByIndex(elements, "TestList", 5));
    }

    [Fact]
    public void GetElementFromListByIndex_NotAList_ThrowsException()
    {
        var elements = new List<ISubmodelElement>
        {
            new Property(idShort: "NotAList", valueType: DataTypeDefXsd.String),
        };

        Throws<InternalDataProcessingException>(() => _sut.GetElementFromListByIndex(elements, "NotAList", 0));
    }

    [Fact]
    public void GetChildElements_Collection_ReturnsValue()
    {
        var child = new Property(idShort: "Child", valueType: DataTypeDefXsd.String);
        var collection = new SubmodelElementCollection(idShort: "Col", value: [child]);

        var result = _sut.GetChildElements(collection);

        NotNull(result);
        Single(result!);
        Same(child, result[0]);
    }

    [Fact]
    public void GetChildElements_List_ReturnsValue()
    {
        var child = new Property(idShort: "Child", valueType: DataTypeDefXsd.String);
        var list = new SubmodelElementList(
            idShort: "List",
            typeValueListElement: AasSubmodelElements.Property,
            value: [child]
        );

        var result = _sut.GetChildElements(list);

        NotNull(result);
        Single(result!);
    }

    [Fact]
    public void GetChildElements_Entity_ReturnsStatements()
    {
        var statement = new Property(idShort: "Statement", valueType: DataTypeDefXsd.String);
        var entity = new Entity(idShort: "Ent", entityType: EntityType.SelfManagedEntity, statements: [statement]);

        var result = _sut.GetChildElements(entity);

        NotNull(result);
        Single(result!);
    }

    [Fact]
    public void GetChildElements_Property_ReturnsNull()
    {
        var property = new Property(idShort: "Prop", valueType: DataTypeDefXsd.String);

        var result = _sut.GetChildElements(property);

        Null(result);
    }

    [Fact]
    public void ResolveLanguages_WithValues_ReturnsLanguagesFromValues()
    {
        var mlp = new MultiLanguageProperty(
            idShort: "TestMlp",
            value: [
                new LangStringTextType("en", "English"),
                new LangStringTextType("de", "German"),
            ]
        );

        var result = _sut.ResolveLanguages(mlp);

        Equal(2, result.Count);
        Contains("en", result);
        Contains("de", result);
    }

    [Fact]
    public void ResolveLanguages_WithNullValue_ReturnsEmpty()
    {
        var mlp = new MultiLanguageProperty(idShort: "TestMlp", value: null);

        var result = _sut.ResolveLanguages(mlp);

        Empty(result);
    }

    [Fact]
    public void ResolveLanguages_WithDefaultLanguages_MergesWithDefaults()
    {
        var mlpSettings = Options.Create(new MultiLanguagePropertySettings { DefaultLanguages = ["en", "fr"] });
        var sut = new SubmodelElementHelper(Substitute.For<ILogger<SubmodelElementHelper>>(), mlpSettings);

        var mlp = new MultiLanguageProperty(
            idShort: "TestMlp",
            value: [new LangStringTextType("en", "English"), new LangStringTextType("de", "German")]
        );

        var result = sut.ResolveLanguages(mlp);

        Equal(3, result.Count);
        Contains("en", result);
        Contains("de", result);
        Contains("fr", result);
    }

    [Fact]
    public void ResolveLanguages_WithOnlyDefaultLanguages_ReturnsDefaults()
    {
        var mlpSettings = Options.Create(new MultiLanguagePropertySettings { DefaultLanguages = ["en", "fr"] });
        var sut = new SubmodelElementHelper(Substitute.For<ILogger<SubmodelElementHelper>>(), mlpSettings);

        var mlp = new MultiLanguageProperty(idShort: "TestMlp", value: null);

        var result = sut.ResolveLanguages(mlp);

        Equal(2, result.Count);
        Contains("en", result);
        Contains("fr", result);
    }
}
