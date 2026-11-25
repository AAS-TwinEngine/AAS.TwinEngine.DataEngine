using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using AasCore.Aas3_1;

namespace AAS.TwinEngine.DataEngine.Api.Shared.ContentModifiers;

/// <summary>
/// Format "Value" (ValueOnly serialization in JSON, IDTA-01001 Mappings).
/// Object members are ordered by idShort (ordinal) and language strings by language; list items keep their index order.
/// </summary>
internal static partial class ValueOnlySerializer
{
    public static bool IsSupported(ISubmodelElement element) => element is not (IOperation or ICapability);

    public static JsonObject SerializeElements(IEnumerable<ISubmodelElement>? elements)
    {
        var json = new JsonObject();
        foreach (var element in OrderByIdShort(elements))
        {
            var value = SerializeElement(element, isListItem: false);
            if (value is not null)
            {
                json[element.IdShort!] = value;
            }
        }

        return json;
    }

    public static JsonNode? SerializeElement(ISubmodelElement element) => SerializeElement(element, isListItem: false);

    private static JsonNode? SerializeElement(ISubmodelElement element, bool isListItem) => element switch
    {
        IProperty property => SerializeTypedValue(property.Value, property.ValueType),
        IMultiLanguageProperty mlp => SerializeLangStrings(mlp.Value, isListItem),
        IRange range => SerializeRange(range),
        IFile file => SerializeFileOrBlob(file.ContentType, file.Value),
        IBlob blob => SerializeFileOrBlob(blob.ContentType, blob.Value is null ? null : Convert.ToBase64String(blob.Value)),
        IReferenceElement referenceElement => SerializeReference(referenceElement.Value),
        IAnnotatedRelationshipElement annotated => SerializeRelationship(annotated.First, annotated.Second, annotated.Annotations),
        IRelationshipElement relationship => SerializeRelationship(relationship.First, relationship.Second, annotations: null),
        IEntity entity => SerializeEntity(entity),
        IBasicEventElement basicEvent => basicEvent.Observed is null ? null : new JsonObject { ["observed"] = SerializeReference(basicEvent.Observed) },
        ISubmodelElementCollection collection => SerializeElements(collection.Value),
        ISubmodelElementList list => SerializeList(list),
        _ => null
    };

    private static JsonArray SerializeList(ISubmodelElementList list)
    {
        var json = new JsonArray();
        foreach (var item in list.Value ?? [])
        {
            // List items are always serialized (even without value) so that indices match the normal format.
            json.Add(SerializeElement(item, isListItem: true));
        }

        return json;
    }

    private static JsonArray? SerializeLangStrings(List<ILangStringTextType>? langStrings, bool isListItem)
    {
        if (langStrings is not { Count: > 0 })
        {
            return isListItem ? [] : null;
        }

        var json = new JsonArray();
        foreach (var langString in langStrings.OrderBy(l => l.Language, StringComparer.OrdinalIgnoreCase))
        {
            json.Add(new JsonObject { [langString.Language] = langString.Text });
        }

        return json;
    }

    private static JsonObject? SerializeRange(IRange range)
    {
        if (range.Min is null && range.Max is null)
        {
            return null;
        }

        var json = new JsonObject();
        if (range.Min is not null)
        {
            json["min"] = SerializeTypedValue(range.Min, range.ValueType);
        }

        if (range.Max is not null)
        {
            json["max"] = SerializeTypedValue(range.Max, range.ValueType);
        }

        return json;
    }

    private static JsonObject? SerializeFileOrBlob(string? contentType, string? value)
    {
        if (contentType is null && value is null)
        {
            return null;
        }

        var json = new JsonObject();
        if (contentType is not null)
        {
            json["contentType"] = contentType;
        }

        if (value is not null)
        {
            json["value"] = value;
        }

        return json;
    }

    private static JsonObject? SerializeRelationship(IReference? first, IReference? second, IEnumerable<IDataElement>? annotations)
    {
        var json = new JsonObject();
        if (first is not null)
        {
            json["first"] = SerializeReference(first);
        }

        if (second is not null)
        {
            json["second"] = SerializeReference(second);
        }

        if (annotations is not null)
        {
            var annotationValues = SerializeElements(annotations);
            if (annotationValues.Count > 0)
            {
                json["annotations"] = annotationValues;
            }
        }

        return json.Count > 0 ? json : null;
    }

    private static JsonObject SerializeEntity(IEntity entity)
    {
        var json = new JsonObject();

        var statements = SerializeElements(entity.Statements);
        if (statements.Count > 0)
        {
            json["statements"] = statements;
        }

        if (entity.EntityType is not null)
        {
            json["entityType"] = Stringification.ToString(entity.EntityType);
        }

        if (!string.IsNullOrEmpty(entity.GlobalAssetId))
        {
            json["globalAssetId"] = entity.GlobalAssetId;
        }

        if (entity.SpecificAssetIds is { Count: > 0 })
        {
            json["specificAssetIds"] = new JsonArray([.. entity.SpecificAssetIds.Select(id => (JsonNode)Jsonization.Serialize.ToJsonObject(id))]);
        }

        return json;
    }

    private static JsonObject? SerializeReference(IReference? reference)
        => reference is null ? null : Jsonization.Serialize.ToJsonObject(reference);

    private static JsonValue? SerializeTypedValue(string? value, DataTypeDefXsd valueType)
    {
        if (value is null)
        {
            return null;
        }

        return valueType switch
        {
            DataTypeDefXsd.Boolean => SerializeBoolean(value),
            DataTypeDefXsd.Decimal or DataTypeDefXsd.Integer or DataTypeDefXsd.Double or DataTypeDefXsd.Float
                or DataTypeDefXsd.Byte or DataTypeDefXsd.Short or DataTypeDefXsd.Int or DataTypeDefXsd.Long
                or DataTypeDefXsd.UnsignedByte or DataTypeDefXsd.UnsignedShort or DataTypeDefXsd.UnsignedInt or DataTypeDefXsd.UnsignedLong
                or DataTypeDefXsd.PositiveInteger or DataTypeDefXsd.NonNegativeInteger
                or DataTypeDefXsd.NegativeInteger or DataTypeDefXsd.NonPositiveInteger => SerializeNumber(value),
            _ => JsonValue.Create(value)
        };
    }

    private static JsonValue SerializeBoolean(string value) => value.Trim() switch
    {
        "true" or "1" => JsonValue.Create(true),
        "false" or "0" => JsonValue.Create(false),
        _ => JsonValue.Create(value)
    };

    // Values that are not a valid JSON number (e.g. "INF", "NaN" or malformed plugin data) stay strings.
    private static JsonValue SerializeNumber(string value)
    {
        var trimmed = value.Trim().TrimStart('+');
        if (JsonNumber().IsMatch(trimmed))
        {
            return (JsonValue)JsonNode.Parse(trimmed)!;
        }

        return decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                   ? JsonValue.Create(number)
                   : JsonValue.Create(value);
    }

    private static IEnumerable<ISubmodelElement> OrderByIdShort(IEnumerable<ISubmodelElement>? elements)
        => (elements ?? []).Where(e => e.IdShort is not null && IsSupported(e)).OrderBy(e => e.IdShort, StringComparer.Ordinal);

    [GeneratedRegex(@"^-?(0|[1-9]\d*)(\.\d+)?([eE][+-]?\d+)?$")]
    private static partial Regex JsonNumber();
}
