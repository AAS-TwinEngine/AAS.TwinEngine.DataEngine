using System.Net.Http.Json;

using AAS.TwinEngine.DataEngine.ApplicationLogic.Exceptions.Infrastructure;
using AAS.TwinEngine.DataEngine.Infrastructure.Monitoring;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Config;
using AAS.TwinEngine.DataEngine.Infrastructure.Providers.PluginDataProvider.Services;

using Json.Schema;

using NSubstitute;

namespace AAS.TwinEngine.DataEngine.UnitTests.Infrastructure.Providers.PluginDataProvider.Services;

public class PluginRequestBuilderTests
{
    private readonly IPluginManifestHealthStatus _pluginManifestHealthStatus;
    private readonly PluginRequestBuilder _sut;

    public PluginRequestBuilderTests()
    {
        _pluginManifestHealthStatus = Substitute.For<IPluginManifestHealthStatus>();
        _sut = new PluginRequestBuilder(_pluginManifestHealthStatus);
    }

    [Fact]
    public void Build_WhenManifestIsUnhealthy_ShouldThrowMultiPluginConflictException()
    {
        _pluginManifestHealthStatus.IsHealthy.Returns(false);
        var jsonSchemas = new Dictionary<string, JsonSchema>
        {
            { "schema1", new JsonSchemaBuilder().Type(SchemaValueType.String).Build() }
        };

        Assert.Throws<MultiPluginConflictException>(() => _sut.Build(jsonSchemas));
    }

    [Fact]
    public void Build_WhenManifestIsHealthy_ShouldCreatePluginRequestSubmodels()
    {
        var schema1 = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        var schema2 = new JsonSchemaBuilder().Type(SchemaValueType.Number).Build();
        _pluginManifestHealthStatus.IsHealthy.Returns(true);
        var jsonSchemas = new Dictionary<string, JsonSchema>
        {
            { "schema1", schema1 },
            { "schema2", schema2 }
        };

        var result = _sut.Build(jsonSchemas).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.HttpClientName == $"{PluginConfig.HttpClientNamePrefix}schema1");
        Assert.Contains(result, r => r.HttpClientName == $"{PluginConfig.HttpClientNamePrefix}schema2");
    }

    [Fact]
    public void Build_ShouldReturnEmptyList_WhenInputIsEmpty()
    {
        var jsonSchemas = new Dictionary<string, JsonSchema>();
        _pluginManifestHealthStatus.IsHealthy.Returns(true);

        var result = _sut.Build(jsonSchemas);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Build_ShouldCreatePluginRequestSubmodels_ForEachSchema()
    {
        var schema1 = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        var schema2 = new JsonSchemaBuilder().Type(SchemaValueType.Number).Build();
        _pluginManifestHealthStatus.IsHealthy.Returns(true);
        var jsonSchemas = new Dictionary<string, JsonSchema>
        {
            { "schema1", schema1 },
            { "schema2", schema2 }
        };

        var result = _sut.Build(jsonSchemas).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.HttpClientName == $"{PluginConfig.HttpClientNamePrefix}schema1");
        Assert.Contains(result, r => r.HttpClientName == $"{PluginConfig.HttpClientNamePrefix}schema2");
    }

    [Fact]
    public void Build_WhenManifestNotHealthy_ThrowsMultiPluginConflictException()
    {
        _pluginManifestHealthStatus.IsHealthy = false;
        var plugins = new List<string> { "PluginA" };

        Assert.Throws<MultiPluginConflictException>(() => _sut.Build(plugins));
    }

    [Fact]
    public void Build_WhenEmptyPluginList_ReturnsEmptyList()
    {
        var plugins = new List<string>();
        _pluginManifestHealthStatus.IsHealthy.Returns(true);

        var result = _sut.Build(plugins);

        Assert.Empty(result);
    }

    [Fact]
    public void Build_WhenPluginsProvided_ReturnsCorrectMetaData()
    {
        var plugins = new List<string> { "PluginA", "PluginB" };
        _pluginManifestHealthStatus.IsHealthy.Returns(true);

        var result = _sut.Build(plugins);

        Assert.Equal(2, result.Count);
        Assert.Collection(result,
            item =>
            {
                Assert.Equal($"{PluginConfig.HttpClientNamePrefix}PluginA", item.HttpClientName);
                Assert.Equal(string.Empty, item.AasIdentifier);
            },
            item =>
            {
                Assert.Equal($"{PluginConfig.HttpClientNamePrefix}PluginB", item.HttpClientName);
                Assert.Equal(string.Empty, item.AasIdentifier);
            });
    }

    [Fact]
    public void Build_WhenAasIdentifierProvided_AssignsToAllResults()
    {
        _pluginManifestHealthStatus.IsHealthy.Returns(true);
        var plugins = new List<string> { "PluginA" };
        const string AasIdentifier = "aas-123";

        var result = _sut.Build(plugins, AasIdentifier);

        var item = Assert.Single(result);
        Assert.Equal($"{PluginConfig.HttpClientNamePrefix}PluginA", item.HttpClientName);
        Assert.Equal("aas-123", item.AasIdentifier);
    }

    [Fact]
    public void Build_WhenAasIdentifierIsNull_AssignsEmptyString()
    {
        _pluginManifestHealthStatus.IsHealthy.Returns(true);
        var plugins = new List<string> { "PluginA" };

        var result = _sut.Build(plugins, null);

        var item = Assert.Single(result);
        Assert.Equal($"{PluginConfig.HttpClientNamePrefix}PluginA", item.HttpClientName);
        Assert.Equal(string.Empty, item.AasIdentifier);
    }

    [Fact]
    public void CreateHttpContent_WithValidSchema_ReturnsJsonContentWithCorrectOptions()
    {
        var schema = new JsonSchemaBuilder().Title("Test").Build();

        var content = InvokeCreateHttpContent(schema);

        Assert.NotNull(content);
        Assert.Equal("application/json", content.Headers.ContentType?.MediaType);
    }

    private static JsonContent InvokeCreateHttpContent(JsonSchema schema)
    {
        var method = typeof(PluginRequestBuilder)
            .GetMethod("CreateHttpContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);

        return (JsonContent)method.Invoke(null, [schema])!;
    }
}
