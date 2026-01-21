using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;
using AAS.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.More;
using Json.Schema;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Submodel.Services;

public class SemanticTreeHandlerTests
{
    private readonly SemanticTreeHandler _sut;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public SemanticTreeHandlerTests()
    {
        var jsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();
        _sut = new SemanticTreeHandler(jsonSchemaValidator);
    }

    [Fact]
    public void GetJson_WithLeafNodeWithStringDataType_ReturnsJsonWithValue()
    {
        var dataQueryString = JsonNode.Parse(@"
            {
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
        ""Name"": {
        ""type"": ""string""
        }},
        ""required"": [""Name""],
        ""definitions"" : {}
        }").AsJsonString();
        var dataQuery = JsonSerializer.Deserialize<JsonSchema>(dataQueryString, _options);
        var leafNodeWithStringDataType = new SemanticLeafNode("Name", DataType.String, "John");
        var expectedLeafWithStringDataTypeJson = JsonNode.Parse(@"{""Name"" : ""John""}")?.AsObject();

        var result = _sut.GetJson(leafNodeWithStringDataType, dataQuery);

        Assert.Equal(JsonSerializer.Serialize(expectedLeafWithStringDataTypeJson), JsonSerializer.Serialize(result));
    }

    [Fact]
    public void GetJson_WithLeafNodeWithBooleanDataType_ReturnsJsonWithValue()
    {
        var dataQueryString = JsonNode.Parse(@"
            {
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
        ""HaveTitle"": {
        ""type"": ""boolean""
        }},
        ""required"": [""HaveTitle""],
        ""definitions"" : {}
        }").AsJsonString();
        var dataQuery = JsonSerializer.Deserialize<JsonSchema>(dataQueryString, _options);
        var leafNodeWithBooleanDataType = new SemanticLeafNode("HaveTitle", DataType.Boolean, "true");
        var expectedLeafWithBooleanDataTypeJson = JsonNode.Parse(@"{""HaveTitle"" : true }")?.AsObject();

        var result = _sut.GetJson(leafNodeWithBooleanDataType, dataQuery);

        Assert.Equal(JsonSerializer.Serialize(expectedLeafWithBooleanDataTypeJson), JsonSerializer.Serialize(result));
    }

    [Fact]
    public void GetJson_WithLeafNodeWithIntegerDataType_ReturnsJsonWithValue()
    {
        var dataQueryString = JsonNode.Parse(@"
            {
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
        ""Weight"": {
        ""type"": ""integer""
        }},
        ""required"": [""Weight""],
        ""definitions"" : {}
        }").AsJsonString();
        var dataQuery = JsonSerializer.Deserialize<JsonSchema>(dataQueryString, _options);
        var leafNodeWithIntergerDataType = new SemanticLeafNode("Weight", DataType.Integer, "22");
        var expectedLeafWithIntergerDataTypeJson = JsonNode.Parse(@"{""Weight"" : 22 }").AsObject();

        var result = _sut.GetJson(leafNodeWithIntergerDataType, dataQuery);

        Assert.Equal(JsonSerializer.Serialize(expectedLeafWithIntergerDataTypeJson), JsonSerializer.Serialize(result));
    }

    [Fact]
    public void GetJson_WithLeafNodeWithNumberDataType_ReturnsJsonWithValue()
    {
        var dataQueryString = JsonNode.Parse(@"
            {
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
        ""Weight"": {
        ""type"": ""number""
        }},
        ""required"": [""Weight""],
        ""definitions"" : {}
        }").AsJsonString();
        var dataQuery = JsonSerializer.Deserialize<JsonSchema>(dataQueryString, _options);
        var leafNodeWithNumberDataType = new SemanticLeafNode("Weight", DataType.Number, "35.485");
        var expectedLeafWithNumberDataTypeJson = JsonNode.Parse(@"{""Weight"" : 35.485 }").AsObject();

        var result = _sut.GetJson(leafNodeWithNumberDataType, dataQuery);

        Assert.Equal(JsonSerializer.Serialize(expectedLeafWithNumberDataTypeJson), JsonSerializer.Serialize(result));
    }

