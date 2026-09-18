using System.Globalization;
using System.Text.RegularExpressions;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Microsoft.Extensions.Options;

using Range = AasCore.Aas3_1.Range;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

public partial class SubmodelElementHelper(ILogger<SubmodelElementHelper> logger, IOptions<PluginsConfig> pluginsConfig) : ISubmodelElementHelper
{
    private readonly HashSet<string>? _defaultLanguagesSet = pluginsConfig.Value.MultiLanguageProperty.DefaultLanguages is { Count: > 0 }
                                                                ? new HashSet<string>(pluginsConfig.Value.MultiLanguageProperty.DefaultLanguages, StringComparer.OrdinalIgnoreCase)
                                                                : null;

    public ISubmodelElement CloneElement(ISubmodelElement element) => CloneSubmodelElement(element);

    public IReadOnlyList<ISubmodelElement> CloneElements(ISubmodelElement element, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        var clones = new ISubmodelElement[count];
        for (var i = 0; i < count; i++)
        {
            clones[i] = CloneSubmodelElement(element);
        }

        return clones;
    }

    public ISubmodelElement? GetElementByIdShort(IEnumerable<ISubmodelElement>? submodelElements, string idShort)
    {
        if (TryParseIdShortWithBracketIndex(idShort, out var idShortWithoutIndex, out var index))
        {
            return GetElementFromListByIndex(submodelElements, idShortWithoutIndex, index);
        }

        return submodelElements?.FirstOrDefault(e => e.IdShort == idShort);
    }

    public ISubmodelElement GetElementFromListByIndex(IEnumerable<ISubmodelElement>? elements, string idShortWithoutIndex, int index)
    {
        var baseElement = elements?.FirstOrDefault(e => e.IdShort == idShortWithoutIndex);

        if (baseElement is not ISubmodelElementList list)
        {
            logger.LogError("Expected list element with IdShort '{IdShortWithoutIndex}' not found or is not a list.", idShortWithoutIndex);
            throw new InternalDataProcessingException();
        }

        if (index >= 0 && index < list.Value!.Count)
        {
            return list.Value[index];
        }

        logger.LogError("Index {Index} is out of bounds for list '{IdShortWithoutIndex}' with count {Count}.", index, idShortWithoutIndex, list.Value!.Count);
        throw new InternalDataProcessingException();
    }

    public IList<ISubmodelElement>? GetChildElements(ISubmodelElement submodelElement)
    {
        return submodelElement switch
        {
            ISubmodelElementCollection c => c.Value,
            ISubmodelElementList l => l.Value,
            IEntity entity => entity.Statements,
            _ => null
        };
    }

    public HashSet<string> ResolveLanguages(MultiLanguageProperty mlp)
    {
        var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (mlp.Value is { Count: > 0 })
        {
            foreach (var langValue in mlp.Value)
            {
                _ = languages.Add(langValue.Language);
            }
        }

        if (_defaultLanguagesSet != null)
        {
            languages.UnionWith(_defaultLanguagesSet);
        }

        return languages;
    }

