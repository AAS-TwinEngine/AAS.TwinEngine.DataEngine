using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AAS.TwinEngine.Plugin.TestPlugin.PlaywrightTests.ContentModifiers;

/// <summary>
/// Derives the expected content-modifier ($metadata, $reference, $path, $value) responses from the
/// normal representation of the same resource, so the tests need no stored fixtures.
/// </summary>
internal static partial class ContentModifierExpectations
{
    private const string SubmodelCollection = "SubmodelElementCollection";
    private const string SubmodelList = "SubmodelElementList";
    private const string EntityType = "Entity";
    private const string AnnotatedRelationship = "AnnotatedRelationshipElement";

    private static readonly Dictionary<string, string[]> ValueBearingFields = new(StringComparer.Ordinal)
    {
        [SubmodelCollection] = ["value"],
        [SubmodelList] = ["value"],
        [EntityType] = ["statements", "globalAssetId", "specificAssetIds"],
        ["BasicEventElement"] = ["observed"],
        ["Property"] = ["value", "valueId"],
        ["MultiLanguageProperty"] = ["value", "valueId"],
        ["Range"] = ["min", "max"],
        ["ReferenceElement"] = ["value"],
        ["RelationshipElement"] = ["first", "second"],
        [AnnotatedRelationship] = ["first", "second", "annotations"],
        ["Blob"] = ["value", "contentType"],
        ["File"] = ["value", "contentType"]
    };

    private static readonly HashSet<string> NumericValueTypes = new(StringComparer.Ordinal)
    {
        "xs:decimal", "xs:integer", "xs:int", "xs:long", "xs:short", "xs:byte",
        "xs:unsignedInt", "xs:unsignedLong", "xs:unsignedShort", "xs:unsignedByte",
        "xs:nonNegativeInteger", "xs:positiveInteger", "xs:negativeInteger", "xs:nonPositiveInteger",
        "xs:double", "xs:float"
    };

    [GeneratedRegex(@"^(?<name>[^\[\]]+)(?<indexes>(\[\d+\])*)$")]
    private static partial Regex PathSegmentRegex();

    [GeneratedRegex(@"\[(\d+)\]")]
    private static partial Regex ListIndexRegex();

    // ---------- decoding helpers ----------

    public static string DecodeBase64(string encoded)
    {
        var text = Uri.UnescapeDataString(encoded).Replace('-', '+').Replace('_', '/');
        text = text.PadRight(text.Length + ((4 - (text.Length % 4)) % 4), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(text));
    }

    /// <summary>Bracket idShortPaths are double-encoded in URLs (Documents%255B0%255D).</summary>
    public static string DecodeIdShortPath(string urlPath) => Uri.UnescapeDataString(Uri.UnescapeDataString(urlPath));

    public static string ModelTypeOf(JsonNode node) => node["modelType"]?.GetValue<string>() ?? string.Empty;

    public static string IdShortOf(JsonNode node) => node["idShort"]?.GetValue<string>() ?? string.Empty;

    // ---------- $metadata ----------

    public static JsonNode SubmodelMetadata(JsonNode submodel)
    {
        var clone = submodel.DeepClone().AsObject();
        clone.Remove("submodelElements");
        return clone;
    }

    public static JsonNode ElementMetadata(JsonNode element)
    {
        var clone = element.DeepClone().AsObject();
        if (ValueBearingFields.TryGetValue(ModelTypeOf(element), out var fields))
        {
            foreach (var field in fields)
            {
                clone.Remove(field);
            }
        }

        return clone;
    }

    public static JsonArray SubmodelMetadataList(IEnumerable<JsonNode?> submodels) =>
        new(submodels.Select(s => (JsonNode?)SubmodelMetadata(s!)).ToArray());

    public static JsonArray ElementMetadataList(IEnumerable<JsonNode?> elements) =>
        new(elements.Select(e => (JsonNode?)ElementMetadata(e!)).ToArray());

    // ---------- $reference ----------

    public static JsonObject Key(string type, string value) => new() { ["type"] = type, ["value"] = value };

