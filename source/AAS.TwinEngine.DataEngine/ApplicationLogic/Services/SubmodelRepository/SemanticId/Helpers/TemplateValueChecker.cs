using AasCore.Aas3_1;

using File = AasCore.Aas3_1.File;
using Range = AasCore.Aas3_1.Range;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Services.SubmodelRepository.SemanticId.Helpers;

/// <summary>
/// Decides whether a value already present in a submodel template can be used as-is instead of requesting it from a plugin.
/// </summary>
public static class TemplateValueChecker
{
    private const string NullLiteral = "null";

    public static bool IsResolved(string? value)
        => !string.IsNullOrWhiteSpace(value) && !value.Trim().Equals(NullLiteral, StringComparison.OrdinalIgnoreCase);

    public static bool IsResolved(byte[]? value) => value is { Length: > 0 };

    public static bool HasResolvedKey(IReference? reference)
        => reference is { Type: ReferenceTypes.ModelReference } && reference.Keys.Any(key => IsResolved(key.Value));

    public static bool HasResolvedValue(ISubmodelElement element) => element switch
    {
        Property property => IsResolved(property.Value),
        File file => IsResolved(file.Value),
        Blob blob => IsResolved(blob.Value),
        Range range => IsResolved(range.Min) || IsResolved(range.Max),
        MultiLanguageProperty mlp => mlp.Value?.Any(text => IsResolved(text.Text)) ?? false,
        ReferenceElement referenceElement => HasResolvedKey(referenceElement.Value),
        RelationshipElement relationship => HasResolvedKey(relationship.First) || HasResolvedKey(relationship.Second),
        Entity entity => HasResolvedEntityValue(entity),
        SubmodelElementCollection collection => collection.Value?.Any(HasResolvedValue) ?? false,
        SubmodelElementList list => list.Value?.Any(HasResolvedValue) ?? false,
        _ => false
    };

    private static bool HasResolvedEntityValue(Entity entity)
        => (entity.EntityType == EntityType.SelfManagedEntity
            && (IsResolved(entity.GlobalAssetId) || (entity.SpecificAssetIds?.Any(specificAssetId => IsResolved(specificAssetId.Value)) ?? false)))
           || (entity.Statements?.Any(HasResolvedValue) ?? false);
}