    [Fact]
    public void GetJson_WithLeafNodeWithNoDataType_ReturnsJsonWithValueAsString()
    {
        var dataQueryString = JsonNode.Parse(@"
            {
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
        ""ImageLink"": {
        }},
        ""required"": [""ImageLink""],
        ""definitions"" : {}
        }").AsJsonString();
        var dataQuery = JsonSerializer.Deserialize<JsonSchema>(dataQueryString, _options);
        var leafNodeWithNoDataType = new SemanticLeafNode("ImageLink", DataType.Unknown, "https://www.mm-software.com/fake");
        var expectedLeafWithNoDataTypeJson = JsonNode.Parse(@"{""ImageLink"" : ""https://www.mm-software.com/fake"" }").AsObject();

        var result = _sut.GetJson(leafNodeWithNoDataType, dataQuery);

        Assert.Equal(JsonSerializer.Serialize(expectedLeafWithNoDataTypeJson), JsonSerializer.Serialize(result));
    }

    [Fact]
    public void GetJson_WithSingleChildBranchWithObjectDataType_ReturnsValue()
    {
        var dataQueryString = JsonNode.Parse(@"{
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
        ""ContactInformation"": {
        ""type"": ""object"",
        ""properties"": {
        ""Name"": {
          ""type"": ""string""
        }
        },
      ""required"": [""Name""],
        ""definitions"" : {}
        }}}
        ").AsJsonString();
        var dataQuery = JsonSerializer.Deserialize<JsonSchema>(dataQueryString, _options);
        var singleChildBranchWithObjectDataType = new SemanticBranchNode("ContactInformation", DataType.Object);
        singleChildBranchWithObjectDataType.AddChild(new SemanticLeafNode("Name", DataType.String, "John"));
        var singleChildBranchWithObjectDataTypeExpectedJson = JsonNode.Parse(@"{ ""ContactInformation"" : { ""Name"" : ""John""} }")!.AsObject();

        var result = _sut.GetJson(singleChildBranchWithObjectDataType, dataQuery);