    public static JsonObject ModelReference(params JsonObject[] keys) =>
        new() { ["type"] = "ModelReference", ["keys"] = new JsonArray(keys.Select(k => (JsonNode?)k).ToArray()) };

    public static JsonObject SubmodelReference(string decodedSubmodelId) => ModelReference(Key("Submodel", decodedSubmodelId));

    public static JsonObject ShellReference(string decodedAasId) => ModelReference(Key("AssetAdministrationShell", decodedAasId));

    public static JsonArray SubmodelReferenceList(IEnumerable<JsonNode?> submodels) =>
        new(submodels.Select(s => (JsonNode?)SubmodelReference(s!["id"]!.GetValue<string>())).ToArray());

    public static JsonArray ShellReferenceList(IEnumerable<JsonNode?> shells) =>
        new(shells.Select(s => (JsonNode?)ShellReference(s!["id"]!.GetValue<string>())).ToArray());

    /// <summary>Submodel key plus one key per path segment; a list index adds a key typed after the list item.</summary>
    public static JsonObject ElementReference(JsonNode submodel, string decodedSubmodelId, string decodedIdShortPath)
    {
        var keys = new List<JsonObject> { Key("Submodel", decodedSubmodelId) };
        JsonNode? current = null;

        foreach (var segment in decodedIdShortPath.Split('.'))
        {
            var match = PathSegmentRegex().Match(segment);
            if (!match.Success)
            {
                throw new InvalidOperationException($"Invalid idShortPath segment '{segment}'.");
            }

            var name = match.Groups["name"].Value;
            var siblings = current is null ? submodel["submodelElements"] : ChildrenOf(current);
            current = siblings?.AsArray().FirstOrDefault(e => IdShortOf(e!) == name)
                      ?? throw new InvalidOperationException($"Element '{name}' not found in the normal representation.");
            keys.Add(Key(ModelTypeOf(current), name));

            foreach (Match index in ListIndexRegex().Matches(match.Groups["indexes"].Value))
            {
                var itemIndex = int.Parse(index.Groups[1].Value, CultureInfo.InvariantCulture);
                current = current["value"]!.AsArray()[itemIndex]
                          ?? throw new InvalidOperationException($"List item {itemIndex} of '{name}' not found.");
                keys.Add(Key(ModelTypeOf(current), itemIndex.ToString(CultureInfo.InvariantCulture)));
            }
        }

        return ModelReference(keys.ToArray());
    }

    public static JsonArray ElementReferenceList(JsonNode submodel, string decodedSubmodelId, IEnumerable<JsonNode?> elements) =>
        new(elements.Select(e => (JsonNode?)ElementReference(submodel, decodedSubmodelId, IdShortOf(e!))).ToArray());

    private static JsonNode? ChildrenOf(JsonNode element) => ModelTypeOf(element) switch
    {
        SubmodelCollection or SubmodelList => element["value"],
        EntityType => element["statements"],
        AnnotatedRelationship => element["annotations"],
        _ => null
    };

    // ---------- $path ----------

    public static List<string> SubmodelPaths(JsonNode submodel) => ElementListPaths(submodel["submodelElements"]);

    /// <summary>Pre-order idShortPaths of top-level elements (sorted by idShort, ordinal) and all their descendants.</summary>
    public static List<string> ElementListPaths(JsonNode? elements)
    {
        var paths = new List<string>();
        foreach (var element in (elements?.AsArray() ?? []).OrderBy(e => IdShortOf(e!), StringComparer.Ordinal))
        {
            AddPaths(element!, IdShortOf(element!), paths);
        }

        return paths;
    }

    public static List<string> ElementPaths(JsonNode element, string decodedIdShortPath)
    {
        var paths = new List<string>();
        AddPaths(element, decodedIdShortPath, paths);
        return paths;
    }

