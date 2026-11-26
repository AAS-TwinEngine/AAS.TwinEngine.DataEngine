using System.Text.Json.Nodes;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;

/// <summary>
/// Format "Metadata" (IDTA-01001 Mappings): the normal serialization without the value-bearing fields.
/// </summary>
internal static class MetadataSerializer
{
    private const string Value = "value";

    private static readonly Dictionary<string, string[]> FieldsWithoutMetadata = new(StringComparer.Ordinal)
    {
        ["AssetAdministrationShell"] = ["assetInformation", "submodels"],
        ["Submodel"] = ["submodelElements"],
        ["SubmodelElementCollection"] = [Value],
        ["SubmodelElementList"] = [Value],
        ["Entity"] = ["statements", "globalAssetId", "specificAssetIds"],
        ["BasicEventElement"] = ["observed"],
        ["Property"] = [Value, "valueId"],
        ["MultiLanguageProperty"] = [Value, "valueId"],
        ["Range"] = ["min", "max"],
        ["ReferenceElement"] = [Value],
        ["RelationshipElement"] = ["first", "second"],
        ["AnnotatedRelationshipElement"] = ["first", "second", "annotations"],
        ["Blob"] = [Value, "contentType"],
        ["File"] = [Value, "contentType"]
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