        var resultObj = JsonNode.Parse(JsonSerializer.Serialize(result))?.AsObject();
        Assert.Equal(JsonSerializer.Serialize(singleChildBranchWithObjectDataTypeExpectedJson), JsonSerializer.Serialize(result));
        Assert.Single(resultObj?["ContactInformation"]?.AsObject()!);
        Assert.Equal("John", resultObj?["ContactInformation"]!["Name"]!.GetValue<string>());
    }

    [Fact]
    public void GetJson_WithSingleChildBranchWithArrayDataType_ReturnsValue()
    {
        var dataQueryString = JsonNode.Parse(@"{
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
        ""ContactInformation"": {
        ""type"": ""array"",
        ""properties"": {
        ""Name"": {
          ""type"": ""string""
        }
        },
      ""required"": [""Name""],
        ""definitions"" : {}
        }}}
        ").AsJsonString();
        var dataQuery = JsonSerializer.Deserialize<JsonSchema>(dataQueryString, _options);
        var singleChildBranchWithArrayDataType = new SemanticBranchNode("ContactInformation", DataType.Array);
        singleChildBranchWithArrayDataType.AddChild(new SemanticLeafNode("Name", DataType.String, "John"));
        var singleChildBranchWithArrayDataTypeExpectedJson = JsonNode.Parse(@"{ ""ContactInformation"" :[ { ""Name"" : ""John""} ] } ")!.AsObject();

        var result = _sut.GetJson(singleChildBranchWithArrayDataType, dataQuery);

        var resultObj = JsonNode.Parse(JsonSerializer.Serialize(result))!.AsObject();
        Assert.Equal(JsonSerializer.Serialize(singleChildBranchWithArrayDataTypeExpectedJson), JsonSerializer.Serialize(result));
        Assert.Single(resultObj["ContactInformation"]!.AsArray());
        Assert.Equal("John", resultObj["ContactInformation"]![0]!["Name"]!.GetValue<string>());
    }

    [Fact]
    public void GetJson_WithNestedArraytypeWithDifferntDataType_ReturnsValue()
    {
        var dataQueryString = JsonNode.Parse(@"{
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
        ""ContactInformations"": {
           ""type"" : ""object"",
            ""properties"":{
            ""ContactInformation"" :{
            ""$ref"": ""#/definitions/ContactInformation""
            }
         },
        ""required"": [""ContactInformation""]
        }},
        ""definitions"": {
        ""ContactInformation"": {
        ""type"": ""array"",
        ""properties"": {
        ""name"": {
          ""type"": ""string""
        },
        ""description"": {
          ""type"": ""string""
        },
        ""ipCommunication"": {
          ""$ref"": ""#/definitions/IPCommunication""
        }},
        ""required"": [""name"", ""description"", ""ipCommunication""]
        },
        ""IPCommunication"": {
        ""type"": ""array"",
        ""properties"": {
        ""AvailableTime"": {
          ""type"": ""number""
        }},
        ""required"": [""AvailableTime""],
        ""additionalProperties"": false
        }}}").AsJsonString();
        var dataQuery = JsonSerializer.Deserialize<JsonSchema>(dataQueryString, _options);
        var contactRoot = new SemanticBranchNode("ContactInformations", DataType.Object);
        var contactInformation = new SemanticBranchNode("ContactInformation", DataType.Array);
        var nameLeaf = new SemanticLeafNode("name", DataType.String, "jone doh");
        var descriptionLeaf = new SemanticLeafNode("description", DataType.String, "this is contact infomation");
        var ipCommunication = new SemanticBranchNode("ipCommunication", DataType.Array);
        var availableTimeLeaf = new SemanticLeafNode("AvailableTime", DataType.Number, "9");
        ipCommunication.AddChild(availableTimeLeaf);
        contactInformation.AddChild(nameLeaf);
        contactInformation.AddChild(descriptionLeaf);
        contactInformation.AddChild(ipCommunication);
        var contactInformation1 = new SemanticBranchNode("ContactInformation", DataType.Array);
        var nameLeaf1 = new SemanticLeafNode("name", DataType.String, "jane");
        var descriptionLeaf1 = new SemanticLeafNode("description", DataType.String, "this is contact infomation");
        var ipCommunication1 = new SemanticBranchNode("ipCommunication", DataType.Array);
        var availableTimeLeaf1_1 = new SemanticLeafNode("AvailableTime", DataType.Number, "8");
        var availableTimeLeaf1_2 = new SemanticLeafNode("AvailableTime", DataType.Number, "56");
        ipCommunication1.AddChild(availableTimeLeaf1_1);
        ipCommunication1.AddChild(availableTimeLeaf1_2);
        contactInformation1.AddChild(nameLeaf1);
        contactInformation1.AddChild(descriptionLeaf1);
        contactInformation1.AddChild(ipCommunication1);
        contactRoot.AddChild(contactInformation);
        contactRoot.AddChild(contactInformation1);
        var expectedContactJson = JsonNode.Parse(@"
            {
              ""ContactInformations"":{
                  ""ContactInformation"": [
                     {""name"": ""jone doh"",
                    ""description"": ""this is contact infomation"",
                    ""ipCommunication"":[ {
                      ""AvailableTime"": 9
                    }]},
                    {""name"": ""jane"",
                    ""description"": ""this is contact infomation"",
                    ""ipCommunication"": [{
                      ""AvailableTime"": 8
                    },{
                      ""AvailableTime"": 56
                    }]}
                    ]}}")
                                          ?.AsObject();

        var result = _sut.GetJson(contactRoot, dataQuery);

        var resultObj = JsonNode.Parse(JsonSerializer.Serialize(result))!.AsObject();
        Assert.Equal(JsonSerializer.Serialize(expectedContactJson), JsonSerializer.Serialize(result));
    }

    [Fact]
    public void GetJson_WithNullNode_ThrowsArgumentException() => Assert.Throws<ArgumentException>(() => _sut.GetJson(null, null));
}
