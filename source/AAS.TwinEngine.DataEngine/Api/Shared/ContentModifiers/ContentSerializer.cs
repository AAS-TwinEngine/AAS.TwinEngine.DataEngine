using System.Text.Json.Nodes;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Extensions;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;

/// <summary>
/// Single entry point that serializes AAS model objects according to the requested <see cref="Content"/> modifier.
/// </summary>
public static class ContentSerializer
{
    public static JsonNode SerializeSubmodel(ISubmodel submodel, Content content)
    {
        ArgumentNullException.ThrowIfNull(submodel);

        return content switch
        {
            Content.Metadata => MetadataSerializer.Serialize(submodel),
            Content.Value => ValueOnlySerializer.SerializeElements(submodel.SubmodelElements),
            Content.Reference => SerializeReference(ModelReferenceExtensions.CreateModelReference(KeyTypes.Submodel, submodel.Id)),
            Content.Path => ToJsonArray(IdShortPathSerializer.SerializeElements(submodel.SubmodelElements)),
            _ => Jsonization.Serialize.ToJsonObject(submodel)
        };
    }

    public static JsonArray SerializeSubmodels(IEnumerable<ISubmodel> submodels, Content content)
    {
        ArgumentNullException.ThrowIfNull(submodels);

        return content == Content.Path
                   ? ToJsonArray(submodels.SelectMany(submodel => IdShortPathSerializer.SerializeElements(submodel.SubmodelElements)))
                   : new JsonArray([.. submodels.Select(submodel => SerializeSubmodel(submodel, content))]);
    }

    /// <param name="element">The resolved submodel element.</param>
    /// <param name="content">The requested content modifier. <see cref="Content.Reference"/> is resolved by the service layer, see <see cref="SerializeReference"/>.</param>
    /// <param name="idShortPath">The decoded idShortPath of the element, used as prefix for <see cref="Content.Path"/>.</param>
    public static JsonNode? SerializeSubmodelElement(ISubmodelElement element, Content content, string idShortPath)
    {
        ArgumentNullException.ThrowIfNull(element);

        return content switch
        {
            Content.Metadata => MetadataSerializer.Serialize(element),
            Content.Value => ValueOnlySerializer.IsSupported(element)
                                 ? ValueOnlySerializer.SerializeElement(element)
                                 : throw UnsupportedModifier(content, element),
            Content.Path => IdShortPathSerializer.IsSupported(element)
                                ? ToJsonArray(IdShortPathSerializer.SerializeElement(element, idShortPath))
                                : throw UnsupportedModifier(content, element),
            Content.Reference => throw new InvalidOperationException("Submodel element references are resolved by the service layer."),
            _ => Jsonization.Serialize.ToJsonObject(element)
        };
    }

    public static JsonNode SerializeSubmodelElements(IEnumerable<ISubmodelElement> elements, Content content, string submodelId)
    {
        ArgumentNullException.ThrowIfNull(elements);

        return content switch
        {
            Content.Metadata => new JsonArray([.. elements.Select(element => (JsonNode)MetadataSerializer.Serialize(element))]),
            Content.Value => ValueOnlySerializer.SerializeElements(elements),
            Content.Reference => new JsonArray([.. elements.Select(element => (JsonNode)SerializeReference(CreateElementReference(submodelId, element)))]),
            Content.Path => ToJsonArray(IdShortPathSerializer.SerializeElements(elements)),
            _ => new JsonArray([.. elements.Select(element => (JsonNode)Jsonization.Serialize.ToJsonObject(element))])
        };
    }

    public static JsonObject SerializeShellReference(IAssetAdministrationShell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);

        return SerializeReference(ModelReferenceExtensions.CreateModelReference(KeyTypes.AssetAdministrationShell, shell.Id));
    }

    public static JsonObject SerializeReference(IReference reference) => Jsonization.Serialize.ToJsonObject(reference);

    private static IReference CreateElementReference(string submodelId, ISubmodelElement element)
        => new IKey[]
        {
            new Key(KeyTypes.Submodel, submodelId),
            new Key(element.ToKeyType(), element.IdShort ?? string.Empty)
        }.ToModelReference();

    private static JsonArray ToJsonArray(IEnumerable<string> values) => new([.. values.Select(value => (JsonNode)JsonValue.Create(value))]);

    private static InvalidUserInputException UnsupportedModifier(Content content, ISubmodelElement element)
        => new($"Content={content} is not supported for submodel elements of type {element.ToKeyType()}.");
}
