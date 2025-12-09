using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Config;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Helper;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.AasRegistry;
using AAS.TwinEngine.DataEngine.DomainModel.AasRepository;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.Shared;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

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
    private readonly PluginDataHandler _sut;

    public PluginDataHandlerTests()
    {
        _pluginRequestBuilder = Substitute.For<IPluginRequestBuilder>();
        _pluginDataProvider = Substitute.For<IPluginDataProvider>();
        _jsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();
        _multiPluginDataHandler = Substitute.For<IMultiPluginDataHandler>();
        _logger = Substitute.For<ILogger<PluginDataHandler>>();

        var aasOptions = Substitute.For<IOptions<AasEnvironmentConfig>>();
        aasOptions.Value.Returns(new AasEnvironmentConfig { DataEngineRepositoryBaseUrl = new Uri("https://www.mm-software.com") });

        _sut = new PluginDataHandler(_pluginRequestBuilder, _pluginDataProvider, _jsonSchemaValidator, aasOptions, _multiPluginDataHandler, _logger);
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
    public void Controller_Throws_WhenBaseUrlMissing()
    {
        var opts = Options.Create(new AasEnvironmentConfig { DataEngineRepositoryBaseUrl = null! });
        Assert.Throws<ArgumentNullException>(() => new PluginDataHandler(_pluginRequestBuilder, _pluginDataProvider, _jsonSchemaValidator, opts, _multiPluginDataHandler, _logger));
    }

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
                new($"{PluginConfig.HttpClientNamePrefix}TestPlugin", jsonContent)
            };

        var manifests = new List<PluginManifest>
        {
        new()
        {
            PluginName = "TestPlugin",
            PluginUrl = new Uri("http://localhost"),
            SupportedSemanticIds = new List<string> { "Contact" },
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

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        httpResponse.Content = new StringContent(ExpectedJsonResponse, Encoding.UTF8, "application/json");

        _pluginDataProvider
            .GetDataForSemanticIdsAsync(
                Arg.Any<IList<PluginRequestSubmodel>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
           .Returns(_ => Task.FromResult<IList<HttpContent>>(new List<HttpContent> { httpResponse.Content }));

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
            .Returns(new List<string> { "PluginA" });

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>())
            .Returns(new List<PluginRequestMetaData> { new($"{PluginConfig.HttpClientNamePrefix}PluginA", "") });

        _pluginDataProvider
            .GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns(new List<HttpContent> { httpResponse.Content });

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
            .Returns(new List<string> { "PluginA" });

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForAllShellDescriptorsAsync(null, null, Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns(new List<HttpContent> { httpResponse.Content });

        await Assert.ThrowsAsync<ResponseParsingException>(() =>
            _sut.GetDataForAllShellDescriptorsAsync(null, null, manifests, CancellationToken.None));
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
            .Returns(new List<string> { "PluginA" });

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>(), Arg.Any<string>())
            .Returns(new List<PluginRequestMetaData> { new($"{PluginConfig.HttpClientNamePrefix}PluginA", "") });

        _pluginDataProvider
            .GetDataForShellDescriptorByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns(new List<HttpContent> { httpResponse.Content });

        var result = await _sut.GetDataForShellDescriptorAsync(manifests, "ContactInformation", CancellationToken.None);

        Assert.Equal("ContactInformation", result.Id);
        Assert.StartsWith("https://www.mm-software.com/shells/", result.Href);
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
            .Returns(new List<string> { "PluginA" });

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForShellDescriptorByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns(new List<HttpContent> { httpResponse.Content });

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
            .Returns(new List<string> { "PluginA" });

        _pluginRequestBuilder.Build(Arg.Any<IList<string>>(), Arg.Any<string>())
            .Returns(new List<PluginRequestMetaData> { new($"{PluginConfig.HttpClientNamePrefix}PluginA", "") });

        _pluginDataProvider
            .GetDataForAssetInformationByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns(new List<HttpContent> { httpResponse.Content });

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
            .Returns(new List<string> { "PluginA" });

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForAssetInformationByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns(new List<HttpContent> { httpResponse.Content });

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
            .Returns(new List<string> { "PluginA" });

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        _pluginDataProvider
            .GetDataForAssetInformationByIdAsync(Arg.Any<IList<PluginRequestMetaData>>(), Arg.Any<CancellationToken>())
            .Returns(new List<HttpContent> { httpResponse.Content });

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

