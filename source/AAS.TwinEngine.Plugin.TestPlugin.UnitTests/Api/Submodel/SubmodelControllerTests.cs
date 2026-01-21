using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel;
using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Handler;
using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Requests;
using AAS.TwinEngine.Plugin.TestPlugin.ApplicationLogic.Constants;

using Json.Schema;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Submodel;

public class SubmodelControllerTests
{
    private readonly ISubmodelHandler _handler = Substitute.For<ISubmodelHandler>();
    private readonly SubmodelController _sut;
    private readonly string _submodelId = "ContactInformation";
    private readonly string _encodedSubmodelId;
    private readonly JsonSchema _dataQuery;
    private readonly JsonObject _response;
    private readonly string _dataQueryString = "{\r\n  \"$schema\": \"http://json-schema.org/draft-07/schema#\",\r\n  \"type\": \"object\",\r\n  \"properties\": {\r\n    \"https://admin-shell.io/zvei/nameplate/1/0/ContactInformations\": {\r\n      \"type\": \"object\",\r\n      \"properties\": {\r\n        \"https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation\": {\r\n          \"anyOf\": [\r\n            { \"$ref\": \"#/definitions/ContactInformation\" },\r\n            {\r\n              \"type\": \"array\",\r\n              \"items\": { \"$ref\": \"#/definitions/ContactInformation\" }\r\n            }\r\n          ]\r\n        }\r\n      },\r\n      \"required\": [\r\n        \"https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation\"\r\n      ]\r\n    }\r\n  },\r\n  \"definitions\": {\r\n    \"ContactInformation\": {\r\n      \"type\": \"object\",\r\n      \"properties\": {\r\n        \"0173-1#02-AAO204#003\": { \"type\": \"string\" },\r\n        \"https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation/Language\": { \"type\": \"string\" }\r\n      },\r\n      \"required\": [\r\n        \"0173-1#02-AAO204#003\",\r\n        \"https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation/Language\"\r\n      ]\r\n    }\r\n  }\r\n}\r\n";

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

    public SubmodelControllerTests()
    {
        _sut = new SubmodelController(_handler);
        _encodedSubmodelId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(_submodelId));
        _response = JsonNode.Parse(@"{""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations"": {
            ""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation"": {
            ""0173-1#02-AAO204#003"": ""John Doe"",
            ""https://admin-shell.io/zvei/nameplate/1/0/ContactInformations/ContactInformation/Language"": ""en""
        }}}").AsObject();
        _dataQuery = JsonSerializer.Deserialize<JsonSchema>(_dataQueryString, _options);
    }

    [Fact]
    public async Task RetrieveDataAsync_ReturnsBadRequest_WhenComplexDataQueryIsNull()
    {
        var result = await _sut.RetrieveDataAsync(null, _encodedSubmodelId, CancellationToken.None);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(ExceptionMessages.InvalidRequestPayload, badResult.Value);
    }

    [Fact]
    public async Task RetrieveDataAsync_ReturnsOk_WithJsonObject()
    {
        _handler.GetSubmodelData(Arg.Any<GetSubmodelDataRequest>(), Arg.Any<CancellationToken>())
                .Returns(_response);

        var result = await _sut.RetrieveDataAsync(_dataQuery, _encodedSubmodelId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var json = Assert.IsType<JsonObject>(okResult.Value);
        Assert.Equal(_response.ToJsonString(), json.ToJsonString());
    }
}

