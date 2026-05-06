using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Legacy;
using AAS.TwinEngine.Plugin.TestPlugin.Api.Submodel.Services.Validation;

using Json.Schema;

namespace AAS.TwinEngine.Plugin.TestPlugin.UnitTests.Api.Submodel.Services.Validation;

public class JsonSchemaDraftHandlersTests
{
    [Fact]
    public void JsonSchemaDraft202012Handler_CanHandle_WhenSchemaMissing_ReturnsTrue()
    {
        var sut = new JsonSchemaDraft202012Handler();

        var result = sut.CanHandle(null);

        Assert.True(result);
    }

    [Fact]
    public void JsonSchemaDraft202012Handler_CanHandle_WhenDraft7Schema_ReturnsFalse()
    {
        var sut = new JsonSchemaDraft202012Handler();

        var result = sut.CanHandle(MetaSchemas.Draft7Id.OriginalString);

        Assert.False(result);
    }

    [Fact]
    public void LegacyDraft7JsonSchemaValidatorHandler_CanHandle_WhenDraft7Schema_ReturnsTrue()
    {
#pragma warning disable CS0618
        var sut = new LegacyDraft7JsonSchemaValidatorHandler();
#pragma warning restore CS0618

        var result = sut.CanHandle(MetaSchemas.Draft7Id.OriginalString);

        Assert.True(result);
    }

    [Fact]
    public void LegacyDraft7JsonSchemaValidatorHandler_CanHandle_WhenDraft202012Schema_ReturnsFalse()
    {
#pragma warning disable CS0618
        var sut = new LegacyDraft7JsonSchemaValidatorHandler();
#pragma warning restore CS0618

        var result = sut.CanHandle(MetaSchemas.Draft202012Id.OriginalString);

        Assert.False(result);
    }
}
