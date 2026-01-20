using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Handler;
using Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;
using Aas.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services;
using Aas.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Services.Submodel;
using Aas.TwinEngine.Plugin.TestPlugin.DomainModel.Submodel;

using Json.Schema;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Submodel.Handler;

public class SubmodelHandlerTests
{
    private const string JsonSchemaString = @"{
    ""$schema"": ""http://json-schema.org/draft-07/schema#"",
    ""type"": ""object"",
    ""properties"": {
    ""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations"": {
      ""type"": ""object"",
      ""properties"": {
        ""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation"": {
          ""anyOf"": [
            { ""$ref"": ""#/definitions/ContactInformation"" },
            {
              ""type"": ""array"",
              ""items"": { ""$ref"": ""#/definitions/ContactInformation"" }
            }
          ]
        }
      },
      ""required"": [
        ""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation""
      ]
    }
  },
  ""definitions"": {
    ""ContactInformation"": {
      ""type"": ""object"",
      ""properties"": {
        ""0173-1#02-AAO204#003"": { ""type"": ""string"" },
        ""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation/Language"": { ""type"": ""string"" }
      },
      ""required"": [
        ""0173-1#02-AAO204#003"",
        ""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation/Language""
      ]
    }
  }}";

    private const string JsonResponse = @"{
            ""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations"": {
            ""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation"": {
            ""0173-1#02-AAO204#003"": ""John Doe"",
            ""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation/Language"": ""en""
            }
        }
        }";

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

    private readonly JsonSchema JsonSchemaRequest;
    private readonly JsonObject _expectedResponse;
    private readonly ILogger<SubmodelHandler> _logger = Substitute.For<ILogger<SubmodelHandler>>();
    private readonly ISubmodelService _pluginService = Substitute.For<ISubmodelService>();
    private readonly IJsonSchemaParser _jsonSchemaParser = Substitute.For<IJsonSchemaParser>();
    private readonly SubmodelHandler _sut;
    private readonly GetSubmodelDataRequest _request;
    private readonly SemanticBranchNode _semanticTree;
    private readonly SemanticBranchNode _sematicTreeWithData;
    private readonly ISemanticTreeHandler _semanticTreeHandler = Substitute.For<ISemanticTreeHandler>();

    public SubmodelHandlerTests()
    {
        _semanticTree = new SemanticBranchNode("https://admin-shell.io/zvei/nameplate/1/0/ContactInformations", DataType.Object);
        var contactNode = new SemanticBranchNode("https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation", DataType.Object);
        contactNode.AddChild(new SemanticLeafNode("0173-1#02-AAO204#003", DataType.String, ""));
        contactNode.AddChild(new SemanticLeafNode(
            "https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation/Language", DataType.String, ""));
        _semanticTree.AddChild(contactNode);

        _sematicTreeWithData = new SemanticBranchNode("https://admin-shell.io/zvei/nameplate/1/0/ContactInformations", DataType.Object);
        var contactNodeWithData = new SemanticBranchNode("https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation", DataType.Object);
        contactNodeWithData.AddChild(new SemanticLeafNode("0173-1#02-AAO204#003", DataType.String, "John Doe"));
        contactNodeWithData.AddChild(new SemanticLeafNode(
            "https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation/Language", DataType.String, "en"));
        _sematicTreeWithData.AddChild(contactNodeWithData);

        _expectedResponse = JsonNode.Parse(JsonResponse)!.AsObject();
        JsonSchemaRequest = JsonSerializer.Deserialize<JsonSchema>(JsonSchemaString, _options);
        _sut = new SubmodelHandler(_logger, _pluginService, _jsonSchemaParser, _semanticTreeHandler);
        _request = new GetSubmodelDataRequest("ContactInformation", JsonSchemaRequest);
    }

    [Fact]
    public async Task Handle_ReturnsJsonObject_WhenParserAndServiceSucceed()
    {
        _jsonSchemaParser.ParseJsonSchema(JsonSchemaRequest).Returns(_semanticTree);
        _pluginService.GetValuesBySemanticIds(_semanticTree, "ContactInformation").Returns(_sematicTreeWithData);
        _semanticTreeHandler.GetJson(_sematicTreeWithData, JsonSchemaRequest).Returns(_expectedResponse);

        var actual = await _sut.GetSubmodelData(_request, CancellationToken.None);

        Assert.Same(_expectedResponse, actual);
        _jsonSchemaParser.Received(1).ParseJsonSchema(JsonSchemaRequest);
        _pluginService.Received(1).GetValuesBySemanticIds(_semanticTree, "ContactInformation");
        _semanticTreeHandler.Received(1).GetJson(_sematicTreeWithData, JsonSchemaRequest);
    }

    [Fact]
    public async Task Handle_CallsServiceEvenWhenParserReturnsEmptyList()
    {
        var emptyBranch = new SemanticBranchNode("emptyRoot", DataType.Object);
        _jsonSchemaParser.ParseJsonSchema(JsonSchemaRequest).Returns(emptyBranch);
        var emptyResponse = new JsonObject();
        _pluginService.GetValuesBySemanticIds(emptyBranch, "ContactInformation").Returns(emptyBranch);
        _semanticTreeHandler.GetJson(emptyBranch, JsonSchemaRequest).Returns(emptyResponse);

        var actual = await _sut.GetSubmodelData(_request, CancellationToken.None);

        Assert.Same(emptyResponse, actual);
        _jsonSchemaParser.Received(1).ParseJsonSchema(JsonSchemaRequest);
        _pluginService.Received(1).GetValuesBySemanticIds(emptyBranch, "ContactInformation");
        _semanticTreeHandler.Received(1).GetJson(emptyBranch, JsonSchemaRequest);
    }
}

