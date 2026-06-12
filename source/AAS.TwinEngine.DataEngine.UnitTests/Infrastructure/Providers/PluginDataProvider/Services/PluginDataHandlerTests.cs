using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;
using AAS.TwinEngine.DataEngine.ServiceConfiguration.Config;

using AasCore.Aas3_1;

using Json.Schema;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginDataHandlerTests
{
    private readonly IPluginRequestBuilder _pluginRequestBuilder;
    private readonly IPluginDataProvider _pluginDataProvider;
    private readonly IJsonSchemaValidator _jsonSchemaValidator;
    private readonly IMultiPluginDataHandler _multiPluginDataHandler;
    private readonly ILogger<PluginDataHandler> _logger;
    private readonly IOptions<GeneralConfig> _options;
    private readonly PluginDataHandler _sut;

    public PluginDataHandlerTests()
    {
        _pluginRequestBuilder = Substitute.For<IPluginRequestBuilder>();
        _pluginDataProvider = Substitute.For<IPluginDataProvider>();
        _jsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();
        _multiPluginDataHandler = Substitute.For<IMultiPluginDataHandler>();
        _logger = Substitute.For<ILogger<PluginDataHandler>>();
        _options = Options.Create(new GeneralConfig
        {
            DataEngineRepositoryBaseUrl = new Uri("https://www.mm-software.com"),
        });

        _sut = new PluginDataHandler(_pluginRequestBuilder, _pluginDataProvider, _jsonSchemaValidator, _multiPluginDataHandler, _logger, _options);
    }

    private readonly JsonSerializerOptions _jsonoptions = new()
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

    [Fact]
    public async Task TryGetValuesAsync_WithValidManifestAndResponse_ReturnsMergedSemanticTreeNode()
    {
        // Arrange
        var inputSemanticTreeNode = new SemanticLeafNode("Contact", "", DataType.String, Cardinality.One);

        const string ExpectedJsonResponse = """
                                            {
                                                "Contact": "value"
                                            }
                                            """;

        const string JsonSchemaString = """
                                            {
                                                "$schema": "http://json-schema.org/draft-07/schema#",
                                                "type": "object",
                                                "properties": {
                                                    "Contact": { "type": "string" }
                                                }
                                            }
                                            """;

        using var jsonContent = ConvertToJsonContent(JsonSchemaString);

        var requestList = new List<PluginRequestSubmodel>
            {
                new($"{HttpClientNames.PluginDataProviderPrefix}TestPlugin", jsonContent)
            };

        var manifests = new List<PluginManifest>
        {
        new()
        {
            PluginName = "TestPlugin",
            PluginUrl = new Uri("http://localhost"),
            SupportedSemanticIds = ["Contact"],
            Capabilities = new Capabilities { HasShellDescriptor = true }
        }
        };

        _multiPluginDataHandler
            .SplitByPluginManifests(Arg.Any<SemanticTreeNode>(), Arg.Any<IReadOnlyList<PluginManifest>>())
            .Returns(new Dictionary<string, SemanticTreeNode> { { "TestPlugin", inputSemanticTreeNode } });

        _jsonSchemaValidator
            .When(x => x.ValidateRequestSchema(Arg.Any<JsonSchema>()))
            .Do(_ => { });

        _jsonSchemaValidator
            .When(x => x.ValidateResponseContent(Arg.Any<string>(), Arg.Any<JsonSchema>()))
            .Do(_ => { });

        _pluginRequestBuilder
            .Build(Arg.Any<IDictionary<string, JsonSchema>>())
            .Returns(requestList);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ExpectedJsonResponse, Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForSemanticIdsAsync(
                Arg.Any<IList<PluginRequestSubmodel>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
           .Returns(_ => Task.FromResult<IList<HttpContent>>([httpResponse.Content]));

        _multiPluginDataHandler
            .Merge(Arg.Any<SemanticTreeNode>(), Arg.Any<IList<SemanticTreeNode>>())
            .Returns(ci => ci.ArgAt<IList<SemanticTreeNode>>(1).First());

        // Act
        var result = await _sut.TryGetValuesAsync(manifests, inputSemanticTreeNode, "submodelId", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var leaf = Assert.IsType<SemanticLeafNode>(result);
        Assert.Equal("Contact", leaf.SemanticId);
        Assert.Equal("value", leaf.Value);

        _jsonSchemaValidator.Received().ValidateRequestSchema(Arg.Any<JsonSchema>());
        _jsonSchemaValidator.Received().ValidateResponseContent(ExpectedJsonResponse, Arg.Any<JsonSchema>());
    }

    [Fact]
    public async Task GetDataForAllShellDescriptorsAsync_ReturnsListWithHrefSet()
    {
        var metaData = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = "nextCursor" },
            ShellDescriptors = [new() { Id = "ContactInformation" },
                                new() { Id = "Nameplate" }]
        };

        var json = JsonSerializer.Serialize(metaData, _jsonoptions);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasShellDescriptor = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>())
            .Returns([new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        _pluginDataProvider
            .GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        var result = await _sut.GetDataForAllShellDescriptorsAsync(null, null, manifests, CancellationToken.None);

        Assert.Equal(2, result.ShellDescriptors.Count);
        Assert.All(result.ShellDescriptors, dto => Assert.StartsWith("https://www.mm-software.com/shells/", dto.Href));
    }

    [Fact]
    public async Task GetDataForAllShellDescriptorsAsync_Throws_WhenDeserializationFails()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasShellDescriptor = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        await Assert.ThrowsAsync<ResponseParsingException>(() =>
            _sut.GetDataForAllShellDescriptorsAsync(null, null, manifests, CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForAllShellDescriptorsAsync_ThrowsAndLogsIdentifiers_WhenAnyDescriptorIdIsEmpty()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasShellDescriptor = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>())
            .Returns([new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        var invalid = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors = [
                new ShellDescriptorMetaData { Id = "", IdShort = "TestIdShort", GlobalAssetId = "TestGlobalAssetId" },
                new ShellDescriptorMetaData { Id = "valid-id" }
            ]
        };

        var json = JsonSerializer.Serialize(invalid, _jsonoptions);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            _sut.GetDataForAllShellDescriptorsAsync(null, null, manifests, CancellationToken.None));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(state =>
                state.ToString()!.Contains("Invalid descriptors (IdShort/GlobalAssetId)") &&
                state.ToString()!.Contains("TestIdShort") &&
                state.ToString()!.Contains("TestGlobalAssetId")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!);
    }

    [Fact]
    public async Task GetDataForAllShellDescriptorsAsync_ThrowsAndLogsNullMarkers_WhenIdShortAndGlobalAssetIdAreNull()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasShellDescriptor = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>())
            .Returns([new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        var invalid = new ShellDescriptorsMetaData
        {
            PagingMetaData = new PagingMetaData { Cursor = null },
            ShellDescriptors = [
                new ShellDescriptorMetaData { Id = "", IdShort = null, GlobalAssetId = null }
            ]
        };

        var json = JsonSerializer.Serialize(invalid, _jsonoptions);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            _sut.GetDataForAllShellDescriptorsAsync(null, null, manifests, CancellationToken.None));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(state =>
                state.ToString()!.Contains("Invalid descriptors (IdShort/GlobalAssetId)") &&
                state.ToString()!.Contains("<null>") &&
                state.ToString()!.Contains("GlobalAssetId = <null>")),
            null,
            Arg.Any<Func<object, Exception?, string>>()!);
    }

    [Fact]
    public async Task GetDataForShellDescriptorAsync_WhenIdIsEmpty_ThrowsValidationFailedException()
    {
        const string RequestedId = "id";

        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasShellDescriptor = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>(), Arg.Any<string>())
            .Returns(returnThis: [new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                  "id": "",
                  "idShort": "test"
                }
                """, Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForShellDescriptorByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            _sut.GetDataForShellDescriptorAsync(manifests, RequestedId, CancellationToken.None));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(state =>
                state.ToString()!.Contains("requested id") &&
                state.ToString()!.Contains(RequestedId)),
            null,
            Arg.Any<Func<object, Exception?, string>>()!);
    }

    [Fact]
    public async Task GetDataForShellDescriptorAsync_ReturnsSingleWithHrefSet()
    {
        var single = new ShellDescriptorMetaData { Id = "ContactInformation" };
        var json = JsonSerializer.Serialize(single, _jsonoptions);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasAssetInformation = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>(), Arg.Any<string>())
            .Returns([new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        _pluginDataProvider
            .GetDataForShellDescriptorByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        var result = await _sut.GetDataForShellDescriptorAsync(manifests, "ContactInformation", CancellationToken.None);

        Assert.Equal("ContactInformation", result.Id);
        Assert.StartsWith("https://www.mm-software.com/shells/", result.Href);
    }

    [Fact]
    public async Task GetDataForShellDescriptorAsync_WhenIdIsNull_ThrowsValidationFailedException()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasShellDescriptor = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>(), Arg.Any<string>())
            .Returns(returnThis: [new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                  "id": null,
                  "idShort": "test"
                }
                """, Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForShellDescriptorByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            _sut.GetDataForShellDescriptorAsync(manifests, "id", CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForShellDescriptorAsync_ShouldThrow_WhenJsonMalformed()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasAssetInformation = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForShellDescriptorByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        await Assert.ThrowsAsync<ResponseParsingException>(() =>
            _sut.GetDataForShellDescriptorAsync(manifests, "id", CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForAssetInformationByIdAsync_ReturnsAssetInformation()
    {
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(AssetData, Encoding.UTF8, "application/json")
        };

        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasAssetInformation = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>(), Arg.Any<string>())
            .Returns([new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        _pluginDataProvider
            .GetDataForAssetInformationByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        var result = await _sut.GetDataForAssetInformationByIdAsync(manifests, "ContactInformation", CancellationToken.None);

        Assert.IsType<AssetData>(result);
        Assert.Equal("ContactInformation", result.GlobalAssetId);
    }

    [Fact]
    public async Task GetDataForAssetInformationByIdAsync_ShouldThrow_WhenJsonInvalid()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasAssetInformation = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForAssetInformationByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        await Assert.ThrowsAsync<ResponseParsingException>(() =>
            _sut.GetDataForAssetInformationByIdAsync(manifests, "ContactInformation", CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForAssetInformationByIdAsync_ShouldThrow_WhenDeserializedNull()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasAssetInformation = true }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns(["PluginA"]);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForAssetInformationByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        await Assert.ThrowsAsync<ResponseParsingException>(() =>
            _sut.GetDataForAssetInformationByIdAsync(manifests, "ContactInformation", CancellationToken.None));
    }

    public static JsonContent ConvertToJsonContent(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonContent.Create(doc.RootElement.Clone(), options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    [Fact]
    public async Task GetDataForShellDescriptorsByAssetIdsAsync_ReturnsShellDescriptors()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasAssetIdSearch = true }
            }
        };

        _multiPluginDataHandler
            .GetAvailablePlugins(
                Arg.Any<IReadOnlyList<PluginManifest>>(),
                Arg.Any<Func<Capabilities, bool>>())
                    .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>())
            .Returns([new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        var responseJson = """
        {
            "result": [
                { "id": "urn:aas:001", "idShort": "Motor001", "globalAssetId": "urn:asset:001" }
            ],
            "paging_metadata": { "cursor": null }
        }
        """;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForShellDescriptorsByAssetIdsAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<HttpContent> { httpResponse.Content });

        const string Json = """[{"name":"sn","value":"123"}]""";

        var assetIds = JsonSerializer.Deserialize<List<SpecificAssetId>>(Json)!;
        var result = await _sut.GetDataForShellsByAssetIdsAsync(manifests, assetIds, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.ShellDescriptors);
        Assert.Equal("urn:aas:001", result.ShellDescriptors[0].Id);
    }

    [Fact]
    public async Task GetDataForShellDescriptorsByAssetIdsAsync_WhenNullDeserialization_ThrowsResponseParsingException()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasAssetIdSearch = true }
            }
        };

        _multiPluginDataHandler
            .GetAvailablePlugins(
                Arg.Any<IReadOnlyList<PluginManifest>>(),
                Arg.Any<Func<Capabilities, bool>>())
                    .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>())
            .Returns([new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForShellDescriptorsByAssetIdsAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        await Assert.ThrowsAsync<ResponseParsingException>(() =>
            _sut.GetDataForShellsByAssetIdsAsync(manifests, [], CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForShellDescriptorsByAssetIdsAsync_WhenInvalidJson_ThrowsResponseParsingException()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities{ HasAssetIdSearch = true }
            }
        };

        _multiPluginDataHandler
            .GetAvailablePlugins(
                Arg.Any<IReadOnlyList<PluginManifest>>(),
                Arg.Any<Func<Capabilities, bool>>())
                    .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>())
            .Returns([new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json!", Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForShellDescriptorsByAssetIdsAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        await Assert.ThrowsAsync<ResponseParsingException>(() =>
            _sut.GetDataForShellsByAssetIdsAsync(manifests, [], CancellationToken.None));
    }

    [Fact]
    public async Task GetDataForShellDescriptorsByAssetIdsAsync_SetsHrefOnResults()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities { HasAssetIdSearch = true }
            }
        };

        _multiPluginDataHandler
            .GetAvailablePlugins(
                Arg.Any<IReadOnlyList<PluginManifest>>(),
                Arg.Any<Func<Capabilities, bool>>())
                    .Returns(["PluginA"]);

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>())
            .Returns([new($"{HttpClientNames.PluginDataProviderPrefix}PluginA", "")]);

        var responseJson = """
        {
            "result": [
                { "id": "urn:aas:001", "idShort": "Motor001" },
                { "id": "urn:aas:002", "idShort": "Motor002" }
            ],
            "paging_metadata": { "cursor": null }
        }
        """;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForShellDescriptorsByAssetIdsAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([httpResponse.Content]);

        var result = await _sut.GetDataForShellsByAssetIdsAsync(manifests, [], CancellationToken.None);

        Assert.Equal(2, result.ShellDescriptors.Count);
        Assert.All(result.ShellDescriptors, dto => Assert.StartsWith("https://www.mm-software.com/shells/", dto.Href));
    }

    [Fact]
    public async Task GetDataForShellDescriptorsByAssetIdsAsync_WhenNoAvailablePlugins_ThrowsPluginCapabilityNotSupportedException()
    {
        var manifests = new List<PluginManifest>
        {
            new()
            {
                PluginName = "PluginA",
                PluginUrl = new Uri("http://plugin-a"),
                SupportedSemanticIds = ["id-1"],
                Capabilities = new Capabilities{ HasAssetIdSearch = false }
            }
        };

        _multiPluginDataHandler.GetAvailablePlugins(manifests, Arg.Any<Func<Capabilities, bool>>())
            .Returns([]);

        await Assert.ThrowsAsync<PluginCapabilityNotSupportedException>(() =>
            _sut.GetDataForShellsByAssetIdsAsync(manifests, [], CancellationToken.None));
    }

    private const string AssetData = """
                                                {
                                                    "globalAssetId": "ContactInformation",
                                                    "specificAssetIds": [],
                                                    "defaultThumbnail": {
                                                      "contentType": "image/svg+xml",
                                                      "path": "AAS_Logo.svg"
                                                    }
                                                }
                                                """;
}