    private static void AddPaths(JsonNode element, string path, List<string> paths)
    {
        paths.Add(path);

        switch (ModelTypeOf(element))
        {
            case SubmodelList:
                var items = element["value"]?.AsArray() ?? [];
                for (var i = 0; i < items.Count; i++)
                {
                    AddPaths(items[i]!, $"{path}[{i}]", paths);
                }

                break;
            case SubmodelCollection or EntityType or AnnotatedRelationship:
                var children = ChildrenOf(element)?.AsArray() ?? [];
                foreach (var child in children.OrderBy(c => IdShortOf(c!), StringComparer.Ordinal))
                {
                    AddPaths(child!, $"{path}.{IdShortOf(child!)}", paths);
                }

                break;
        }
    }

    // ---------- $value ----------

    /// <summary>Submodel $value: an unnamed object holding one entry per element that has a value.</summary>
    public static void AssertSubmodelValue(JsonNode? actual, JsonNode submodel) =>
        AssertObjectValue(actual, submodel["submodelElements"]?.AsArray() ?? [], "submodel", ordered: false);

    /// <summary>Elements-list $value: the same unnamed object, built from the listed elements.</summary>
    public static void AssertElementsValue(JsonNode? actual, JsonNode? elements) =>
        AssertObjectValue(actual, elements?.AsArray() ?? [], "submodel-elements", ordered: false);

    public static void AssertElementValue(JsonNode? actual, JsonNode element, string path)
    {
        Assert.True(actual is not null, $"$value of '{path}' is missing.");

        switch (ModelTypeOf(element))
        {
            case "Property":
                AssertScalar(actual, element["value"], element["valueType"]?.GetValue<string>(), path);
                break;
            case "MultiLanguageProperty":
                AssertMultiLanguageValue(actual, element, path);
                break;
            case SubmodelCollection:
                AssertObjectValue(actual, element["value"]?.AsArray() ?? [], path, ordered: true);
                break;
            case SubmodelList:
                AssertListValue(actual, element["value"]?.AsArray() ?? [], path);
                break;
            case "File" or "Blob":
                AssertFileValue(actual, element, path);
                break;
            case "Range":
                AssertRangeValue(actual, element, path);
                break;
            case "ReferenceElement":
                Assert.True(JsonNode.DeepEquals(element["value"], actual), $"$value of reference element '{path}' differs from its value.");
                break;
            case "RelationshipElement":
                AssertObjectKeyEquals(actual, element, "first", path);
                AssertObjectKeyEquals(actual, element, "second", path);
                break;
        }
    }

    /// <summary>True when the element contributes no entry to a parent object.</summary>
    public static bool IsOmittedFromValue(JsonNode element) => ModelTypeOf(element) switch
    {
        "Operation" or "Capability" => true,
        "Property" => IsNullOrMissing(element["value"]),
        "MultiLanguageProperty" => (element["value"]?.AsArray().Count ?? 0) == 0,
        "ReferenceElement" => IsNullOrMissing(element["value"]),
        _ => false
    };

    private static bool IsNullOrMissing(JsonNode? node) => node is null || node.GetValueKind() == JsonValueKind.Null;

    private static void AssertObjectValue(JsonNode? actual, JsonArray children, string path, bool ordered)
    {
        Assert.True(actual is JsonObject, $"$value of '{path}' must be a JSON object but was {actual?.GetValueKind()}.");
        var actualObject = actual!.AsObject();

        var expectedChildren = children
            .Where(c => !IsOmittedFromValue(c!))
            .OrderBy(c => IdShortOf(c!), StringComparer.Ordinal)
            .ToList();

        var expectedKeys = expectedChildren.Select(c => IdShortOf(c!)).ToList();
        var actualKeys = actualObject.Select(p => p.Key).ToList();

        if (ordered)
        {
            Assert.Equal(expectedKeys, actualKeys);
        }
        else
        {
            Assert.Equal(expectedKeys.Order(StringComparer.Ordinal), actualKeys.Order(StringComparer.Ordinal));
        }

        foreach (var child in expectedChildren)
        {
            var childName = IdShortOf(child!);
            AssertElementValue(actualObject[childName], child!, $"{path}.{childName}");
        }
    }

    private static void AssertListValue(JsonNode? actual, JsonArray items, string path)
    {
        Assert.True(actual is JsonArray, $"$value of list '{path}' must be a JSON array but was {actual?.GetValueKind()}.");
        var actualArray = actual!.AsArray();
        Assert.Equal(items.Count, actualArray.Count);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i]!;
            var itemPath = $"{path}[{i}]";

