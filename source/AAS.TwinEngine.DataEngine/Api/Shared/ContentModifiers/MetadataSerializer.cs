using System.Text.Json.Nodes;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;

/// <summary>
/// Format "Metadata" (IDTA-01001 Mappings): the normal serialization without the value-bearing fields.
/// </summary>
internal static class MetadataSerializer
{
    private static readonly Dictionary<string, string[]> FieldsWithoutMetadata = new(StringComparer.Ordinal)
    {
        ["AssetAdministrationShell"] = ["assetInformation", "submodels"],
        ["Submodel"] = ["submodelElements"],
        ["SubmodelElementCollection"] = ["value"],
        ["SubmodelElementList"] = ["value"],
        ["Entity"] = ["statements", "globalAssetId", "specificAssetIds"],
        ["BasicEventElement"] = ["observed"],
        ["Property"] = ["value", "valueId"],
        ["MultiLanguageProperty"] = ["value", "valueId"],
        ["Range"] = ["min", "max"],
        ["ReferenceElement"] = ["value"],
        ["RelationshipElement"] = ["first", "second"],
        ["AnnotatedRelationshipElement"] = ["first", "second", "annotations"],
        ["Blob"] = ["value", "contentType"],
        ["File"] = ["value", "contentType"]
    };

    public static JsonObject Serialize(IClass instance)
    {
        var json = Jsonization.Serialize.ToJsonObject(instance);
        var modelType = json["modelType"]?.GetValue<string>();

        if (modelType is not null && FieldsWithoutMetadata.TryGetValue(modelType, out var fields))
        {
            foreach (var field in fields)
            {
                _ = json.Remove(field);
            }
        }

        return json;
    }
}
