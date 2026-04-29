using System.Net.Http.Json;
using System.Text.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin;
using AAS.TwinEngine.DataEngine.ApplicationLogic.Services.Plugin.Providers;
using AAS.TwinEngine.DataEngine.DomainModel.Plugin;
using AAS.TwinEngine.DataEngine.DomainModel.SubmodelRepository;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Helper.LegacyV1;

using Json.Schema;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Helper.LegacyV1;

#pragma warning disable CS0618
public class LegacySchemaRetryHandlerTests
{
    private readonly IPluginRequestBuilder _pluginRequestBuilder;
    private readonly IPluginDataProvider _pluginDataProvider;
    private readonly ILogger<LegacySchemaRetryHandler> _logger;
    private readonly LegacySchemaRetryHandler _sut;

    public LegacySchemaRetryHandlerTests()
    {
        _pluginRequestBuilder = Substitute.For<IPluginRequestBuilder>();
        _pluginDataProvider = Substitute.For<IPluginDataProvider>();
        _logger = Substitute.For<ILogger<LegacySchemaRetryHandler>>();
        _sut = new LegacySchemaRetryHandler(_pluginRequestBuilder, _pluginDataProvider, _logger);
    }

    [Fact]
    public async Task RetryWithDraft7Async_WhenSemanticNodesProvided_LogsWarningBuildsDraft7SchemasAndReturnsProviderResponse()
    {
        const string rootSemanticId = "https://example.com/contactInformation";
        const string submodelId = "submodel-id";
        using var responseContent = new StringContent("{\"ok\":true}");
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        using var requestContent = JsonContent.Create(new { });
        var rootNode = new SemanticBranchNode(rootSemanticId, Cardinality.One);
        rootNode.AddChild(new SemanticLeafNode("https://example.com/name", string.Empty, DataType.String, Cardinality.One));
        var semanticNodes = new Dictionary<string, SemanticTreeNode>
        {
            ["plugin-a"] = rootNode
        };
        IDictionary<string, JsonSchema>? capturedSchemas = null;
        var expectedRequests = new List<PluginRequestSubmodel>
        {
            new("plugin-a", requestContent)
        };
        _pluginRequestBuilder
            .Build(Arg.Do<IDictionary<string, JsonSchema>>(schemas => capturedSchemas = schemas))
            .Returns(expectedRequests);
        _pluginDataProvider
            .GetDataForSemanticIdsAsync(expectedRequests, submodelId, cancellationToken)
            .Returns([responseContent]);

        var result = await _sut.RetryWithDraft7Async(semanticNodes, submodelId, cancellationToken);

        var singleResult = Assert.Single(result);
        Assert.Same(responseContent, singleResult);
        Assert.NotNull(capturedSchemas);
        Assert.True(capturedSchemas!.TryGetValue("plugin-a", out var generatedSchema));
        var schemaElement = ToElement(generatedSchema);
        Assert.Equal("http://json-schema.org/draft-07/schema#", schemaElement.GetProperty("$schema").GetString());
        Assert.True(schemaElement.GetProperty("properties").TryGetProperty(rootSemanticId, out _));
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Retrying with Draft-07 fallback", StringComparison.Ordinal)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());

        _pluginRequestBuilder.Received(1).Build(Arg.Any<IDictionary<string, JsonSchema>>());
        await _pluginDataProvider.Received(1).GetDataForSemanticIdsAsync(expectedRequests, submodelId, cancellationToken);
    }

    [Fact]
    public async Task RetryWithDraft7Async_WhenSemanticNodesAreEmpty_BuildsEmptySchemaSetAndReturnsProviderResponse()
    {
        const string submodelId = "submodel-id";
        var semanticNodes = new Dictionary<string, SemanticTreeNode>();
        var expectedRequests = new List<PluginRequestSubmodel>();
        var expectedResponse = new List<HttpContent>();
        _pluginRequestBuilder.Build(Arg.Any<IDictionary<string, JsonSchema>>()).Returns(expectedRequests);
        _pluginDataProvider
            .GetDataForSemanticIdsAsync(expectedRequests, submodelId, CancellationToken.None)
            .Returns(expectedResponse);

        var result = await _sut.RetryWithDraft7Async(semanticNodes, submodelId, CancellationToken.None);

        Assert.Empty(result);
        _pluginRequestBuilder.Received(1).Build(Arg.Is<IDictionary<string, JsonSchema>>(schemas => schemas.Count == 0));
        await _pluginDataProvider.Received(1).GetDataForSemanticIdsAsync(expectedRequests, submodelId, CancellationToken.None);
    }

    [Fact]
    public async Task RetryWithDraft7Async_WhenPluginRequestBuilderThrows_PropagatesExceptionAndDoesNotCallProvider()
    {
        var semanticNodes = new Dictionary<string, SemanticTreeNode>
        {
            ["plugin-a"] = new SemanticLeafNode("https://example.com/name", string.Empty, DataType.String, Cardinality.One)
        };

        _pluginRequestBuilder
            .Build(Arg.Any<IDictionary<string, JsonSchema>>())
            .Throws(new InvalidOperationException("build failed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RetryWithDraft7Async(semanticNodes, "submodel-id", CancellationToken.None));

        Assert.Equal("build failed", exception.Message);
        await _pluginDataProvider.DidNotReceive().GetDataForSemanticIdsAsync(Arg.Any<IList<PluginRequestSubmodel>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetryWithDraft7Async_WhenProviderThrows_PropagatesException()
    {
        using var requestContent = JsonContent.Create(new { });
        var semanticNodes = new Dictionary<string, SemanticTreeNode>
        {
            ["plugin-a"] = new SemanticLeafNode("https://example.com/name", string.Empty, DataType.String, Cardinality.One)
        };
        _pluginRequestBuilder
            .Build(Arg.Any<IDictionary<string, JsonSchema>>())
            .Returns([new("plugin-a", requestContent)]);
        _pluginDataProvider
            .GetDataForSemanticIdsAsync(Arg.Any<IList<PluginRequestSubmodel>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<IList<HttpContent>>(new HttpRequestException("provider failed")));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _sut.RetryWithDraft7Async(semanticNodes, "submodel-id", CancellationToken.None));

        Assert.Equal("provider failed", exception.Message);
    }

    private static JsonElement ToElement(JsonSchema schema)
    {
        var json = JsonSerializer.Serialize(schema);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
#pragma warning restore CS0618