    // Native field-by-field cloning avoids the reflection-heavy Jsonization serialize/deserialize round-trip.
    private static ISubmodelElement CloneSubmodelElement(ISubmodelElement element) => element switch
    {
        Property p => new Property(p.ValueType, CloneExtensions(p.Extensions), p.Category, p.IdShort, CloneDisplayNames(p.DisplayName), CloneDescriptions(p.Description), CloneReference(p.SemanticId), CloneReferenceList(p.SupplementalSemanticIds), CloneQualifiers(p.Qualifiers), CloneEmbeddedDataSpecifications(p.EmbeddedDataSpecifications), p.Value, CloneReference(p.ValueId)),
        MultiLanguageProperty mlp => new MultiLanguageProperty(CloneExtensions(mlp.Extensions), mlp.Category, mlp.IdShort, CloneDisplayNames(mlp.DisplayName), CloneDescriptions(mlp.Description), CloneReference(mlp.SemanticId), CloneReferenceList(mlp.SupplementalSemanticIds), CloneQualifiers(mlp.Qualifiers), CloneEmbeddedDataSpecifications(mlp.EmbeddedDataSpecifications), CloneDescriptions(mlp.Value), CloneReference(mlp.ValueId)),
        SubmodelElementCollection c => new SubmodelElementCollection(CloneExtensions(c.Extensions), c.Category, c.IdShort, CloneDisplayNames(c.DisplayName), CloneDescriptions(c.Description), CloneReference(c.SemanticId), CloneReferenceList(c.SupplementalSemanticIds), CloneQualifiers(c.Qualifiers), CloneEmbeddedDataSpecifications(c.EmbeddedDataSpecifications), CloneChildren(c.Value)),
        SubmodelElementList l => new SubmodelElementList(l.TypeValueListElement, CloneExtensions(l.Extensions), l.Category, l.IdShort, CloneDisplayNames(l.DisplayName), CloneDescriptions(l.Description), CloneReference(l.SemanticId), CloneReferenceList(l.SupplementalSemanticIds), CloneQualifiers(l.Qualifiers), CloneEmbeddedDataSpecifications(l.EmbeddedDataSpecifications), l.OrderRelevant, CloneReference(l.SemanticIdListElement), l.ValueTypeListElement, CloneChildren(l.Value)),
        Entity e => new Entity(CloneExtensions(e.Extensions), e.Category, e.IdShort, CloneDisplayNames(e.DisplayName), CloneDescriptions(e.Description), CloneReference(e.SemanticId), CloneReferenceList(e.SupplementalSemanticIds), CloneQualifiers(e.Qualifiers), CloneEmbeddedDataSpecifications(e.EmbeddedDataSpecifications), CloneChildren(e.Statements), e.EntityType, e.GlobalAssetId, CloneSpecificAssetIds(e.SpecificAssetIds)),
        ReferenceElement r => new ReferenceElement(CloneExtensions(r.Extensions), r.Category, r.IdShort, CloneDisplayNames(r.DisplayName), CloneDescriptions(r.Description), CloneReference(r.SemanticId), CloneReferenceList(r.SupplementalSemanticIds), CloneQualifiers(r.Qualifiers), CloneEmbeddedDataSpecifications(r.EmbeddedDataSpecifications), CloneReference(r.Value)),
        AnnotatedRelationshipElement a => new AnnotatedRelationshipElement(CloneExtensions(a.Extensions), a.Category, a.IdShort, CloneDisplayNames(a.DisplayName), CloneDescriptions(a.Description), CloneReference(a.SemanticId), CloneReferenceList(a.SupplementalSemanticIds), CloneQualifiers(a.Qualifiers), CloneEmbeddedDataSpecifications(a.EmbeddedDataSpecifications), CloneReference(a.First), CloneReference(a.Second), CloneAnnotations(a.Annotations)),
        RelationshipElement r => new RelationshipElement(CloneExtensions(r.Extensions), r.Category, r.IdShort, CloneDisplayNames(r.DisplayName), CloneDescriptions(r.Description), CloneReference(r.SemanticId), CloneReferenceList(r.SupplementalSemanticIds), CloneQualifiers(r.Qualifiers), CloneEmbeddedDataSpecifications(r.EmbeddedDataSpecifications), CloneReference(r.First), CloneReference(r.Second)),
        AasCore.Aas3_1.File f => new AasCore.Aas3_1.File(CloneExtensions(f.Extensions), f.Category, f.IdShort, CloneDisplayNames(f.DisplayName), CloneDescriptions(f.Description), CloneReference(f.SemanticId), CloneReferenceList(f.SupplementalSemanticIds), CloneQualifiers(f.Qualifiers), CloneEmbeddedDataSpecifications(f.EmbeddedDataSpecifications), f.Value, f.ContentType),
        Blob b => new Blob(CloneExtensions(b.Extensions), b.Category, b.IdShort, CloneDisplayNames(b.DisplayName), CloneDescriptions(b.Description), CloneReference(b.SemanticId), CloneReferenceList(b.SupplementalSemanticIds), CloneQualifiers(b.Qualifiers), CloneEmbeddedDataSpecifications(b.EmbeddedDataSpecifications), (byte[]?)b.Value?.Clone(), b.ContentType),
        Range rng => new Range(rng.ValueType, CloneExtensions(rng.Extensions), rng.Category, rng.IdShort, CloneDisplayNames(rng.DisplayName), CloneDescriptions(rng.Description), CloneReference(rng.SemanticId), CloneReferenceList(rng.SupplementalSemanticIds), CloneQualifiers(rng.Qualifiers), CloneEmbeddedDataSpecifications(rng.EmbeddedDataSpecifications), rng.Min, rng.Max),
        Operation o => new Operation(CloneExtensions(o.Extensions), o.Category, o.IdShort, CloneDisplayNames(o.DisplayName), CloneDescriptions(o.Description), CloneReference(o.SemanticId), CloneReferenceList(o.SupplementalSemanticIds), CloneQualifiers(o.Qualifiers), CloneEmbeddedDataSpecifications(o.EmbeddedDataSpecifications), CloneOperationVariables(o.InputVariables), CloneOperationVariables(o.OutputVariables), CloneOperationVariables(o.InoutputVariables)),
        Capability c => new Capability(CloneExtensions(c.Extensions), c.Category, c.IdShort, CloneDisplayNames(c.DisplayName), CloneDescriptions(c.Description), CloneReference(c.SemanticId), CloneReferenceList(c.SupplementalSemanticIds), CloneQualifiers(c.Qualifiers), CloneEmbeddedDataSpecifications(c.EmbeddedDataSpecifications)),
        BasicEventElement ev => new BasicEventElement(CloneReference(ev.Observed)!, ev.Direction, ev.State, CloneExtensions(ev.Extensions), ev.Category, ev.IdShort, CloneDisplayNames(ev.DisplayName), CloneDescriptions(ev.Description), CloneReference(ev.SemanticId), CloneReferenceList(ev.SupplementalSemanticIds), CloneQualifiers(ev.Qualifiers), CloneEmbeddedDataSpecifications(ev.EmbeddedDataSpecifications), ev.MessageTopic, CloneReference(ev.MessageBroker), ev.LastUpdate, ev.MinInterval, ev.MaxInterval),
        _ => throw new InternalDataProcessingException()
    };