            if (IsOmittedFromValue(item))
            {
                // A list keeps one entry per item; an empty MLP is written as [].
                if (ModelTypeOf(item) == "MultiLanguageProperty")
                {
                    Assert.True(actualArray[i] is JsonArray { Count: 0 }, $"$value of '{itemPath}' must be an empty array.");
                }

                continue;
            }

            AssertElementValue(actualArray[i], item, itemPath);
        }
    }

    private static void AssertMultiLanguageValue(JsonNode? actual, JsonNode element, string path)
    {
        Assert.True(actual is JsonArray, $"$value of '{path}' must be a JSON array but was {actual?.GetValueKind()}.");

        var expected = (element["value"]?.AsArray() ?? [])
            .Select(e => (Language: e!["language"]!.GetValue<string>(), Text: e["text"]!.GetValue<string>()))
            .OrderBy(e => e.Language, StringComparer.Ordinal)
            .ToList();

        var actualEntries = actual!.AsArray();
        Assert.Equal(expected.Count, actualEntries.Count);

        for (var i = 0; i < expected.Count; i++)
        {
            var entry = actualEntries[i] as JsonObject;
            Assert.True(entry is not null && entry.Count == 1, $"Entry {i} of '{path}' must be a single-key object.");
            var pair = entry.Single();
            Assert.Equal(expected[i].Language, pair.Key);
            Assert.Equal(expected[i].Text, pair.Value!.GetValue<string>());
        }
    }

    private static void AssertFileValue(JsonNode? actual, JsonNode element, string path)
    {
        Assert.True(actual is JsonObject, $"$value of '{path}' must be a JSON object but was {actual?.GetValueKind()}.");
        var actualObject = actual!.AsObject();

        var contentType = element["contentType"]?.GetValue<string>();
        if (contentType is not null)
        {
            Assert.Equal(contentType, actualObject["contentType"]?.GetValue<string>());
        }

        if (ModelTypeOf(element) == "File" && element["value"] is { } fileValue)
        {
            Assert.Equal(fileValue.GetValue<string>(), actualObject["value"]?.GetValue<string>());
        }
    }

    private static void AssertRangeValue(JsonNode? actual, JsonNode element, string path)
    {
        Assert.True(actual is JsonObject, $"$value of '{path}' must be a JSON object but was {actual?.GetValueKind()}.");
        var valueType = element["valueType"]?.GetValue<string>();

        foreach (var bound in new[] { "min", "max" })
        {
            if (!IsNullOrMissing(element[bound]))
            {
                AssertScalar(actual!.AsObject()[bound], element[bound], valueType, $"{path}.{bound}");
            }
        }
    }

    private static void AssertObjectKeyEquals(JsonNode? actual, JsonNode element, string key, string path)
    {
        Assert.True(actual is JsonObject, $"$value of '{path}' must be a JSON object but was {actual?.GetValueKind()}.");
        Assert.True(JsonNode.DeepEquals(element[key], actual![key]), $"'{key}' of '{path}' differs from the normal representation.");
    }

    private static void AssertScalar(JsonNode? actual, JsonNode? rawValue, string? valueType, string path)
    {
        Assert.True(actual is JsonValue, $"$value of '{path}' must be a JSON scalar but was {actual?.GetValueKind()}.");
        var raw = rawValue?.GetValue<string>() ?? string.Empty;
        var kind = actual!.GetValueKind();

        if (valueType == "xs:boolean")
        {
            var expected = raw is "true" or "1";
            Assert.Equal(expected ? JsonValueKind.True : JsonValueKind.False, kind);
        }
        else if (valueType is not null && NumericValueTypes.Contains(valueType)
                 && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var expectedNumber))
        {
            Assert.Equal(JsonValueKind.Number, kind);
            Assert.Equal(expectedNumber, actual.GetValue<double>(), 9);
        }
        else
        {
            Assert.Equal(JsonValueKind.String, kind);
            Assert.Equal(raw, actual.GetValue<string>());
        }
    }
}
