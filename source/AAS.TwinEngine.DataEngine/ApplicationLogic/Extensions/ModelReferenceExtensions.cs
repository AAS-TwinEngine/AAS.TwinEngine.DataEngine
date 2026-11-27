using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

public static class ModelReferenceExtensions
{
    public static KeyTypes ToKeyType(this ISubmodelElement element) => element switch
    {
        // AnnotatedRelationshipElement derives from RelationshipElement, so it must be matched first.
        IAnnotatedRelationshipElement => KeyTypes.AnnotatedRelationshipElement,
        IRelationshipElement => KeyTypes.RelationshipElement,
        IBasicEventElement => KeyTypes.BasicEventElement,
        IBlob => KeyTypes.Blob,
        ICapability => KeyTypes.Capability,
        IEntity => KeyTypes.Entity,
        IFile => KeyTypes.File,
        IMultiLanguageProperty => KeyTypes.MultiLanguageProperty,
        IOperation => KeyTypes.Operation,
        IProperty => KeyTypes.Property,
        IRange => KeyTypes.Range,
        IReferenceElement => KeyTypes.ReferenceElement,
        ISubmodelElementCollection => KeyTypes.SubmodelElementCollection,
        ISubmodelElementList => KeyTypes.SubmodelElementList,
        _ => KeyTypes.SubmodelElement
    };

    public static IReference ToModelReference(this IEnumerable<IKey> keys)
        => new Reference(ReferenceTypes.ModelReference, [.. keys]);

    public static IReference CreateModelReference(KeyTypes type, string value)
        => new[] { new Key(type, value) }.ToModelReference();
}