    private static List<ISubmodelElement>? CloneChildren(List<ISubmodelElement>? children) => children?.ConvertAll(CloneSubmodelElement);

    private static List<IDataElement>? CloneAnnotations(List<IDataElement>? annotations) => annotations?.ConvertAll(a => (IDataElement)CloneSubmodelElement(a));

    private static List<IOperationVariable>? CloneOperationVariables(List<IOperationVariable>? variables) =>
        variables?.ConvertAll(v => (IOperationVariable)new OperationVariable(CloneSubmodelElement(v.Value)));

    private static List<IExtension>? CloneExtensions(List<IExtension>? extensions) =>
        extensions?.ConvertAll(e => (IExtension)new Extension(e.Name, CloneReference(e.SemanticId), CloneReferenceList(e.SupplementalSemanticIds), e.ValueType, e.Value, CloneReferenceList(e.RefersTo)));

    private static List<IQualifier>? CloneQualifiers(List<IQualifier>? qualifiers) =>
        qualifiers?.ConvertAll(q => (IQualifier)new Qualifier(q.Type, q.ValueType, CloneReference(q.SemanticId), CloneReferenceList(q.SupplementalSemanticIds), q.Kind, q.Value, CloneReference(q.ValueId)));

    private static List<ISpecificAssetId>? CloneSpecificAssetIds(List<ISpecificAssetId>? assetIds) =>
        assetIds?.ConvertAll(a => (ISpecificAssetId)new SpecificAssetId(a.Name, a.Value, CloneReference(a.SemanticId), CloneReferenceList(a.SupplementalSemanticIds), CloneReference(a.ExternalSubjectId)));

