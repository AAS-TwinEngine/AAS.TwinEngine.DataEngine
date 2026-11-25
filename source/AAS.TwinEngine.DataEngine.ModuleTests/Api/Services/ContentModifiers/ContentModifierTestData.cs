namespace AAS.TwinEngine.DataEngine.ModuleTests.Api.Services.ContentModifiers;

/// <summary>
/// Shared fixture and expected payloads for the content modifier route tests.
/// The fixture submodel contains the property "Name" (xs:string "A") and the collection "Group" with the property "Child" (xs:int 5).
/// </summary>
internal static class ContentModifierTestData
{
    public const string SubmodelId = "https://example.com/submodel/1";
    public const string AasId = "https://example.com/aas/1";

    public const string SubmodelMetadata = """{ "id": "https://example.com/submodel/1", "modelType": "Submodel" }""";
    public const string SubmodelValue = """{ "Group": { "Child": 5 }, "Name": "A" }""";
    public const string SubmodelReference = """{ "type": "ModelReference", "keys": [ { "type": "Submodel", "value": "https://example.com/submodel/1" } ] }""";
    public const string SubmodelPath = """[ "Group", "Group.Child", "Name" ]""";

    public const string ElementsMetadata = """
        { "paging_metadata": { "cursor": "next" },
          "result": [ { "idShort": "Name", "valueType": "xs:string", "modelType": "Property" },
                      { "idShort": "Group", "modelType": "SubmodelElementCollection" } ] }
        """;

    public const string ElementsValue = """{ "paging_metadata": { "cursor": "next" }, "result": { "Group": { "Child": 5 }, "Name": "A" } }""";

    public const string ElementsReference = """
        { "paging_metadata": { "cursor": "next" },
          "result": [ { "type": "ModelReference", "keys": [ { "type": "Submodel", "value": "https://example.com/submodel/1" }, { "type": "Property", "value": "Name" } ] },
                      { "type": "ModelReference", "keys": [ { "type": "Submodel", "value": "https://example.com/submodel/1" }, { "type": "SubmodelElementCollection", "value": "Group" } ] } ] }
        """;

    public const string ElementsPath = """{ "paging_metadata": { "cursor": "next" }, "result": [ "Group", "Group.Child", "Name" ] }""";

    public const string GroupMetadata = """{ "idShort": "Group", "modelType": "SubmodelElementCollection" }""";
    public const string GroupValue = """{ "Child": 5 }""";
    public const string GroupPath = """[ "Group", "Group.Child" ]""";
    public const string GroupReference = """
        { "type": "ModelReference", "keys": [ { "type": "Submodel", "value": "https://example.com/submodel/1" }, { "type": "SubmodelElementCollection", "value": "Group" } ] }
        """;

    public const string NameMetadata = """{ "idShort": "Name", "valueType": "xs:string", "modelType": "Property" }""";
    public const string NameValue = "\"A\"";
    public const string NameReference = """
        { "type": "ModelReference", "keys": [ { "type": "Submodel", "value": "https://example.com/submodel/1" }, { "type": "Property", "value": "Name" } ] }
        """;

    public const string ListSubmodelsMetadata = """{ "paging_metadata": { "cursor": "next" }, "result": [ { "id": "https://example.com/submodel/1", "modelType": "Submodel" } ] }""";
    public const string ListSubmodelsValue = """{ "paging_metadata": { "cursor": "next" }, "result": [ { "Group": { "Child": 5 }, "Name": "A" } ] }""";
    public const string ListSubmodelsReference = """
        { "paging_metadata": { "cursor": "next" },
          "result": [ { "type": "ModelReference", "keys": [ { "type": "Submodel", "value": "https://example.com/submodel/1" } ] } ] }
        """;
    public const string ListSubmodelsPath = """{ "paging_metadata": { "cursor": "next" }, "result": [ "Group", "Group.Child", "Name" ] }""";
}
