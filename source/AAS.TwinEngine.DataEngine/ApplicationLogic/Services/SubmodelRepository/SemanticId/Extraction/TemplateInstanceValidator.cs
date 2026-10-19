using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers.Interfaces;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;

using AasCore.Aas3_1;

using File = AasCore.Aas3_1.File;
using Range = AasCore.Aas3_1.Range;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Extraction;

/// <summary>
/// When a template holds several collection/list instances of the same semantic id, the template is the only source for them,
/// so every mandatory element of every instance must already carry a value.
/// </summary>
public static class TemplateInstanceValidator
{
    /// <summary>Validates mandatory values in repeated template instances before extraction.</summary>
    public static void Validate(
        IEnumerable<ISubmodelElement>? elements,
        ISemanticIdResolver semanticIdResolver,
        ISubmodelElementHelper elementHelper,
        ILogger logger,
        string parentPath = "")
    {
        ArgumentNullException.ThrowIfNull(semanticIdResolver);
        ArgumentNullException.ThrowIfNull(elementHelper);
        ArgumentNullException.ThrowIfNull(logger);

        if (elements is null)
        {
            return;
        }

        var siblings = elements.ToList();

        var multiInstanceGroups = siblings
                                  .Where(element => element is SubmodelElementCollection or SubmodelElementList)
                                  .GroupBy(semanticIdResolver.ExtractSemanticId, StringComparer.Ordinal)
                                  .Where(group => group.Key.Length > 0 && group.Count() > 1);

        foreach (var group in multiInstanceGroups)
        {
            foreach (var instance in group)
            {
                var missing = FindUnresolvedMandatory(instance, semanticIdResolver, elementHelper).FirstOrDefault();
                if (missing is null)
                {
                    continue;
                }

                logger.LogError(
                    "Template contains {Count} instances of semantic id '{SemanticId}' under '{Path}', but mandatory element '{IdShort}' of instance '{Instance}' has no value.",
                    group.Count(),
                    group.Key,
                    parentPath,
                    missing.IdShort,
                    instance.IdShort);
                throw new TemplateNotValidException();
            }
        }

        foreach (var sibling in siblings)
        {
            if (sibling is SubmodelElementList list)
            {
                ValidateRepeatedListItems(list.Value, semanticIdResolver, elementHelper, logger, $"{parentPath}/{sibling.IdShort}");
            }

            Validate(GetChildren(sibling), semanticIdResolver, elementHelper, logger, $"{parentPath}/{sibling.IdShort}");
        }
    }

    private static void ValidateRepeatedListItems(
        IEnumerable<ISubmodelElement>? items,
        ISemanticIdResolver semanticIdResolver,
        ISubmodelElementHelper elementHelper,
        ILogger logger,
        string listPath)
    {
        if (items is null)
        {
            return;
        }

        var repeatedItems = items
                           .Where(item => item is not (SubmodelElementCollection or SubmodelElementList))
                           .GroupBy(semanticIdResolver.ExtractSemanticId, StringComparer.Ordinal)
                           .Where(group => group.Key.Length > 0 && group.Count() > 1);

        foreach (var group in repeatedItems)
        {
            foreach (var item in group)
            {
                if (IsMandatory(item, semanticIdResolver) && HasUnresolvedValue(item, elementHelper))
                {
                    logger.LogError(
                        "List '{Path}' contains repeated items for semantic id '{SemanticId}', but mandatory item '{IdShort}' has no value.",
                        listPath,
                        group.Key,
                        item.IdShort);
                    throw new TemplateNotValidException();
                }
            }
        }
    }

    private static IEnumerable<ISubmodelElement>? GetChildren(ISubmodelElement element) => element switch
    {
        SubmodelElementCollection collection => collection.Value,
        SubmodelElementList list => list.Value,
        Entity entity => entity.Statements,
        _ => null
    };

    private static IEnumerable<ISubmodelElement> FindUnresolvedMandatory(ISubmodelElement element, ISemanticIdResolver semanticIdResolver, ISubmodelElementHelper elementHelper)
    {
        if (element is not (SubmodelElementCollection or SubmodelElementList) && IsMandatory(element, semanticIdResolver) && HasUnresolvedValue(element, elementHelper))
        {
            yield return element;
        }

        foreach (var child in GetChildren(element) ?? [])
        {
            foreach (var unresolved in FindUnresolvedMandatory(child, semanticIdResolver, elementHelper))
            {
                yield return unresolved;
            }
        }
    }

    private static bool IsMandatory(ISubmodelElement element, ISemanticIdResolver semanticIdResolver)
        => semanticIdResolver.GetCardinality(element) is Cardinality.One or Cardinality.OneToMany;

    private static bool HasUnresolvedValue(ISubmodelElement element, ISubmodelElementHelper elementHelper) => element switch
    {
        Property property => !TemplateValueChecker.IsResolved(property.Value),
        File file => !TemplateValueChecker.IsResolved(file.Value),
        Blob blob => !TemplateValueChecker.IsResolved(blob.Value),
        Range range => !TemplateValueChecker.IsResolved(range.Min) || !TemplateValueChecker.IsResolved(range.Max),
        MultiLanguageProperty mlp => HasUnresolvedLanguage(mlp, elementHelper),
        ReferenceElement referenceElement => HasUnresolvedKey(referenceElement.Value),
        RelationshipElement relationship => HasUnresolvedKey(relationship.First) || HasUnresolvedKey(relationship.Second),
        Entity { EntityType: EntityType.SelfManagedEntity } entity =>
            !TemplateValueChecker.IsResolved(entity.GlobalAssetId)
            || (entity.SpecificAssetIds?.Any(specificAssetId =>
                specificAssetId.SemanticId is not null && !TemplateValueChecker.IsResolved(specificAssetId.Value)) ?? false),
        _ => false
    };

    private static bool HasUnresolvedKey(IReference? reference)
        => reference is { Type: ReferenceTypes.ModelReference } && reference.Keys.Any(key => !TemplateValueChecker.IsResolved(key.Value));

    private static bool HasUnresolvedLanguage(MultiLanguageProperty mlp, ISubmodelElementHelper elementHelper)
    {
        var languages = elementHelper.ResolveLanguages(mlp) ?? [];
        return languages.Any(language => !(mlp.Value?.Any(text => text.Language.Equals(language, StringComparison.OrdinalIgnoreCase) && TemplateValueChecker.IsResolved(text.Text)) ?? false));
    }
}