    private static List<ILangStringNameType>? CloneDisplayNames(List<ILangStringNameType>? names) =>
        names?.ConvertAll(n => (ILangStringNameType)new LangStringNameType(n.Language, n.Text));

    private static List<ILangStringTextType>? CloneDescriptions(List<ILangStringTextType>? texts) =>
        texts?.ConvertAll(t => (ILangStringTextType)new LangStringTextType(t.Language, t.Text));

    private static IReference? CloneReference(IReference? reference) =>
        reference is null
            ? null
            : new Reference(reference.Type, reference.Keys.ConvertAll(k => (IKey)new Key(k.Type, k.Value)), CloneReference(reference.ReferredSemanticId));

    private static List<IReference>? CloneReferenceList(List<IReference>? references) => references?.ConvertAll(r => CloneReference(r)!);

    private static List<IEmbeddedDataSpecification>? CloneEmbeddedDataSpecifications(List<IEmbeddedDataSpecification>? specifications) =>
        specifications?.ConvertAll(s => (IEmbeddedDataSpecification)new EmbeddedDataSpecification(CloneReference(s.DataSpecification), CloneDataSpecificationContent(s.DataSpecificationContent)));

    private static IDataSpecificationContent? CloneDataSpecificationContent(IDataSpecificationContent? content) => content switch
    {
        null => null,
        DataSpecificationIec61360 iec => new DataSpecificationIec61360(
            iec.PreferredName.ConvertAll(n => (ILangStringPreferredNameTypeIec61360)new LangStringPreferredNameTypeIec61360(n.Language, n.Text)),
            iec.ShortName?.ConvertAll(n => (ILangStringShortNameTypeIec61360)new LangStringShortNameTypeIec61360(n.Language, n.Text)),
            iec.Unit,
            CloneReference(iec.UnitId),
            iec.SourceOfDefinition,
            iec.Symbol,
            iec.DataType,
            iec.Definition?.ConvertAll(n => (ILangStringDefinitionTypeIec61360)new LangStringDefinitionTypeIec61360(n.Language, n.Text)),
            iec.ValueFormat,
            CloneValueList(iec.ValueList),
            iec.Value,
            CloneLevelType(iec.LevelType)),
        _ => throw new InternalDataProcessingException()
    };

    private static IValueList? CloneValueList(IValueList? valueList) =>
        valueList is null
            ? null
            : new ValueList(valueList.ValueReferencePairs.ConvertAll(p => (IValueReferencePair)new ValueReferencePair(p.Value, CloneReference(p.ValueId))));

    private static ILevelType? CloneLevelType(ILevelType? levelType) =>
        levelType is null ? null : new LevelType(levelType.Min, levelType.Nom, levelType.Typ, levelType.Max);

    private static bool TryParseIdShortWithBracketIndex(string idShort, out string idShortWithoutIndex, out int index)
    {
        var match = SubmodelElementListIndex().Match(idShort);
        if (!match.Success)
        {
            idShortWithoutIndex = string.Empty;
            index = -1;
            return false;
        }

        idShortWithoutIndex = match.Groups[1].Value;
        var indexGroup = match.Groups[2].Success ? match.Groups[2] : match.Groups[3];
        if (!indexGroup.Success)
        {
            idShortWithoutIndex = string.Empty;
            index = -1;
            return false;
        }

        index = int.Parse(indexGroup.Value, CultureInfo.InvariantCulture);
        return true;
    }

    /// <summary>
    /// Matches strings like "element[3]" and captures:
    ///   Group 1 → element name (any characters, lazy match)
    ///   Group 2 → index (digits inside square brackets)
    /// e.g. "element[3]" -> matches Group1= "element", Group2 = "3"
    /// Pattern: ^(.+?)\[(\d+)\]$
    /// </summary>
    [GeneratedRegex(@"^(.+?)(?:\[(\d+)\]|%5B(\d+)%5D)$")]
    private static partial Regex SubmodelElementListIndex();
}
